using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using WebApplication1.Services.VnPay;
using static WebApplication1.Tests.VnPayTestHelper;

namespace WebApplication1.Tests;

// UT_01 - UT_05: không cần CSDL, chạy được trên mọi máy
public class VnPayServiceTests
{
    [Fact] // UT_01
    public void CreatePaymentUrl_NhanSoTienVoi100_VaCoChuKy()
    {
        var url = CreateService().CreatePaymentUrl("DH001", 100000m, "Thanh toan", "https://localhost/return", "127.0.0.1");

        var q = QueryHelpers.ParseQuery(new Uri(url).Query);
        Assert.Equal("10000000", q["vnp_Amount"].ToString());
        Assert.Equal("DH001", q["vnp_TxnRef"].ToString());
        Assert.Equal(128, q["vnp_SecureHash"].ToString().Length);   // HMAC-SHA512 = 64 byte = 128 ký tự hex
    }

    [Fact] // UT_02
    public void CreatePaymentUrl_ThieuCauHinh_NemLoi()
    {
        var svc = new VnPayService(Options.Create(new VnPayOptions()));   // TmnCode, HashSecret rỗng

        Assert.False(svc.IsConfigured);
        Assert.Throws<InvalidOperationException>(() =>
            svc.CreatePaymentUrl("DH001", 1000m, "x", "https://localhost/return", "127.0.0.1"));
    }

    [Fact] // UT_03
    public void ValidateResponse_ChuKyDung_Ma00_LaThanhCong()
    {
        var r = CreateService().ValidateResponse(Response("DH001", 10000000, "00", "00"));

        Assert.True(r.IsValidSignature);
        Assert.True(r.IsSuccess);
        Assert.Equal("DH001", r.TxnRef);
        Assert.Equal(10000000, r.Amount);
    }

    [Fact] // UT_04
    public void ValidateResponse_SuaSoTienSauKhiKy_ChuKySai()
    {
        var url = CreateService().CreatePaymentUrl("DH001", 100000m, "Thanh toan", "https://localhost/return", "127.0.0.1");
        var q = QueryHelpers.ParseQuery(new Uri(url).Query);
        q["vnp_Amount"] = "100";                                   // kẻ gian sửa số tiền trên URL

        var r = CreateService().ValidateResponse(new QueryCollection(q));

        Assert.False(r.IsValidSignature);
        Assert.False(r.IsSuccess);
    }

    [Fact] // UT_05
    public void ValidateResponse_KhachHuy_ChuKyDungNhungKhongThanhCong()
    {
        var r = CreateService().ValidateResponse(Response("DH001", 10000000, "24", "02"));

        Assert.True(r.IsValidSignature);
        Assert.False(r.IsSuccess);
        Assert.Equal("24", r.ResponseCode);
    }

    [Fact] // bổ sung: ký bằng khoá khác (giả mạo) thì bị từ chối
    public void ValidateResponse_KyBangKhoaKhac_ChuKySai()
    {
        var r = CreateService().ValidateResponse(Response("DH001", 10000000, "00", "00", secret: "KHOA_GIA_MAO"));

        Assert.False(r.IsValidSignature);
        Assert.False(r.IsSuccess);
    }
}
