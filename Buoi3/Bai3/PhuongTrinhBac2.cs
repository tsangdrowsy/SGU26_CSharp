using System;
namespace Bai3;
class PhuongTrinhBac2
{
    static void Main(String[] args)
    {
        double a,b,c;
        Console.WriteLine("moi ban nhap he so a:");
        a=double.Parse(Console.ReadLine());
        Console.WriteLine("moi ban nhap he so b:");
        b=double.Parse(Console.ReadLine());
        Console.WriteLine("moi ban nhap he so c:");
        c=double.Parse(Console.ReadLine());
        double S= b*b-4*a*c;
         if (S < 0)
            {
                Console.WriteLine("vo nghiem");
            }
            else if (S== 0)
            {
                double x = -b / (2 * a);
                Console.WriteLine("1 nghiem kep, x1 = x2 = {0:F2}", x);
            }
            else
            {
                double x1 = (-b + Math.Sqrt(S)) / (2 * a);
                double x2 = (-b - Math.Sqrt(S)) / (2 * a);
                 Console.WriteLine("phuong trinh bac 2 {0}x^2 + {1}x + {2} = 0 co 2 nghiem x1={3}, x2={4}:",a,b,c,x1,x2);
            }
       
        
     }
}