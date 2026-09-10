using System;


namespace Buoi2.Bai2
{
    class LuyThucNhanh1{

    
        public static void Main(String[] args)
        {
            Console.Write("Moi ban nhap so thuc a: ");
            int a= int.Parse(Console.ReadLine());

            //ờm bruteforce nhân 6 lần i guess?

            int temp1=a*a;

            int temp2=temp1*a;

            int temp3=temp1*temp2;

            int temp4=temp3*temp2;

            int temp5=temp4*temp4;

            int temp6=temp5*a;

            Console.WriteLine("Ket qua: {0}^2={1}, {0}^5={2}, {0}^17={3}",a,temp1,temp3,temp6);
            Console.Read();
        }
    }
}