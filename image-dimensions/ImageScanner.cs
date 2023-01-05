using System;
using System.Text;

namespace Foxip.Image.Dimensions
{
    public static class ImageScanner
    {
        public static ImageInfo GetImageInfo(Stream stream)
        {
            ImageInfo imageInfo = new ImageInfo();

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            // Read the first 30 bytes
            byte[] buffer = new byte[30];
            if (stream.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                imageInfo.Error = "Too small";
                return imageInfo;
            }

            string s = Encoding.UTF8.GetString(buffer);

            // Determine image format
            if (s.StartsWith("GIF87a") || s.StartsWith("GIF89a"))
                imageInfo.ImageFormat = ImageFormat.Gif;
            else if ((buffer[0] == 0xFF) && (buffer[1] == 0xD8))
                imageInfo.ImageFormat = ImageFormat.Jpg;
            else if ((buffer[0] == 0x89) &&
                     (buffer[1] == 0x50) &&
                     (buffer[2] == 0x4e) &&
                     (buffer[3] == 0x47) &&
                     (buffer[4] == 0x0d) &&
                     (buffer[5] == 0x0a) &&
                     (buffer[6] == 0x1a) &&
                     (buffer[7] == 0x0a))
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
                    imageInfo.Width = BitConverter.ToUInt16(buffer, 6);
                    imageInfo.Height = BitConverter.ToUInt16(buffer, 8);
                    break;
                case ImageFormat.Bmp:
                    imageInfo.Width = BitConverter.ToUInt16(buffer, 18);
                    imageInfo.Height = BitConverter.ToUInt16(buffer, 22);
                    break;
                case ImageFormat.Psd:

                    // PSD version number should always be 1
                    if ((buffer[4] != 0 || buffer[5] != 1))
                    {
                        imageInfo.Error = "Invalid PSD format";
                        return imageInfo;
                    }

                    // Reverse the bytes
                    byte[] psdWidth = new byte[4];
                    byte[] psdHeight = new byte[4];

                    psdHeight[0] = buffer[17];
                    psdHeight[1] = buffer[16];
                    psdHeight[2] = buffer[15];
                    psdHeight[3] = buffer[14];

                    psdWidth[0] = buffer[21];
                    psdWidth[1] = buffer[20];
                    psdWidth[2] = buffer[19];
                    psdWidth[3] = buffer[18];

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

                    pngWidth[0] = buffer[19];
                    pngWidth[1] = buffer[18];
                    pngWidth[2] = buffer[17];
                    pngWidth[3] = buffer[16];

                    pngHeight[0] = buffer[23];
                    pngHeight[1] = buffer[22];
                    pngHeight[2] = buffer[21];
                    pngHeight[3] = buffer[20];

                    imageInfo.Width = BitConverter.ToInt32(pngWidth, 0);
                    imageInfo.Height = BitConverter.ToInt32(pngHeight, 0);
                    break;
                case ImageFormat.Jpg:

                    int pos = 2;
                    byte marker = 0;
                    UInt16 len = 0;
                    int offset = 0;
                    string state = "";

                    while ((true) && (pos < buffer.Length))
                    { 
                        // Marker starts with FF
                        if (buffer[pos] != 0xFF)
                        {
                            imageInfo.Error = "Invalid JPG format";
                            return imageInfo;
                        }

                        // Marker type
                        pos++;
                        marker = buffer[pos];

                        // Length
                        pos++;
                        buffer[0] = buffer[pos + 1]; // Reverse byte
                        buffer[1] = buffer[pos]; // Reverse byte
                        len = BitConverter.ToUInt16(buffer, 0);

                        // Read more from the stream
                        offset = buffer.Length;
                        buffer = Resize(buffer, buffer.Length + len);
                        int bytesToRead = len;
                        int bytesRead = stream.Read(buffer, offset, len);
                        while (bytesToRead != bytesRead)
                        {
                            bytesToRead = (ushort)(bytesToRead - bytesRead);
                            offset = offset + bytesRead;
                            bytesRead = stream.Read(buffer, offset, bytesToRead);
                        }

                        // SOF Marker for width/height
                        if (((marker >= 0xC0) && (marker <= 0xC3)) ||
                            ((marker >= 0xC5) && (marker <= 0xC7)) ||
                            ((marker >= 0xC9) && (marker <= 0xCB)) ||
                            ((marker >= 0xCD) && (marker <= 0xCF)))
                        {
                            buffer[0] = buffer[pos + 4]; // Reverse byte
                            buffer[1] = buffer[pos + 3]; // Reverse byte
                            imageInfo.Height = BitConverter.ToInt16(buffer, 0);

                            buffer[0] = buffer[pos + 6]; // Reverse byte
                            buffer[1] = buffer[pos + 5]; // Reverse byte
                            imageInfo.Width = BitConverter.ToInt16(buffer, 0);
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

