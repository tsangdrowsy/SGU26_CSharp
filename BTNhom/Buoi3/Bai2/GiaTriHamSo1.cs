using System;
namespace Buoi3.Bai2;
class GiaTriHamSo1
{
    static void Main(String[] args)
    {
        double x,temp;
        double fx1=0,fx2=0;
        Console.WriteLine("moi ban nhap so thuc x:");
        x=double.Parse(Console.ReadLine());

        if(x<=0)   // Nếu x nhỏ hơn hoặc bằng 0, giá trị của hàm f1(x) là 0
        {fx1=0;}
        else if(x>0 && x<=1) // Nếu x lớn hơn 0 và nhỏ hơn hoặc bằng 1, giá trị của hàm f1(x) là x
        {fx1=x;}
        else if (x>1) //Nếu x lớn hơn 1, giá trị của hàm f1(x) là x^4
        {fx1=x*x*x*x;}

        temp=x*x+4*x+5;
        if(x<=2) // Nếu x nhỏ hơn hoặc bằng 2, giá trị của hàm f2(x) là temp
        {fx2=temp;}
        else
        {fx2=1/temp;} // Nếu x lớn hơn 2, giá trị của hàm f2(x) là 1/temp
        Console.WriteLine("f1({0})={1}",x,fx1);
        Console.WriteLine("f2({0})={1}",x,fx2);
      }
}