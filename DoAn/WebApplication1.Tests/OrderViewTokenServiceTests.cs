using Microsoft.AspNetCore.DataProtection;
using WebApplication1.Services.VnPay;

namespace WebApplication1.Tests;

// UT_10: EphemeralDataProtectionProvider tạo khoá tạm trong bộ nhớ, đủ dùng cho test
public class OrderViewTokenServiceTests
{
    private readonly OrderViewTokenService _svc = new(new EphemeralDataProtectionProvider());

    [Fact]
    public void Token_DungDon_HopLe() => Assert.True(_svc.IsValid(_svc.Create(5), 5));

    [Fact]
    public void Token_DonKhac_KhongHopLe() => Assert.False(_svc.IsValid(_svc.Create(5), 6));

    [Fact]
    public void Token_BiSua_KhongHopLe()
    {
        var t = _svc.Create(5);
        var mid = t.Length / 2;
        var tampered = t[..mid] + (t[mid] == 'A' ? 'B' : 'A') + t[(mid + 1)..];   // đổi 1 ký tự ở giữa

        Assert.False(_svc.IsValid(tampered, 5));
    }

    [Fact]
    public void Token_Rong_KhongHopLe() => Assert.False(_svc.IsValid(null, 5));
}
