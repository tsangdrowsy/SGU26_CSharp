using System;

namespace Buoi8.Bai2
{
    class LietKeSoHoanHao
    {
        static bool SoHoanHao(int x)
        {
            int tong = 0;
            for (int i = 1;i <= x/2;i++)
            {
                if (x%i == 0)
                    tong = tong + i;
            }
            return tong == x;
        }

        static void Main()
        {
            Console.Write("Moi ban nhap so n: ");
            int n = int.Parse(Console.ReadLine());

            Console.Write($"Cac so hoan hao trong doan [1, {n}]: ");

            for (int i = n; i >= 1; i--) 
            {
                if (SoHoanHao(i))
                    Console.Write("{0} ", i);
            }

            Console.WriteLine();
        }
    }
}