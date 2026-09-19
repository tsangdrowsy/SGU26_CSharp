using System;
using System.Data;
using System.Reflection.Metadata;


namespace Buoi10.Bai5
{
    class DayDanDau
    {
        public static void Main(String[] args)
        {
            Console.Write("Nhap so phan tu n: ");
            int n = int.Parse(Console.ReadLine());

            int[] e = new int[n]; //making a fixed array following the number size

            for (int i = 0; i < e.Length; i++)
            {
                int temp = int.Parse(Console.ReadLine());
                e[i] = temp;

            }

            Console.WriteLine("Day so co {0} phan tu: ", n);

            for (int i = 0; i < e.Length; i++)
            {
                int xd = 0;
                xd = e[i];
                Console.Write("{0} ", xd);
            }

        
            int max = 0;
            int demm = 0;
            int begin = 0;
            int end = 0;
            int[] x = new int[0];

            for (int i = 0; i < e.Length; i++)
            {
                if (e[i] > 0)
                {

                    demm++;

                    if (demm > max) //my idea but horrendously executed so ... tldr you make sure the current value is >0 then check if it's larger than the current max, if it is update the max streak, begin / end of the array section, then resize the result printing array and throw it in
                    {
                        max = demm;
                        end = i;
                        begin = i - max + 1;


                        Array.Resize(ref x, max);

                        int iX = 0;

                        for (int j = begin; j <= end; j++)
                        {
                            x[iX] = e[j];
                            iX++;
                        }

                    }

                }

                else
                {
                 
                    demm = 0;

                }

            }



            Console.WriteLine("Doan duong dai nhat[{0},{1}] dai nhat:", begin, end);

            for (int j = 0; j < x.Length; j++)
            {
                Console.Write("{0} ", x[j]);
            }


            Console.Read();








        }
    }
}



