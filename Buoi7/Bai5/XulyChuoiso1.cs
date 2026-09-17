using System;

namespace XuLyChuSo1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Moi ban nhap so nguyen n: ");
            string nStr = Console.ReadLine(); 

            Console.Write("Moi ban nhap vi tri k: ");
            int k = int.Parse(Console.ReadLine());

            // Tìm chữ số thứ k
            char chuSoThuK = TimChuSoThuK(nStr, k);
            Console.WriteLine($"Chu so o vi tri {k} (tinh tu trai) cua {nStr} la {chuSoThuK}.");

            //Tính tổng các chữ số lẻ
            int tongLe = TinhTongSoLe(nStr);
            Console.WriteLine($"Tong cac chu so le cua {nStr} la {tongLe}.");

            // Tìm Min, Max chữ số lẻ
            
            if (TimMinMaxSoLe(nStr, out int minLe, out int maxLe))
            {
                Console.WriteLine($"{nStr} co chu so le nho nhat la {minLe} va lon nhat la {maxLe}.");
            }
            else
            {
                Console.WriteLine($"{nStr} khong co chu so le nao.");
            }

        }


        
        /// (a) Phương thức lấy chữ số ở vị trí k (từ trái sang)
               static char TimChuSoThuK(string nStr, int k)
        {
            // Vị trí thực tế trong mảng/chuỗi bắt đầu từ 0
            return nStr[k - 1];
        }

        
        /// (b) Phương thức tính tổng các chữ số lẻ
        
       static int TinhTongSoLe(string nStr)
        {
            int tong = 0;
            for (int i = 0; i < nStr.Length; i++)
            {
                int chuSo = nStr[i] - '0';
                if (chuSo % 2 != 0)
                {
                    tong += chuSo;
                }
            }
            return tong;
        }

     
        /// (c) Phương thức tìm chữ số lẻ nhỏ nhất và lớn nhất. 
        /// Trả về true nếu có số lẻ, ngược lại trả về false.
        
        static bool TimMinMaxSoLe(string nStr, out int minLe, out int maxLe)
        {
            minLe = 10;
            maxLe = -1;
            bool coSoLe = false;

            for (int i = 0; i < nStr.Length; i++)
            {
                int chuSo = nStr[i] - '0';
                if (chuSo % 2 != 0) // Nếu là số lẻ
                {
                    if (chuSo < minLe) minLe = chuSo;
                    if (chuSo > maxLe) maxLe = chuSo;
                    coSoLe = true;
                }
            }
            
            return coSoLe;
        }
    }
}