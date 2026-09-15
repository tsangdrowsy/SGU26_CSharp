using System;
namespace Buoi4.bai2
{
    public class TienNuoc
    {
        // hằng số
        const int M1 = 4400, M2 = 8300;
        const int M3 = 105000;
        const double VAT = 0.05, BVMT = 0.1;
        public static void Main(string[] args)
        {
            // khai báo biến
            int chiSoThangTruoc, chiSoTrongThang, mKhoi;
            double soTien;

            // nhập dữ liệu
            Console.Write("Moi ban nhap chi so tieu thu nuoc thang truoc (m^3): ");
            chiSoThangTruoc = int.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap chi so tieu thu nuoc trong thang (m^3):");
            chiSoTrongThang = int.Parse(Console.ReadLine());

            // xử lý 
            mKhoi = chiSoTrongThang - chiSoThangTruoc;
            if (mKhoi < 0)
            {
                Console.Write("Khong hop le");
                return;
            }
            if (mKhoi <= 4)

                soTien = M1 * mKhoi;

            else if (mKhoi > 4 && mKhoi <= 6)

                soTien = M1 * 4 + (mKhoi - 4) * M2;

            else
                soTien = M1 * 4 + M2 * 2 + (mKhoi - 6) * M3;

            // tính thuế và phí bv môi trường
            soTien = (1 + VAT + BVMT) * soTien;
            // xuất kết quả
            Console.WriteLine("So tien phai tra cho {0} m^3 tieu thu trong thang la {1}.Đ", mKhoi, soTien);
        }

    }
}

