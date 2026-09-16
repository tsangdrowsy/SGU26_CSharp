using System;

namespace Buoi9.Bai4
{
    class TimKiem
    {
        static void Main()
        {
            Console.Write("Moi ban nhap so luong phan tu: ");
            int n = int.Parse(Console.ReadLine());

            double[] a = new double[n];

            for (int i = 0; i < n; i++)
            {
                Console.Write("Phan tu {0}: ", i);
                a[i] = double.Parse(Console.ReadLine());
            }

            // In dãy số
            Console.Write("Day so co {0} phan tu: ", n);
            for (int i = 0; i < n; i++)
            {
                Console.Write("{0} ", a[i]);
            }
            Console.WriteLine();

            // Nhập x cần tìm
            Console.WriteLine("* Tim kiem");
            Console.Write("Moi ban nhap gia tri x can tim: ");
            double x = double.Parse(Console.ReadLine());

            // Tìm kiếm
            int viTriDau = -1;
            int viTriCuoi = -1;
            int dem = 0;

            for (int i = 0; i < n; i++)
            {
                if (a[i] == x)
                {
                    if (viTriDau == -1)
                        viTriDau = i;   // ghi nhận vị trí đầu tiên
                    viTriCuoi = i;      // luôn cập nhật vị trí cuối
                    dem++;
                }
            }

            // In kết quả
            if (dem == 0)
            {
                Console.WriteLine("Gia tri {0} khong ton tai trong day so.", x);
            }
            else
            {
                Console.WriteLine("Gia tri {0} xuat hien {1} lan o vi tri dau tien {2} va cuoi cung {3}.", x, dem, viTriDau, viTriCuoi);
            }
        }
    }
}