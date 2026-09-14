using System;
namespace Bai4;
class NgayTruoc
{
    public static void TinhNgayTruoc(int d,int m,int y)
    {
        int ngay=d,thang=m,nam=y;
        if(ngay==1)
        {
            if(thang==1)
            {
                ngay=31;
                thang=12;  
                nam--;}
            else
            {
                thang--;
                if(thang==2)
                {
                    if(nam%4==0 && nam%100!=0 || nam%400==0)
                    {
                        ngay=29;
                    }
                    else
                    {
                        ngay=28;
                    }
                }
                else
                {
                    switch(thang)
                    {
                        case 4:
                        case 6:
                        case 9:
                        case 11:
                            ngay=30;
                            break;
                        default:
                            ngay=31;
                            break;
                    }
                }
            }
        }
        Console.WriteLine("Ngay truoc Ngay {0}/{1}/{2} la: {3}/{4}/{5}", d, m, y, ngay, thang, nam);
    }
    static void Main(string[] args)
    {
        Console.Write("Nhap vao ngay: ");
        int d = int.Parse(Console.ReadLine());
        Console.Write("Nhap vao thang: ");
        int m = int.Parse(Console.ReadLine());
        Console.Write("Nhap vao nam: ");
        int y = int.Parse(Console.ReadLine());
        TinhNgayTruoc(d,m,y);
    }
}