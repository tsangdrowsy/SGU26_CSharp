using System;
using System.Collections.Generic; //THƯ VIỆN CHO LIST CAUSE WHY THE FUCK DIDN'T I HAVE THIS
namespace Buoi6.Bai2
{
    class XuLyChuSo1
    {
        public static void Main(String[] args)
        {
            Console.Write("Moi ban nhap so n: ");
            int n= int.Parse(Console.ReadLine());


            int temp=1;
            int tempp;
            List<int> a = new List<int>();
            while (temp!=n)
            {
                tempp=n%temp;
                if (tempp==0)
                {
                   
                    a.Add(temp);
                    
                }
                
                
                temp++;
                
            }

            int l=a.Count;
            Console.Write("{0}=",n);
            for (int i=0;i<l;i++)
            {
                Console.Write(a[i]);
                if (i<l-1)
                {
                    Console.Write("+");
                }
            }
            Console.Write(" la so hoan hao");
            Console.Read();
        }
    }
}