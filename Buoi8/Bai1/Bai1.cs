using System;

namespace Buoi8.Bai1
{
    class GiaiThua
    {
     static long HamGiaiThua(int x)
        {
            long gt = 1;
            for (int i = 2; i <= x; i++)
            {
                gt *= i;
            }
            return gt;
        }
         static void Main()
        {
            Console.Write("Moi ban nhap k: ");
            int k = int.Parse(Console.ReadLine());

            Console.Write("Moi ban nhap n: ");
            int n = int.Parse(Console.ReadLine());

            long c = HamGiaiThua(n) / (HamGiaiThua(k) * HamGiaiThua(n - k));

            Console.WriteLine("C({0}, {1}) = {2}.", k, n, c);
        }
    }   
}