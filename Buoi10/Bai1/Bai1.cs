using System;
using System.Security.Cryptography.X509Certificates;

namespace Buoi10.Bai1
{
    class TrungBinhCongCucTri
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

            int[] x = new int[0];
            int dem = 0;
            for (int i = 1; i < n - 1; i++)
            {
                if ((e[i - 1] > e[i] && e[i] < e[i + 1]) || (e[i - 1] < e[i] && e[i] > e[i + 1]))
                {

                    dem++;
                    Array.Resize(ref x, dem);
                    x[dem - 1] = e[i];
                }
            }
            int ans;
            int tempp = 0;
            for (int i = 0; i < dem; i++)
            {

                tempp = tempp + x[i];

            }
            ans = tempp;

            Console.Write("Tong cac phan tu cuc tri trong day so la: {0}", ans); //ra là 1 wth gang, cực trị thì là 5 và 2 là đúng, but -6 also cực trị so đề ra 7 là bịp istg, nếu chí cực trị dương thì sẽ đúng but đề doesn't say so, idc


        }
    }
}

