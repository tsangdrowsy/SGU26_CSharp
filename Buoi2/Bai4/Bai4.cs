using System;
using System.Security.Cryptography.X509Certificates;


namespace Buoi2.Bai4{
    class TimQui
    {
        public static void Main(String[] args)
        {
            Console.Write("Moi ban nhap thang: ");
            int e= int.Parse(Console.ReadLine());

            //Thuần Bruteforce cause why would you actually calculate this?

            if (e<4)
            {
                Console.Write("Thang {0} thuoc qui: 1 ",e);
            }
            else if (e<7)
            {
                 Console.Write("Thang {0} thuoc qui: 2 ",e);
            }
            else if(e<10)
            {
                Console.Write("Thang {0} thuoc qui: 3 ",e);
            }
             else if(e<13)
            {
                Console.Write("Thang {0} thuoc qui: 4 ",e);
            }
            else 
            {
                Console.Write("Thang gi v cu? ");
            }
            Console.Read();
        }
    }
    
}