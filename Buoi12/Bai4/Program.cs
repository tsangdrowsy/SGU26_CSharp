using System;
namespace Bai4;

class program
{
    static void NhapMang(int[,] a, int m, int n)
    {
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
            {
                Console.Write($"a[{i},{j}] = ");
                a[i, j] = int.Parse(Console.ReadLine());
            }
        }
    }
    //hàm in mảng 2 chiều
    static void InMang(int[,] a, int m, int n)
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
    static void TangDan(int[,] a, int m, int n)
    {int dem = 0;
        bool Tang = true;
        Console.Write("Cac dong tang dan la: ");
        for (int i = 0; i < m; i++)
        {
            Tang = true; 
            for (int j = 0; j < n-1; j++)
            {
                if (a[i, j] > a[i, j + 1])
                {
                    Tang = false;
                    break;
                }

            }
            
            if (Tang == true)
            {
                Console.Write(dem + " ");
            }dem++;


        }
    }
    static void Main(string[] args)
    {
        Console.Write("Nhap so dong m: ");
        int m = int.Parse(Console.ReadLine());
        Console.Write("Nhap so cot n: ");
        int n = int.Parse(Console.ReadLine());

        int[,] a = new int[m, n];
        NhapMang(a, m, n);
        InMang(a, m, n);
        TangDan(a, m, n);
    }
}