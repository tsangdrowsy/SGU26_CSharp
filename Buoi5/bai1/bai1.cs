using System;

namespace Buoi5.Bai1
{
    class GiaiThua
    {
        public static void Main(String[] args)
        {
            Console.Write("Moi ban nhap so n: ");
            int n = int.Parse(Console.ReadLine());
            
            long gt = 1; 

            Console.Write("{0}! = ", n);
            
            // vừa nhân dồn để tính kết quả, vừa in ra chuỗi dạng "1.2.3..."
            for (int i = 1; i <= n; i++)
            {
                gt *= i;
                Console.Write(i);
                
                // nếu kh phải số cuối cùng thì in thêm dấu chấm phân cách
                if (i < n) 
                {
                    Console.Write(".");
                }
            }
            
            // in kết quả tổng hợp ở cuối
            Console.WriteLine(" = {0}.", gt);

            Console.Read();
        }
    }
}