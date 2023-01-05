using System;

namespace Foxip.Image.Dimensions
{
   class Program
   {
      static void Main()
      {
         using (StreamReader reader = new StreamReader("test.png"))
         {
            ImageInfo info = ImageScanner.GetImageInfo(reader.BaseStream);
            Console.WriteLine("Width: " + info.Width + "- Height: " + info.Height);
         }        
      }
   }
}
