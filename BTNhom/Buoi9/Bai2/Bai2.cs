using System;

namespace Buoi9.Bai2
{
    class TimKiemGiaTriChan
    {
        static void Main()
        {
            Console.Write("Moi ban nhap so luong phan tu: ");
            int n = int.Parse(Console.ReadLine());

            int[] a = new int[n];

            for (int i = 0; i < n; i++)
            {
                Console.Write("Phan tu {0}: ", i);
                a[i] = int.Parse(Console.ReadLine());
            }

            // Tìm giá trị chẵn ĐẦU TIÊN
            int viTriDau = -1;
            for (int i = 0; i < n; i++)
            {
                if (a[i] % 2 == 0)
                {
                    viTriDau = i;
                    break;  
                }
            }

            // Tìm giá trị chẵn CUỐI CÙNG
            int viTriCuoi = -1;
            for (int i = n - 1; i >= 0; i--)
            {
                if (a[i] % 2 == 0)
                {
                    viTriCuoi = i;
                    break; 
                }
            }

            // In kết quả
            if (viTriDau == -1)
            {
                Console.WriteLine("Mang khong co so chan.");
            }
            else
            {
                Console.WriteLine("Gia tri chan dau tien o vi tri {0} co gia tri {1}.", viTriDau, a[viTriDau]);
                Console.WriteLine("Gia tri chan cuoi cung o vi tri {0} co gia tri {1}.", viTriCuoi, a[viTriCuoi]);
            }
        }
    }
}