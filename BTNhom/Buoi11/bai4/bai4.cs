using System;

namespace Buoi11.Bai4
{
    class DaoDay
    {
        public static void Main(String[] args)
        {
            Console.WriteLine("// Nhap day so");
            Console.Write("Nhap so phan tu n: ");

            int n = int.Parse(Console.ReadLine());

            int[] e = new int[n];

            for (int i = 0; i < n; i++)
            {
                e[i] = int.Parse(Console.ReadLine());
            }

            Console.WriteLine("* Mang truoc dao:");
            Console.Write("Day so co {0} phan tu: ", n);

            for (int i = 0; i < n; i++)
            {
                Console.Write("{0} ", e[i]);
            }
            Console.WriteLine();

            // đảo ngược dãy số
            for (int i = 0; i < n / 2; i++)
            {
                int temp = e[i];

                e[i] = e[n - 1 - i];

                e[n - 1 - i] = temp;
            }

            Console.WriteLine("* Mang sau khi dao:");
            Console.Write("Day so co {0} phan tu: ", n);
            
            for (int i = 0; i < n; i++)
            {
                Console.Write("{0} ", e[i]);
            }
            Console.WriteLine();

            Console.Read();
        }
    }
}