using System;

namespace Buoi11.Bai2
{
    class XoaChan
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

            Console.WriteLine("* Mang truoc xoa:");

            Console.Write("Day so co {0} phan tu: ", n);

            for (int i = 0; i < n; i++)
            {
                Console.Write("{0} ", e[i]);
            }
            Console.WriteLine();

            // Xóa các phần tử
            int newSize = 0;
            
            for (int i = 0; i < n; i++)
            {
                if (e[i] % 2 != 0)
                {
                    e[newSize] = e[i];

                    newSize++;
                }
            }
            n = newSize; // Cap nhat lai so luong phan tu

            Console.WriteLine("* Mang sau khi xoa:");

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