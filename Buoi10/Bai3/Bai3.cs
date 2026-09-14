using System;
using System.Reflection.Metadata;


namespace Buoi10.Bai1
{
    class DayDanDau
    {
        public static void Main(String[] args)
        {
            Console.Write("Nhap so phan tu n: ");
            int n = int.Parse(Console.ReadLine());

            int[] e = new int[n]; //making a fixed array following the number size

            for (int i = 0; i < e.Length; i++)
            {
                int temp = int.Parse(Console.ReadLine());
                e[i] = temp;

            }
            int dem = 0;

            for (int i = 0; i < e.Length - 1; i++) //I love the bare bone brute forcing
            {
                if (e[i] > 0)
                {
                    if (e[i + 1] < 0)
                    {
                        dem++;
                    }
                }
                if (e[i] < 0)
                {
                    if (e[i + 1] > 0)
                    {
                        dem++;
                    }
                }
            }
            Console.WriteLine("Day so co {0} phan tu: ", n);

            for (int i = 0; i < e.Length; i++)
            {
                int xd = 0;
                xd = e[i];
                Console.Write("{0} ", xd);
            }
            if (dem == n-1)
            {
                Console.WriteLine("Day so tren la day dan dau ");
            }

            else
            {
                Console.WriteLine("mmb");
            }

            Console.Read();



        }
    }
}
