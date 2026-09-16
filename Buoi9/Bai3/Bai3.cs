using System;

namespace Buoi9.Bai3
{
    class DemDau
    {
        static void Main()
        {
            Console.Write("Moi ban nhap so luong phan tu: ");
            int n = int.Parse(Console.ReadLine());

            int[] a = new int[n];

            for (int i = 0; i < n; i++)
            {
                Console.Write("Phan tu {0}: ", i );
                a[i] = int.Parse(Console.ReadLine());
            }

            int x = 0, y = 0, z = 0;

            for (int i = 0; i < n; i++) // Duyệt qua từng phần tử của mảng để đếm số lượng số âm, số dương và số không
            {
                if (a[i] > 0)
                    x++;
                else if (a[i] < 0)
                    y++;
                else
                    z++;
            }

            Console.WriteLine("Day so co {0} so am, {1} so khong va {2} so duong.", y, z, x);
        }
    }
}