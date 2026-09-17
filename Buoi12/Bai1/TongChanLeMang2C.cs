using System;
namespace Bai1;
class TongChanLeMang2C
{//hàm nhập mảng 2 chiều
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
    //hàm tính tổng các số chẵn và lẻ trong mảng 2 chiều
    static void ChanLe(int[,] a, int m, int n)
    {
        int tongChan=0;
        int tongLe=0;
        for (int i = 0; i < m; i++)
        {
            for (int j=0; j<n; j++)
            {
                if(a[i,j]%2==0)
                {
                    tongChan+=a[i,j];
                    
                }else
                {
                    tongLe+=a[i,j];
                }

            }
        }Console.WriteLine(" Tong các số chan là {0} và tong cac so le là {1}",tongChan,tongLe);}
public static void Main(string[] args)
    {
        Console.Write("Nhap so dong m: ");
        int m = int.Parse(Console.ReadLine());
        Console.Write("Nhap so cot n: ");
        int n = int.Parse(Console.ReadLine());

        int[,] a = new int[m, n];
        NhapMang(a, m, n);
        InMang(a, m ,n);
        ChanLe(a, m, n);
    }
}
