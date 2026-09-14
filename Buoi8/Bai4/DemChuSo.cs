using System;

namespace Buoi8.Bai4
{
    class DemChuSo
    {
        static void Main()
        {
            Console.Write("Moi ban nhap so nguyen n: ");
            int n = int.Parse(Console.ReadLine());
            int[] dem = new int[10];
            int temp = n;
            if(temp==0)
            {
                dem[0]=1;
            }
            else
            {
            while (temp > 0)
                {
                    int chuSo = temp % 10;  
                    dem[chuSo]++;            
                    temp /= 10;             
                }
            }
            for (int i = 0; i <= 9; i++)
            {
                if (dem[i] > 0)
                    Console.WriteLine("Chu so {0} xuat hien {1} lan.", i, dem[i]);
            }
        }
    }
}