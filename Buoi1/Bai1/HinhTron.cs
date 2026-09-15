using System;
namespace Bai1;
class HinhTron
{
    static void Main(String[] args)
    {
        Console.WriteLine("Nhap ban kinh hinh tron:");
        double r = double.Parse(Console.ReadLine());
        Console.WriteLine($"Dien tich hinh tron la: {Math.PI * r * r:F1}");
        Console.WriteLine($"Chu vi P = {2*Math.PI*r:F1}");
        
    }
}