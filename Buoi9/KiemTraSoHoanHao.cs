using System;
namespace Bai2;
class KiemTraSoHoanHao
{
    public static int SoHoanHao(int n)
    {
        int tong=0;
        for(int i=1;i<n;i++)
        {
            if(n%i==0)
            {
                tong+=i;
            }
        }
        if(tong==n)
        {
            return 1;
        }
        else
        {
            return 0;   
        }
       
    }
    static void Main(string[] args)
    {
        Console.Write("Nhap vao so n: ");
        int n = int.Parse(Console.ReadLine());
        if(SoHoanHao(n)==1)
        {
            Console.Write("{0} = 1",n);
            for(int i=2;i<n;i++)
            {
                if(n%i==0)
                {
                    Console.Write(" + {0}",i);
                }
            }
            Console.WriteLine(" la so hoan hao ");
        }
        else
        {
            Console.WriteLine("{0} khong phai la so hoan hao",n);
        }
    }
}