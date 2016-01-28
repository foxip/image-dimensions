using System;
using System.IO;
using System.Net;

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

            string url = "http://lorempixel.com/400/200/";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
            {
                using (Stream inputStream = response.GetResponseStream())
                {
                    ImageInfo info = ImageScanner.GetImageInfo(inputStream);
                    Console.WriteLine("Width: " + info.Width + "- Height: " + info.Height);
                }
            }
        }
    }
}
