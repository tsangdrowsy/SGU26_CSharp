using System;

namespace Buoi8.Bai3
{
    class RutGonPhanSo
    {
    //    static int U(int a, int b)
    //    {
    //        if (b == 0) return a;
    //        return UCLN(b, a % b);
    //    }
        static int U(int a, int b)
        {
            while (b != 0)
            {
                int temp = a % b;
                a = b;
                b = temp;
            }
            return a;
        }

        static void Main()
        {
            Console.Write("Moi ban nhap tu so: ");
            int tu = int.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap mau so: ");
            int mau = int.Parse(Console.ReadLine());
            int p = U(Math.Abs(tu), Math.Abs(mau));
            int y  = tu / p;
            int z = mau / p;
            Console.WriteLine("Phan so {0}/{1} duoc rut gon thanh {2}/{3}.", tu, mau, y, z);
        }
    }
}