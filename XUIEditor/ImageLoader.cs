using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Pfim;

namespace XUIEditor
{
    public static class ImageLoader
    {
        [HandleProcessCorruptedStateExceptions]
        public static Image Load(byte[] data, string ext)
        {
            try
            {
                ext = (ext ?? "").ToLowerInvariant();
                if (ext == ".dds")
                {
                    var img = Dds.Create(data, new PfimConfig(32768, 0, true));
                    return LoadPfimImage(img);
                }
                else if (ext == ".tga")
                {
                    var img = Targa.Create(data, new PfimConfig(32768, 0, true));
                    return LoadPfimImage(img);
                }
                else
                {
                    using var ms = new MemoryStream(data);
                    return Image.FromStream(ms);
                }
            }
            catch
            {
                return null;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private unsafe static Image LoadPfimImage(IImage img)
        {
            PixelFormat pf = img.Format switch
            {
                Pfim.ImageFormat.Rgb24 => PixelFormat.Format24bppRgb,
                Pfim.ImageFormat.Rgba32 => PixelFormat.Format32bppArgb,
                _ => throw new NotSupportedException($"Unsupported Pfim format {img.Format}")
            };

            var raw = img.Data.ToArray();
            int width = img.Width;
            int height = img.Height;
            int stride = img.Stride;

            var tempBmp = new Bitmap(width, height, stride, pf, (IntPtr)Marshal.UnsafeAddrOfPinnedArrayElement(raw, 0));

            var safeBmp = new Bitmap(tempBmp);

            tempBmp.Dispose();

            return safeBmp;
        }

    }
}
