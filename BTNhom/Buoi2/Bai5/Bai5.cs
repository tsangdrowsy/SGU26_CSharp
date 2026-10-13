using System;

namespace Buoi2.Bai5
{
    class HaiChuSoCuoi
    {
        public static void Main(String[] args)
        {
            Console.Write("Moi ban nhap so nguyen n: ");

            int n=int.Parse(Console.ReadLine());

            //guess I am brute forcing it yk convert it to String then just displaying the genuine last 2 numbers TwT

            double temp=Math.Pow(n,272);

            while (n>1){
        
            string tempp=temp.ToString();

            //cấu trúc cắt chuỗi nhanh(only on .net 6 above), thank you google search TwT

            string SoCuoi= tempp[^2..];

           
            Console.Write("{0}^272 co hai chu so cuoi cung la: {1} ",n,SoCuoi);
            } 
           
           //fail safe condition for dumbos tryna test it with invalid numbers
            Console.Write("You dumb cuh-");
            

            Console.Read();
        }
    }
}