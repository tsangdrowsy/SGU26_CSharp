using System;

namespace Buoi6.Bai2
{
    class XuLyChuSo1
    {
        public static void Main(String[] args)
        {
            Console.Write("Moi ban nhap so nguyen n: ");
            int n= int.Parse(Console.ReadLine());
            
            //to get the number length cause I have no idea what the hell am doing
            String a=n.ToString();
            int x=a.Length;

            Console.Write("Moi ban nhap vi tri k: ");
            double k=int.Parse(Console.ReadLine());

            //nah not gonna use strings cause using it = no greater/lower operators

            int test=n;

            if (k>0&&k<=n)
            {
                double tempp=test%Math.Pow(10,x-k+1)/Math.Pow(10,x-k);
                int temp=(int)tempp;  //holy endless google search for a convertion operator
                Console.WriteLine("Chu so vi tri thu {0} (tinh tu trai) cua {1} la: {2}",k,n,temp);
            }

            int temppp=1;
            int TongL=0;
            int LGreat=0;
            int LSmall=n;

            //I'm praying with the values istg I just ripped off the calc above and just brainlessly twerk it
            while (temppp!=x+1)
            {
                double e=test%Math.Pow(10,x-temppp+1)/Math.Pow(10,x-temppp);
                int ee=(int)e;

                if (ee%2!=0)
                {
                    TongL+=ee;
                    if (ee>LGreat)
                    {
                        LGreat=ee;
                    }
                    if (ee<LSmall)
                    {
                        LSmall=ee;
                    }
                }
                temppp++;
            }

            Console.WriteLine("Tong cac chu so le cua {0} la: {1}",n,TongL);

            Console.WriteLine("{0} co chu so le nho nhat la {1} va lon nhat la {2}",n,LSmall,LGreat);

            Console.Read();




            
            

            Console.Read();
        }
    }
}