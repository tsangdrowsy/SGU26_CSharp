using System;
class TinhSoPi
{
    public static double TinhSopi(double el)
    {
        int n = 1;
        double pi = 4;//biến pi lưu tổng giá trị bằng 4 là số đầu tiên khi n=0
        n++;
        double dau = -1;//biến dau để xác định dấu của số hạng tiếp theo
        double ai = 4.0 / (2 * n + 1);//biến ai lưu giá trị của số hạng tiếp theo
while(ai>el)
        {
            pi += dau * ai;
            n++;
            dau *= -1;
            ai = 4.0 / (2 * n + 1);
        }
        return pi;
    }
    static void Main(String [] args)
    {
        Console.Write("Moi ban nhap vao do sai so: ");
        double el = double.Parse(Console.ReadLine());
        double pi = TinhSopi(el);
        Console.WriteLine("Gia tri so pi la: {0}",pi);
        Console.WriteLine("Do sai so thuc te la: {0}",Math.Abs(Math.PI-pi));// độ sai số là trị tuyệt đối và đc lấy theo số Pi của C#
    }
}