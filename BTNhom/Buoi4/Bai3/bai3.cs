using System;

namespace Buoi4.bai3;

public class NgaySau
{
    public static void Main(String[] args)
    {
        // Khai báo biến
        int d, m, y;
        int d1, m1, y1;
        int soNgay;
        // Nhập dữ liệu
        Console.Write("Nhap ngay, thang, nam: ");
        string[] input = Console.ReadLine().Split(new char[] { ' ', '\t' },
        StringSplitOptions.RemoveEmptyEntries);

        if (input.Length < 3)
        {
            Console.WriteLine("Loi: Phai nhap du 3 so!");
            return;
        }

        d = int.Parse(input[0]);
        m = int.Parse(input[1]);
        y = int.Parse(input[2]);
        // chặn lỗi ngày, tháng, năm không hợp lệ
        if (d < 1 || d > 31)
        {
            Console.WriteLine("Loi: Ngay phai trong khoang 1-31");
            return;
        }
        if (m < 1 || m > 12)
        {
            Console.WriteLine("Loi: Thang phai trong khoang 1-12");
            return;
        }
        if (y < 1)
        {
            Console.WriteLine("Loi: Nam phai > 0");
            return;
        }
        // Xử lý
        int[] soNgayTrongThang = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        soNgay = soNgayTrongThang[m - 1];

        // Nếu là tháng 2 và năm nhuận
        if (m == 2 && ((y % 400 == 0) || (y % 4 == 0 && y % 100 != 0)))
            soNgay = 29;
        // nếu là ngày cuối năm
        if (d == soNgay)
        {
            if (m == 12)
            {
                d1 = 1;
                m1 = 1;
                y1 = y + 1;
            }
            else //nếu là ngày cuối tháng
            {
                d1 = 1;
                m1 = m + 1;
                y1 = y;
            }
        }
        else //nếu là ngày bình thường
        {
            d1 = d + 1;
            m1 = m;
            y1 = y;
        }
        Console.WriteLine("Ngay sau ngay {0}/{1}/{2} la ngay {3}/{4}/{5}.", d, m, y, d1, m1, y1);
    }
}
