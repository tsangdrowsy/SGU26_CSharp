using System;

namespace Buoi8.Bai4
{
    class DemChuSo
    {
        static void Main()
        {
            Console.Write("Moi ban nhap so nguyen n: ");
            int n = int.Parse(Console.ReadLine());

            // 10 biến đếm cho 10 chữ số
            int dem0 = 0, dem1 = 0, dem2 = 0, dem3 = 0, dem4 = 0;
            int dem5 = 0, dem6 = 0, dem7 = 0, dem8 = 0, dem9 = 0;

            int temp = n;
            do
            {
                int chuSo = temp % 10;
                temp /= 10;

                switch (chuSo)
                {
                    case 0: dem0++; break;
                    case 1: dem1++; break;
                    case 2: dem2++; break;
                    case 3: dem3++; break;
                    case 4: dem4++; break;
                    case 5: dem5++; break;
                    case 6: dem6++; break;
                    case 7: dem7++; break;
                    case 8: dem8++; break;
                    case 9: dem9++; break;
                }
            } while (temp > 0);

            // In kết quả
            if (dem0 > 0) Console.WriteLine("Chu so 0 xuat hien {0} lan.", dem0);
            if (dem1 > 0) Console.WriteLine("Chu so 1 xuat hien {0} lan.", dem1);
            if (dem2 > 0) Console.WriteLine("Chu so 2 xuat hien {0} lan.", dem2);
            if (dem3 > 0) Console.WriteLine("Chu so 3 xuat hien {0} lan.", dem3);
            if (dem4 > 0) Console.WriteLine("Chu so 4 xuat hien {0} lan.", dem4);
            if (dem5 > 0) Console.WriteLine("Chu so 5 xuat hien {0} lan.", dem5);
            if (dem6 > 0) Console.WriteLine("Chu so 6 xuat hien {0} lan.", dem6);
            if (dem7 > 0) Console.WriteLine("Chu so 7 xuat hien {0} lan.", dem7);
            if (dem8 > 0) Console.WriteLine("Chu so 8 xuat hien {0} lan.", dem8);
            if (dem9 > 0) Console.WriteLine("Chu so 9 xuat hien {0} lan.", dem9);
        }
    }
}