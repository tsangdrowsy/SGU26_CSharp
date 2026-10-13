using System;

namespace Buoi5.Bai3
{
    class TongPhanSoSaiSo
    {
        public static void Main(String[] args)
        {
            Console.Write("Moi ban nhap so a: ");
            double a = double.Parse(Console.ReadLine());

            Console.Write("Moi ban nhap sai so epsilon (<=0.1): ");
            double epsilon = double.Parse(Console.ReadLine());

            double s = 0;
            int n = 0;
            
            // khởi tạo giá trị của phần tử đầu tiên trong dãy
            double term = 1.0 / (a + n);

            // cứ tiếp tục cộng dồn chừng nào phần tử hiện tại kh nhỏ hơn sai số
            while (term >= epsilon)
            {
                s += term; // cộng vào tổng
                n++;       // tăng n lên để xét phần tử tiếp theo
                term = 1.0 / (a + n); // tính lại giá trị phần tử mới
            }

            Console.WriteLine("Gia tri cua bieu thuc S(a = {0}, epsilon = {1}) = {2}.", a, epsilon, s);

            Console.Read();
        }
    }
}