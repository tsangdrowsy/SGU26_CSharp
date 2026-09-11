using System;

namespace Buoi6.Bai1
{
    class InChuIU
    {
        public static void Main(String[] args)
        {
            Console.Write("Moi ban nhap chieu cao h: ");
            int h = int.Parse(Console.ReadLine());

            //I am bruteforcing this shit gng :sob:

            int temp = h-2;
            int tempp=0;
            if (h>=3)
            {
                Console.WriteLine("*  *  *  *  *        *            *");

                while (tempp !=temp)
                {
                    Console.WriteLine("       *             *            *");
                    tempp++;
                }
                Console.WriteLine("*  *  *  *  *       * * * * * * * *");
            }

            //failsafe h<2 to block scums tryna break this cause it's pure bruteforce
            else
            {
                Console.WriteLine("what do you expect, it's just stars and spaces I cannot do it with less than the height of 3 gng :sob:");
            }
            Console.Read();
        }
    }
}