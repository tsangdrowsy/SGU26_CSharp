using System;
namespace Bai5;
class DienTichTamGiac
{
    static void Main(String[] args)
    {
        Console.Write("Nhap do dai canh a: ");
        double a=double.Parse(Console.ReadLine());
        Console.Write("Nhap do dai canh b: ");
        double b=double.Parse(Console.ReadLine());
        Console.Write("Nhap do dai canh c: ");
         double c=double.Parse(Console.ReadLine());
         //ép kiểu p ra kết quả thập phân
         double p=(double)(a+b+c)/2;
         //tính kết quả diện tích tam giác
         double S= (double)(p*(p-a)*(p-b)*(p-c));
      //ép kết quả in ra chỉ giữ 2 số thập phân
        Console.Write("Dien tich tam giac bang : "+ $"{ S:F2}");
        

    }
}