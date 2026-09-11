using System;
namespace Bai4
{
    class ThangTiengAnh
    {
                static void Main()
        {
            Console.Write("Moi ban nhap vao thang: ");
            int x = int.Parse(Console.ReadLine());

            string tenThang;

            switch (x)
            {
                case 1:  tenThang = "January";   break;
                case 2:  tenThang = "February";  break;
                case 3:  tenThang = "March";     break;
                case 4:  tenThang = "April";     break;
                case 5:  tenThang = "May";       break;
                case 6:  tenThang = "June";      break;
                case 7:  tenThang = "July";      break;
                case 8:  tenThang = "August";    break;
                case 9:  tenThang = "September"; break;
                case 10: tenThang = "October";   break;
                case 11: tenThang = "November";  break;
                case 12: tenThang = "December";  break;
                default: tenThang = "khong hop le"; break;
            }

            Console.WriteLine("Tieng anh cua thang {0} la {1}.,", x, tenThang);
        }
    }
}