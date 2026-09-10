using System;
namespace Bai3
{
    class DoisangGiay
    {
        static void Main(string[] args)
        {
            Console.WriteLine(" Nhap so gio:");
            int gio = int.Parse(Console.ReadLine());
            Console.WriteLine(" Nhap so phut:");
            int phut = int.Parse(Console.ReadLine());
            Console.WriteLine(" Nhap so giay:");
            int giay = int.Parse(Console.ReadLine());
            int tmpgio = gio * 3600;
            int tmpphut = phut * 60;
            int tonggiay = tmpgio + tmpphut + giay;
            Console.WriteLine("Tong so giay cua {0}:{1}:{2} la {3} ", gio, phut, giay, tonggiay);
            Console.ReadLine();
        }
    }
}