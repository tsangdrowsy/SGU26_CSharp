using System;

namespace Buoi11.Bai3
{
    class ThemChanLe
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

            Console.WriteLine("* Mang truoc them:");
            Console.Write("Day so co {0} phan tu: ", n);

            for (int i = 0; i < n; i++)
            {
                Console.Write("{0} ", e[i]);
            }
            Console.WriteLine();

            // Tao mang moi de chua ket qua sau khi thêm
            int[] newArr = new int[1000];
            int newSize = 0;

            for (int i = 0; i < n; i++)
            {
                newArr[newSize] = e[i];

                newSize++;

                // Them 1 gia tri le (e[i] + 1) neu co 2 phần tu chan lien tiep
                if (i < n - 1 && e[i] % 2 == 0 && e[i + 1] % 2 == 0)
                {
                    newArr[newSize] = e[i] + 1;

                    newSize++;
                }
            }

            Console.WriteLine("* Mang sau khi them:");
            Console.Write("Day so co {0} phan tu: ", newSize);
            
            for (int i = 0; i < newSize; i++)
            {
                Console.Write("{0} ", newArr[i]);
            }
            Console.WriteLine();

            Console.Read();
        }
    }
}