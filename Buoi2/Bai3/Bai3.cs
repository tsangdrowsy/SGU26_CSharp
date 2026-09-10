using System;

namespace Buoi2.Bai3
{
    class BieuThucNhanh1
    {
        public static void Main(String[] args)
        {
            Console.Write("Moi ban nhap so x: ");
            int x= int.Parse(Console.ReadLine());

            int temp=x*x;
            int ans=1+2*x+(3*temp)-4*(temp*x);

            // 2 phép cộng, 1 phép trừ, 5 phép nhân = 8 phép tính

            Console.WriteLine("Ket qua f{0}: {1}",x,ans);
            Console.Read();
        }
    }
}