using System;
using System.IO;
using System.Text;

namespace Foxip.Image.Dimensions
{
    public static class ImageScanner
    {
        public static ImageInfo GetImageInfo(Stream stream)
        {
            ImageInfo imageInfo = new ImageInfo();

            // Read the first 30 bytes
            byte[] b = new byte[30];
            if (stream.Read(b, 0, b.Length) != b.Length)
            {
                imageInfo.Error = "Too small";
                return imageInfo;
            }

            string s = Encoding.UTF8.GetString(b);

            // Determine image format
            if (s.StartsWith("GIF87a") || s.StartsWith("GIF89a"))
                imageInfo.ImageFormat = ImageFormat.Gif;
            else if ((b[0] == 0xFF) && (b[1] == 0xD8))
                imageInfo.ImageFormat = ImageFormat.Jpg;
            else if ((b[0] == 0x89) &&
                     (b[1] == 0x50) &&
                     (b[2] == 0x4e) &&
                     (b[3] == 0x47) &&
                     (b[4] == 0x0d) &&
                     (b[5] == 0x0a) &&
                     (b[6] == 0x1a) &&
                     (b[7] == 0x0a))
                imageInfo.ImageFormat = ImageFormat.Png;
            else if (s.StartsWith("BM"))
                imageInfo.ImageFormat = ImageFormat.Bmp;
            else if (s.StartsWith("8BPS"))
                imageInfo.ImageFormat = ImageFormat.Psd;
            else
            {
                imageInfo.ImageFormat = ImageFormat.Unknown;
                return imageInfo;
            }

            // Find width & height
            switch (imageInfo.ImageFormat)
            {
                case ImageFormat.Gif:
                    imageInfo.Width = BitConverter.ToUInt16(b, 6);
                    imageInfo.Height = BitConverter.ToUInt16(b, 8);
                    break;
                case ImageFormat.Bmp:
                    imageInfo.Width = BitConverter.ToUInt16(b, 18);
                    imageInfo.Height = BitConverter.ToUInt16(b, 22);
                    break;
                case ImageFormat.Psd:

                    // PSD version number should always be 1
                    if ((b[4] != 0 || b[5] != 1))
                    {
                        imageInfo.Error = "Invalid PSD format";
                        return imageInfo;
                    }

                    // Reverse the bytes
                    byte[] psdWidth = new byte[4];
                    byte[] psdHeight = new byte[4];

                    psdHeight[0] = b[17];
                    psdHeight[1] = b[16];
                    psdHeight[2] = b[15];
                    psdHeight[3] = b[14];

                    psdWidth[0] = b[21];
                    psdWidth[1] = b[20];
                    psdWidth[2] = b[19];
                    psdWidth[3] = b[18];

                    imageInfo.Width = BitConverter.ToInt32(psdWidth, 0);
                    imageInfo.Height = BitConverter.ToInt32(psdHeight, 0);
                    break;
                case ImageFormat.Png:

                    // Check for IHDR block
                    string ihdr = s.Substring(12, 4);
                    if (ihdr != "IHDR")
                    {
                        imageInfo.Error = "Invalid PNG format";
                        return imageInfo;
                    }

                    // Reverse the bytes
                    byte[] pngWidth = new byte[4];
                    byte[] pngHeight = new byte[4];

                    pngWidth[0] = b[19];
                    pngWidth[1] = b[18];
                    pngWidth[2] = b[17];
                    pngWidth[3] = b[16];

                    pngHeight[0] = b[23];
                    pngHeight[1] = b[22];
                    pngHeight[2] = b[21];
                    pngHeight[3] = b[20];

                    imageInfo.Width = BitConverter.ToInt32(pngWidth, 0);
                    imageInfo.Height = BitConverter.ToInt32(pngHeight, 0);
                    break;
                case ImageFormat.Jpg:

                    int pos = 2;
                    byte marker = 0;
                    UInt16 len = 0;
                    int offset = 0;
                    string state = "";

                    while ((true) && (pos < b.Length))
                    { 
                        // Marker starts with FF
                        if (b[pos] != 0xFF)
                        {
                            imageInfo.Error = "Invalid JPG format";
                            return imageInfo;
                        }

                        // Marker type
                        pos++;
                        marker = b[pos];

                        // Length
                        pos++;
                        b[0] = b[pos + 1]; // Reverse byte
                        b[1] = b[pos]; // Reverse byte
                        len = BitConverter.ToUInt16(b, 0);

                        // Read more from the stream
                        offset = b.Length;
                        b = Resize(b, b.Length + len);
                        int bytesToRead = len;
                        int bytesRead = stream.Read(b, offset, len);
                        while (bytesToRead != bytesRead)
                        {
                            bytesToRead = (ushort)(bytesToRead - bytesRead);
                            offset = offset + bytesRead;
                            bytesRead = stream.Read(b, offset, bytesToRead);
                        }

                        // SOF Marker for width/height
                        if (((marker >= 0xC0) && (marker <= 0xC3)) ||
                            ((marker >= 0xC5) && (marker <= 0xC7)) ||
                            ((marker >= 0xC9) && (marker <= 0xCB)) ||
                            ((marker >= 0xCD) && (marker <= 0xCF)))
                        {
                            b[0] = b[pos + 4]; // Reverse byte
                            b[1] = b[pos + 3]; // Reverse byte
                            imageInfo.Height = BitConverter.ToInt16(b, 0);

                            b[0] = b[pos + 6]; // Reverse byte
                            b[1] = b[pos + 5]; // Reverse byte
                            imageInfo.Width = BitConverter.ToInt16(b, 0);
                            break;
                        }

                        // SOS Marker? (Start Of Stream)
                        if (marker == 0xDA)
                            break;

                        pos = pos + len;
                    }
                    break;
            }

            return imageInfo;
        }

        private static byte[] Resize(Array array, int newSize)
        {
            byte[] newArray = new byte[newSize];
            Array.Copy(array, 0, newArray, 0, array.Length);
            return newArray;
        }
    }
}

