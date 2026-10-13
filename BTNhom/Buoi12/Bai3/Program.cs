using System;
namespace Bai3;

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
    static int Ktra(int[,] a, int m, int n)
    {
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (a[i, j] % 2 == 0)
                {
                    return 0;
                    break;
                }
            }
        }return 1;
    }
    public static void Main(string[] args)
    {
        Console.Write("Nhap so dong m: ");
        int m = int.Parse(Console.ReadLine());
        Console.Write("Nhap so cot n: ");
        int n = int.Parse(Console.ReadLine());

        int[,] a = new int[m, n];
        NhapMang(a, m, n);
        InMang(a, m, n);
        if (Ktra(a, m, n) == 1)
        {
            Console.WriteLine("Mang A toan le");
        }
        else
        {
            Console.WriteLine("Mang A khong toan le");
        }
    }
}