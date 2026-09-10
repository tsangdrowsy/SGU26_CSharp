using System;
namespace Bai6;
class TinhMu
{
    static void Main(String[] args)
    {
        Console.Write("Nhap so thuc a: ");
        double a=double.Parse(Console.ReadLine());
        Console.Write("Nhap so mu n: ");
        double n=double.Parse(Console.ReadLine());
        //tính kết quả a mũ n
        double c=Math.Pow(a,n);
        //ép kết quả in ra chỉ giữ 2 số thập phân
        Console.Write("Ket qua {0}^{1} la : {2}", a, n, c);
    }
}