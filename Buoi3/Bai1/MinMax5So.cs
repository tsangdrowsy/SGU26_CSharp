using System;
namespace Bai1;
class MaxMin5So
{
    static void Main(String[] args)
    {
        int a,b,c,d,e;
        int max;
        int min;
        int temp=1;

        Console.Write("Nhap so a,b,c,d,e:");
        a= int.Parse(Console.ReadLine());
        b= int.Parse(Console.ReadLine());
        c= int.Parse(Console.ReadLine());
        d= int.Parse(Console.ReadLine());
        e= int.Parse(Console.ReadLine());

        max=a;
        min=a;
        while(temp<=4)
        {
            int x=0;

            switch(temp)
            {
                case 1 : x=b; break;
                case 2 : x=c; break;
                case 3 : x=d; break;
                case 4 : x=e; break;
            }
            if(x < min) min= x;
            if(x > max) max= x;
            
            temp++;
        }
        Console.WriteLine("Gia tri lon nhat cua {0},{1},{2},{3},{4} la {5}",a,b,c,d,e,max);
        Console.WriteLine("Gia tri nho nhat cua {0},{1},{2},{3},{4} la {5}",a,b,c,d,e,min);
        Console.Read();
    }
}