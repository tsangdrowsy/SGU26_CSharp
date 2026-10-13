using System;

namespace Buoi5.Bai2
{
    class GiaTriBieuThuc
    {
        public static void Main(String[] args)
        {
            Console.Write("Moi ban nhap so nguyen n: ");
            int n = int.Parse(Console.ReadLine());

            double s1 = 0;
            double s2 = 0;

            // tính S1: chỉ cần cộng dồn các phân số từ 1/1 đến 1/n
            for (int i = 1; i <= n; i++)
            {
                s1 += 1.0 / i; // dùng 1.0 để ép kiểu chia ra số thực kh bị mất thập phân
            }

            // tính S2: cộng ngược từ 1/n về 1/1 và đan xen dấu (+, -)
            int sign = 1;
            for (int i = n; i >= 1; i--)
            {
                s2 += sign * (1.0 / i);
                sign = -sign; // lật dấu cho phần tử ở vòng lặp kế tiếp
            }

            Console.WriteLine("S1 (n={0}) = {1}.", n, s1);
            Console.WriteLine("S2 (n={0}) = {1}.", n, s2);

            Console.Read();
        }
    }
}