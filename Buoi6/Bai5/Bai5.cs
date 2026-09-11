using System;

using System.Collections.Generic; //for da list cuh

namespace Buoi6.Bai5
{
    class LietKeSoHoanHao
    {
        public static void Main(String[] args)
        {
            Console.Write("Moi ban nhap so: ");
            int n = int.Parse(Console.ReadLine());

            int temp = 1;

            if (n <= 1)
            {
                Console.Write("m gay");

            }

            static bool IsPerfectNumber(double n) //ripped from leetcode cause too lazy :"c
            {

                if (n <= 1) return false;

                double sum = 1;

                for (double i = 2; i * i <= n; i++)
                {
                    if (n % i == 0)
                    {
                        sum += i;



                        if (i * i != n)
                        {
                            sum += n / i;
                        }
                    }


                }
                return sum == n;

            }

            List<double> listso = new List<double>();

            for (double i = 1; i <= n; i++)
            {
                if (IsPerfectNumber(i))
                {
                    listso.Add(i);
                }
            }

            string e = string.Join(", ", listso); //guh, cấu trúc join string
            Console.WriteLine("Cac so hoan hao trong doan [1,{0}] la: {1}", n, e);
            Console.Read();
        }

    }
}
