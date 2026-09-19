using System;

namespace Buoi5.Bai4
{
    class TimBeNhatThoa
    {
        public static void Main(String[] args)
        {
            Console.Write("Moi ban nhap so a: ");
            double a = double.Parse(Console.ReadLine());

            double s = 0;
            int n = 0;

            // vòng lặp chạy liên tục, kh dừng lại cho đến khi tổng s lớn hơn hẳn a
            while (s <= a)
            {
                n++; // tăng dần mẫu số
                s += 1.0 / n; // cộng dồn phân số mới vào tổng hiện tại
            }

            // khi thoát được vòng while thì n hiện tại chính là số nhỏ nhất cần tìm
            Console.WriteLine("So nguyen duong n be nhat thoa man dieu kien la: {0}", n);

            Console.Read();
        }
    }
}