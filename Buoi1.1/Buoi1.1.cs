using System;


namespace CSharp.Buoi1
{
    class DoiSangGiay
    {
        public static void Main(string[] args)
        {
            int gio, phut, giay;

            Console.Write("Nhap so gio: ");
            gio = int.Parse(Console.ReadLine());
            Console.Write("Nhap so phut: ");
            phut = int.Parse(Console.ReadLine());
            Console.Write("Nhap so giay: ");
            giay =int.Parse(Console.ReadLine());


            int tonggiay= gio*3600+phut*60+giay;

            Console.WriteLine("Tong so giay cua {0}:{1}:{2} la: {3}",gio,phut,giay,tonggiay);
            Console.Read();
        }
    }
}