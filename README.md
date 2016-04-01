# image-dimensions
Find the dimensions (width and height) of local or remote images by fetching as little as needed in csharp.

Supports: JPG, GIF, BMP, PNG, PSD

### example for parsing a local file
```c#
using (StreamReader reader = new StreamReader("test.png"))
{
    ImageInfo info = ImageScanner.GetImageInfo(reader.BaseStream);
    Console.WriteLine("Width: " + info.Width + "- Height: " + info.Height);
}
```

### example for parsing a remote file
```c#
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
```
