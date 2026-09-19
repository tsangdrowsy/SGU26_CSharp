using System;

namespace Buoi5.Bai1
{
    class SapChanLe
    {
        public static void Main(String[] args)
        {
            // Nhập dãy số
            Console.WriteLine("// Nhap day so");
            Console.Write("Nhap so phan tu n: ");
            int n = int.Parse(Console.ReadLine());

            int[] e = new int[n];

            for (int i = 0; i < e.Length; i++)
            {
                int temp = int.Parse(Console.ReadLine());
                e[i] = temp;
            }

            // In mảng trước khi sắp xếp
            Console.WriteLine("* Mang truoc khi sap xep:");
            Console.Write("Day so co {0} phan tu: ", n);
            for (int i = 0; i < e.Length; i++)
            {
                Console.Write("{0} ", e[i]);
            }
            Console.WriteLine();

            for (int i = 0; i < e.Length - 1; i++)
            {
                for (int j = i + 1; j < e.Length; j++)
                {
                    bool doiCho = false;

                    if (e[i] % 2 != 0 && e[j] % 2 == 0)
                    {
                        doiCho = true;
                    }
                    else if (e[i] % 2 == 0 && e[j] % 2 == 0 && e[i] > e[j])
                    {
                        doiCho = true;
                    }
                    else if (e[i] % 2 != 0 && e[j] % 2 != 0 && e[i] < e[j])
                    {
                        doiCho = true;
                    }

                    if (doiCho)
                    {
                        int temp = e[i];
                        e[i] = e[j];
                        e[j] = temp;
                    }
                }
            }

            // In mảng sau khi sắp xếp
            Console.WriteLine("* Mang sau khi sap xep:");
            Console.Write("Day so co {0} phan tu: ", n);
            for (int i = 0; i < e.Length; i++)
            {
                int xd = 0;
                xd = e[i];
                Console.Write("{0} ", xd);
            }
            Console.WriteLine();

            Console.Read();
        }
    }
}
