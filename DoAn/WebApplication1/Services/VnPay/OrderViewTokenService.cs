using Microsoft.AspNetCore.DataProtection;

namespace WebApplication1.Services.VnPay
{
    // Token ngắn hạn cho phép xem trang OrderSuccess của ĐÚNG 1 đơn hàng mà không cần session.
    //
    // Vì sao cần: session (cookie) gắn với từng domain. Nếu khách đặt hàng ở domain A mà quay về
    // từ VNPay ở domain B (localhost <-> ngrok), hoặc cookie hết hạn trong lúc thanh toán,
    // thì trang OrderSuccess không biết khách là ai -> trước đây bị đẩy về trang chủ.
    //
    // Data Protection của ASP.NET Core mã hóa + ký dữ liệu bằng khóa bí mật của server:
    // khách không đọc được, không sửa được (đổi orderId sang đơn người khác), và token tự hết hạn.
    public class OrderViewTokenService
    {
        private readonly ITimeLimitedDataProtector _protector;
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

        public OrderViewTokenService(IDataProtectionProvider provider)
        {
            // "Purpose" riêng: token này không dùng được cho mục đích khác (vd: giả cookie)
            _protector = provider.CreateProtector("OrderSuccess.View").ToTimeLimitedDataProtector();
        }

        public string Create(int orderId) => _protector.Protect(orderId.ToString(), Lifetime);

        public bool IsValid(string? token, int orderId)
        {
            if (string.IsNullOrEmpty(token)) return false;
            try
            {
                return _protector.Unprotect(token) == orderId.ToString();
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return false; // token bị sửa, sai khóa hoặc đã hết hạn
            }
        }
    }
}
