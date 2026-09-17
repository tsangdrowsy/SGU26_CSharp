using System;
namespace Bai2;
class DemSoTrenBienMang2C
{
   static void NhapMang(double[,] a, int m, int n)
    {
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
            {
                Console.Write($"a[{i},{j}] = ");
                a[i, j] = double.Parse(Console.ReadLine());
            }
        }
    }
    //hàm in mảng 2 chiều
    static void InMang(double[,] a, int m, int n)
    {
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
            {
                Console.Write($"{a[i, j]} ");
            }
            Console.WriteLine();
        }
    }
    static void DemSoTrenBien(double[,] a, int m, int n)
    {
        int demDuong = 0;
        int demAm = 0;
        int dem=0;
        for (int i = 0; i < m; i++)
        {
            for ( int j=0; j<n; j++)
            {
                if (i == 0 || i == m - 1 || j == 0 || j == n - 1)
                {if(a[i,j]>0)
                    {
                        demDuong++;
                    }else if(a[i,j]<0)
                    {
                        demAm++;
                    }else
                    {
                        dem++;
                    }
                    
                }
            }
        }Console.WriteLine("Tren duong bien co {0} so duong, {1} so am va {2} so khong. " ,demDuong,demAm,dem);
    }
static void Main(string[] args)
    {
        Console.Write("Nhap so dong m: ");
        int m = int.Parse(Console.ReadLine());
        Console.Write("Nhap so cot n: ");
        int n = int.Parse(Console.ReadLine());

        double[,] a = new double[m, n];
        NhapMang(a, m, n);
        Console.WriteLine("Mang 2 chieu vua nhap la: ");
        InMang(a, m ,n);
        DemSoTrenBien(a, m, n);
        
        
    }

}