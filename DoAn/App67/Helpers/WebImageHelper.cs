using System;
using System.IO;

namespace App67.Helpers
{
    // Ảnh sản phẩm được Web (WebApplication1) hiển thị từ thư mục wwwroot/img/sanpham.
    // App WinUI chạy trên cùng máy với source code, nên khi admin chọn ảnh, ta copy file
    // vào đúng thư mục đó và lưu đường dẫn tương đối "img/sanpham/<tên file>" vào DB.
    public static class WebImageHelper
    {
        public const string RelativeFolder = "img/sanpham";

        // Đi ngược từ thư mục chạy app (bin/.../AppX) lên trên để tìm DoAn/WebApplication1/wwwroot.
        // Trả về null nếu không tìm thấy (vd: app được cài ở máy khác, không có source Web).
        public static string? FindWebImageFolder()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "WebApplication1", "wwwroot");
                if (Directory.Exists(candidate))
                {
                    return Path.Combine(candidate, "img", "sanpham");
                }
                dir = dir.Parent;
            }
            return null;
        }

        // Copy ảnh vào wwwroot/img/sanpham với tên mới (tránh trùng / ghi đè ảnh cũ),
        // trả về đường dẫn tương đối để lưu vào cột Products.ImageUrl.
        public static string CopyToWeb(string sourcePath)
        {
            string? folder = FindWebImageFolder();
            if (folder == null)
            {
                throw new DirectoryNotFoundException(
                    "Không tìm thấy thư mục WebApplication1/wwwroot. Hãy nhập đường dẫn ảnh thủ công.");
            }

            Directory.CreateDirectory(folder);
            string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            string fileName = $"PROD-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}{ext}";
            File.Copy(sourcePath, Path.Combine(folder, fileName), overwrite: false);
            return $"{RelativeFolder}/{fileName}";
        }
    }
}
