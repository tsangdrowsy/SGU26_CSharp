using System;
namespace Buoi4.bai1
{
    public class DiemTrungBinh
    {

        public static void Main(string[] args)
        {
            // Khai báo biến
            double toan, ly, hoa, DTB;
            string[] t;
            string xeploai;

            // Nhập dữ liệu
            Console.Write("Moi ban nhap diem toan, ly, hoa: ");
            t = Console.ReadLine().Split(new char[] { ' ', '\t' },
            StringSplitOptions.RemoveEmptyEntries);
            // chặn nhập đủ 3 điểm
            if (t.Length < 3)
            {
                Console.WriteLine("Phai nhap du 3 diem");
                return;
            }

            toan = double.Parse(t[0]);
            ly = double.Parse(t[1]);
            hoa = double.Parse(t[2]);


            // CT điểm trung bình
            DTB = (toan * 2 + ly * 3 + hoa) / 6;
            // chặn
            if (toan < 0 || toan > 10)
            {
                Console.WriteLine("Diem khong hop le");
                return;
            }
            else if (ly < 0 || ly > 10)
            {
                Console.WriteLine("Diem khong hop le");
                return;
            }
            else if (hoa < 0 || hoa > 10)
            {
                Console.WriteLine("Diem khong hop le");
                return;
            }

            //xử lý thang điểm
            if (DTB >= 8.0 && DTB <= 10)
            {
                xeploai = "Gioi";
            }
            else if (DTB < 8 && DTB >= 6.5)
            {
                xeploai = "Kha";
            }
            else if (DTB < 6.5 && DTB >= 5.0)
            {
                xeploai = "Trung binh";
            }
            else
            {
                xeploai = "Yeu";
            }

            // xuat kết quả
            Console.WriteLine("Ban co diem trung binh {0:F2} duoc xep loai {1}.", DTB, xeploai);

        }
    }
}