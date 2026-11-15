using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using WebApplication1.Services.VnPay;

namespace WebApplication1.Tests;

// Tạo dữ liệu giả lập như VNPay gửi về (Return URL / IPN), ký bằng cùng HashSecret.
// Hàm ký viết lại độc lập trong test (không gọi hàm private của VnPayService) để làm "đáp án" so sánh.
public static class VnPayTestHelper
{
    public const string Secret = "TESTSECRET0123456789";

    public static VnPayService CreateService() =>
        new(Options.Create(new VnPayOptions { TmnCode = "TESTCODE", HashSecret = Secret }));

    public static string Sign(SortedDictionary<string, string> data, string secret)
    {
        var raw = string.Join("&", data
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));
        var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // Kết quả VNPay trả về: responseCode/transactionStatus = "00" là thành công, "24" là khách huỷ
    public static IQueryCollection Response(string txnRef, long amount, string responseCode, string transactionStatus,
                                            string secret = Secret)
    {
        var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_TmnCode"] = "TESTCODE",
            ["vnp_TxnRef"] = txnRef,
            ["vnp_Amount"] = amount.ToString(CultureInfo.InvariantCulture),
            ["vnp_ResponseCode"] = responseCode,
            ["vnp_TransactionStatus"] = transactionStatus,
            ["vnp_TransactionNo"] = "14123456",
            ["vnp_OrderInfo"] = "Thanh toan don hang " + txnRef,
        };
        var query = data.ToDictionary(kv => kv.Key, kv => new StringValues(kv.Value));
        query["vnp_SecureHash"] = Sign(data, secret);
        return new QueryCollection(query);
    }
}
