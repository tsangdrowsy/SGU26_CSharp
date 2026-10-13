using System;

namespace Buoi2.Bai1
{
    class TinhTongDoan
    {
        public static void Main(String[] args)
        {
            Console.Write("Moi ban nhap so a, b: ");
            
            int a = int.Parse(Console.ReadLine());  
            int b = int.Parse(Console.ReadLine());
            
            int temp=0;
            int tempa=a;
            while (tempa<=b)
            {
                temp = temp+tempa;
                tempa++;
            }
            //initial solution to the problem
            Console.WriteLine("Tong cac so trong doan [{0},{1}] la: {2}", a,b,temp);

            //alternative way
            int tempbb=1;
            int tempaa=1;

            int temppa=0;
            int temppb=0;
            while (tempaa<a)
            {
                temppa=temppa+tempaa;
                tempaa++;
            }

            while(tempbb<=b)
            {
                temppb=temppb+tempbb;
                tempbb++;
            }
            int tempp = temppb-temppa;




           

            Console.WriteLine("Alternative answer testing: {0}",tempp);
            Console.Read();

        }
    }
}