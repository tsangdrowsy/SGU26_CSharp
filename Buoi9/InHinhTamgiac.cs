using System;
namespace Buoi9;

    class InHinhTamgiac
    {

public static void InHinhTamGiac(int vtx,int vty,int h)
{
 int i,j;//biến j để xác định hàng i xác định thứ tự cột
 // In đỉnh của tam giác(dòng 1)
 Console.SetCursorPosition(vtx,vty);
        for (i = 1; i <= h-1; i++)
        {
            Console.Write("{0,2}"," ");
        };
        Console.WriteLine("{0,2}","*");
        //Dòng 2 đến dòng h-1
        for (i = 2; i <= h - 1; i++)
        {
            Console.SetCursorPosition(vtx, vty + i - 1);
            for(j=0;j<=(h-i)*2-1;j++)//Tại sao nhân 2 nhỉ???
            {
                Console.Write(" ");
            }Console.Write("{0,2}","*");
            for(j=1;j<=2*(i-1)-1;j++)
            {
                Console.Write("{0,2}"," ");
            }
            Console.WriteLine("{0,2}","*");
        }
  // In Tầng đáy của tam giác
  Console.SetCursorPosition(vtx, vty + h - 1);
  for(j=1;j<=2*h-1;j++)
  {
      Console.Write("{0,2}","*");
  }
}

        static void Main(string[] args)
        {
            Console.Write("Nhap vao chiều cao h tam giac: ");
            int h = int.Parse(Console.ReadLine());
          InHinhTamGiac(Console.CursorLeft, Console.CursorTop, h);
        }
    }
    