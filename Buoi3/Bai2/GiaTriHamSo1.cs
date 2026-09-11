using System;
namespace Bai2;
class GiaTriHamSo1
{
    static void Main(String[] args)
    {
        double x,temp;
        double fx1=0,fx2=0;
        Console.WriteLine("moi ban nhap so thuc x:");
        x=double.Parse(Console.ReadLine());

        if(x<=0)
        {fx1=0;}
        else if(x>0 && x<=1)
        {fx1=x;}
        else if (x>1)
        {fx1=x*x*x*x;}

        temp=x*x+4*x+5;
        if(x<=2)
        {fx2=temp;}
        else
        {fx2=1/temp;}
        Console.WriteLine("f1({0})={1}",x,fx1);
        Console.WriteLine("f2({0})={1}",x,fx2);
      }
}