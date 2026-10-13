using System;

namespace Bai4;
class doiSangGioPhutGiay
{
    static void Main(String[] agrs)
    {
        Console.Write("Nhap vao tong so giay: ");
        int tong= int.Parse(Console.ReadLine());
        int tmp=tong;
        int gio= tmp/3600;
        tmp= tmp%3600;
        int phut=tmp/60;
        int giay=(tmp%60);
        Console.WriteLine("{0} ung voi thoi gian la: {1}:{2}:{3}",tong,gio,phut,giay);
        
    }

}