using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SeamCarving
{
    class ResizeImage
    {
        private byte[,] rband;
        private byte[,] gband;
        private byte[,] bband;
        private byte[,] alpha;

        private byte[] bitmapSourceToArray(BitmapSource bitmapSource, int stride)
        {
            // Stride = (width) * (bytes per pixel)
            byte[] pixels = new byte[(int)bitmapSource.PixelHeight * stride];
      
            bitmapSource.CopyPixels(pixels, stride, 0);

            return pixels;
        }

        private BitmapSource bitmapSourceFromArray(byte[] pixels, int width, int height)
        {
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * (bitmap.Format.BitsPerPixel / 8), 0);

            return bitmap;
        }

        public BitmapSource halfResize(ref BitmapImage img)
        {
            //构造输入图像像素数组
            int h = img.PixelHeight;
            int w = img.PixelWidth;

            int stride = w * (img.Format.BitsPerPixel / 8);

            //获取像素数据
            byte[] imgPixels = bitmapSourceToArray(img, stride);
            //将像素数组分别转换为rgb数组
            rband = new byte[h, w];
            gband = new byte[h, w];
            bband = new byte[h, w];
            alpha = new byte[h, w];
            int p = 0;
            for (int i = 0; i < h; ++i)
            {
                for (int j = 0; j < w; ++j)
                {
                    alpha[i, j] = imgPixels[p + 3];
                    rband[i, j] = imgPixels[p + 2];   // 每个像素的指针是按BGRA的顺序存储的
                    gband[i, j] = imgPixels[p + 1];
                    bband[i, j] = imgPixels[p];
                    p += 4;   // 偏移一个像素
                }
            }

            int[,] d = generateD(ref rband, ref gband, ref bband, h, w);
            int[,] s = new int[h, w];
            


            int m;
            int n;

            //图像翻转

            //根据rgb数组构造返回byte数组
            int resW = w;
            int resH = h;
            byte[] resPixels = new byte[resH * resW * 4];
            int resP = 0;
            for (int i = 0; i < resH; ++i)
            {
                for (int j = 0; j < resW; ++j)
                {
                    resPixels[resP] = bband[i, j];
                    resPixels[resP + 1] = gband[i, j];
                    resPixels[resP + 2] = rband[i, j];
                    resPixels[resP + 3] = alpha[i, j];
                    resP += 4;   // 偏移一个像素
                }
            }

            //根据返回数组生成输出图像
            BitmapSource resBitmapSource = bitmapSourceFromArray(resPixels, resW, resH);
            return resBitmapSource;
        }
        
        private int[,] generateD (ref byte[,] r, ref byte[,] g, ref byte[,] b, int h, int w)
        {
            int[,] d = new int[h, w];
            for(int i = 0; i < h; i++)
            {
                d[i, 0] = (Math.Abs(r[i, 1] - r[i, 0]) + Math.Abs(g[i, 1] - g[i, 0]) + Math.Abs(b[i, 1] - b[i, 0]))*2;
                d[i, w - 1] = (Math.Abs(r[i, w-1] - r[i, w-2]) + Math.Abs(g[i, w-1] - g[i, w-2]) + Math.Abs(b[i, w-1] - b[i, w-2]))*2;
                for (int j = 1; j < w - 1 ; j++)
                {
                    d[i, j] = Math.Abs(r[i, j - 1] - r[i, j]) + Math.Abs(g[i, j - 1] - g[i, j]) + Math.Abs(b[i, j - 1] - b[i, j]) + Math.Abs(r[i, j + 1] - r[i, j]) + Math.Abs(g[i, j + 1] - g[i, j]) + Math.Abs(b[i, j + 1] - b[i, j]);

                }
            }
            return d;
        }
            

    }
}
