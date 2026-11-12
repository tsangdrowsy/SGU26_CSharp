using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace WebApplication1.Services.VnPay
{
    // Cấu hình đọc từ appsettings.json (mục "VnPay") hoặc user-secrets
    public class VnPayOptions
    {
        public string TmnCode { get; set; } = string.Empty;     // Mã website (Terminal ID) VNPay cấp
        public string HashSecret { get; set; } = string.Empty;  // Chuỗi bí mật để ký dữ liệu - KHÔNG commit lên git
        public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        public string Version { get; set; } = "2.1.0";
        public int ExpireMinutes { get; set; } = 15;
    }

    // Kết quả sau khi kiểm tra dữ liệu VNPay trả về
    public record VnPayResult(
        bool IsValidSignature,
        bool IsSuccess,
        string TxnRef,          // = Order.OrderCode
        long Amount,            // đơn vị: VND * 100
        string ResponseCode,
        string TransactionNo);

    public interface IVnPayService
    {
        bool IsConfigured { get; }
        string CreatePaymentUrl(string txnRef, decimal amount, string orderInfo, string returnUrl, string ipAddress);
        VnPayResult ValidateResponse(IQueryCollection query);
    }

    public class VnPayService : IVnPayService
    {
        private readonly VnPayOptions _options;

        public VnPayService(IOptions<VnPayOptions> options)
        {
            _options = options.Value;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_options.TmnCode) && !string.IsNullOrWhiteSpace(_options.HashSecret);

        public string CreatePaymentUrl(string txnRef, decimal amount, string orderInfo, string returnUrl, string ipAddress)
        {
            // Kiểm tra ở đây (không phải constructor) để thiếu cấu hình VNPay không làm hỏng luôn thanh toán COD
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình VnPay:TmnCode / VnPay:HashSecret (xem hướng dẫn user-secrets).");
            }

            // VNPay yêu cầu thời gian theo giờ Việt Nam (GMT+7). VN không có giờ mùa hè nên +7 là đủ.
            var nowVn = DateTime.UtcNow.AddHours(7);

            // SortedDictionary + StringComparer.Ordinal: VNPay yêu cầu sắp xếp tham số theo thứ tự byte (a-z)
            // trước khi ký. Sai thứ tự -> chữ ký sai -> VNPay báo "Sai chữ ký" (mã 70).
            var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Version"] = _options.Version,
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _options.TmnCode,
                // VNPay nhận số tiền * 100 (không có phần thập phân)
                ["vnp_Amount"] = ((long)(amount * 100)).ToString(CultureInfo.InvariantCulture),
                ["vnp_CurrCode"] = "VND",
                ["vnp_TxnRef"] = txnRef,
                ["vnp_OrderInfo"] = orderInfo,
                ["vnp_OrderType"] = "other",
                ["vnp_Locale"] = "vn",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_IpAddr"] = ipAddress,
                ["vnp_CreateDate"] = nowVn.ToString("yyyyMMddHHmmss"),
                ["vnp_ExpireDate"] = nowVn.AddMinutes(_options.ExpireMinutes).ToString("yyyyMMddHHmmss"),
            };

            var query = BuildQuery(data);
            var secureHash = HmacSha512(_options.HashSecret, query);
            return $"{_options.BaseUrl}?{query}&vnp_SecureHash={secureHash}";
        }

        public VnPayResult ValidateResponse(IQueryCollection query)
        {
            var data = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in query)
            {
                // Chỉ lấy tham số vnp_*, bỏ 2 trường chữ ký ra khỏi dữ liệu cần ký lại
                if (key.StartsWith("vnp_") && key != "vnp_SecureHash" && key != "vnp_SecureHashType")
                {
                    data[key] = value.ToString();
                }
            }

            var receivedHash = query["vnp_SecureHash"].ToString();
            var expectedHash = HmacSha512(_options.HashSecret, BuildQuery(data));

            // So sánh thời gian cố định (FixedTimeEquals) để tránh tấn công đo thời gian (timing attack)
            var isValid = CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expectedHash),
                Encoding.ASCII.GetBytes(receivedHash.ToLowerInvariant()));

            data.TryGetValue("vnp_ResponseCode", out var responseCode);
            data.TryGetValue("vnp_TransactionStatus", out var transactionStatus);
            data.TryGetValue("vnp_TxnRef", out var txnRef);
            data.TryGetValue("vnp_TransactionNo", out var transactionNo);
            data.TryGetValue("vnp_Amount", out var amountStr);
            long.TryParse(amountStr, out var amount);

            return new VnPayResult(
                IsValidSignature: isValid,
                // "00" ở cả 2 trường = giao dịch thành công
                IsSuccess: isValid && responseCode == "00" && transactionStatus == "00",
                TxnRef: txnRef ?? string.Empty,
                Amount: amount,
                ResponseCode: responseCode ?? string.Empty,
                TransactionNo: transactionNo ?? string.Empty);
        }

        // key1=value1&key2=value2 ... (đã URL-encode, bỏ giá trị rỗng) - đúng định dạng VNPay dùng để ký
        private static string BuildQuery(SortedDictionary<string, string> data) =>
            string.Join("&", data
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));

        private static string HmacSha512(string key, string input)
        {
            var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        // Thông báo tiếng Việt cho một số mã lỗi hay gặp (bảng mã đầy đủ ở tài liệu VNPay)
        public static string DescribeResponseCode(string code) => code switch
        {
            "00" => "Giao dịch thành công",
            "07" => "Giao dịch bị nghi ngờ gian lận",
            "09" => "Thẻ/Tài khoản chưa đăng ký Internet Banking",
            "10" => "Xác thực thông tin thẻ/tài khoản sai quá 3 lần",
            "11" => "Hết hạn chờ thanh toán",
            "12" => "Thẻ/Tài khoản bị khóa",
            "13" => "Nhập sai mật khẩu xác thực (OTP)",
            "24" => "Bạn đã hủy giao dịch",
            "51" => "Tài khoản không đủ số dư",
            "65" => "Tài khoản đã vượt hạn mức giao dịch trong ngày",
            "75" => "Ngân hàng thanh toán đang bảo trì",
            "79" => "Nhập sai mật khẩu thanh toán quá số lần quy định",
            _ => $"Thanh toán không thành công (mã {code})"
        };
    }
}
