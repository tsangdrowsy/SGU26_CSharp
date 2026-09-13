using System;
using System.Reflection.Metadata;


namespace Buoi10.Bai1
{
    class KeNhauTraiDau
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

            int dem=0;
            for (int i=0;i<e.Length-1;i++)
            {
                if (e[i]>0 && e[i+1]<0 || e[i]<0&&e[i+1]>0)
                {
                    
                    dem++;
                }

            }
            

            Console.WriteLine("Day so co {0} phan tu: ",n);

            for (int i=0;i<e.Length;i++)
            {
                int xd=0;
                xd=e[i];
                Console.Write("{0} ",xd);
            }

            Console.WriteLine("So luong cac phan tu ke nhau ma trai dau: {0}",dem);
            Console.Read();



        }


    }

}