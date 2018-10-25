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

        public BitmapSource widthResize(ref BitmapSource img)
        {
            //构造输入图像像素数组
            int h = img.PixelHeight;
            int w = img.PixelWidth;

            int stride = w * (img.Format.BitsPerPixel / 8);

            //获取像素数据
            byte[] imgPixels = bitmapSourceToArray(img, stride);
            //将像素数组分别转换为rgb数组
            byte[,] rband = new byte[h, w];
            byte[,] gband = new byte[h, w];
            byte[,] bband = new byte[h, w];
            byte[,] alpha = new byte[h, w];
            int p = 0;
            for (int i = 0; i < h; ++i)
            {
                for (int j = 0; j < w; ++j)
                {
                    // 每个像素的指针是按BGRA的顺序存储
                    alpha[i, j] = imgPixels[p + 3];
                    rband[i, j] = imgPixels[p + 2];   
                    gband[i, j] = imgPixels[p + 1];
                    bband[i, j] = imgPixels[p];
                    p += 4;   // 偏移一个像素
                }
            }
            //生成破坏度数组d和s
            int[,] d = generateD(img, h, w);
            int[,] s = generateS(ref d, h, w);
            //找到接缝
            int[] path = generatePath(ref s, h, w); 
            //根据接缝缩小图像宽度
            for(int i = h-1; i >= 0; i--)
            {
                int j = path[i];
                if (j != 0)
                {
                    rband[i, j - 1] = (byte)((rband[i, j - 1] + rband[i, j]) / 2);
                    gband[i, j - 1] = (byte)((gband[i, j - 1] + gband[i, j]) / 2);
                    bband[i, j - 1] = (byte)((bband[i, j - 1] + bband[i, j]) / 2);
                }
                if (j != w - 1)
                {
                    rband[i, j] = (byte)((rband[i, j] + rband[i, j + 1]) / 2);
                    gband[i, j] = (byte)((gband[i, j] + gband[i, j + 1]) / 2);
                    bband[i, j] = (byte)((bband[i, j] + bband[i, j + 1]) / 2);
                }
                while (j < w - 2)
                {
                    j++;
                    rband[i, j] = rband[i, j + 1];
                    gband[i, j] = gband[i, j + 1];
                    bband[i, j] = bband[i, j + 1];
                    alpha[i, j] = alpha[i, j + 1];
                }
            }
            //根据rgb数组构造返回byte数组
            int resW = w-1;
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
        
        private int[,] generateD (BitmapSource img, int h, int w)
        {
            int[,] d = new int[h, w];
            //for (int i = 0; i < h; i++)
            //{
            //    d[i, 0] = (Math.Abs(r[i, 1] - r[i, 0]) + Math.Abs(g[i, 1] - g[i, 0]) + Math.Abs(b[i, 1] - b[i, 0])) * 2;
            //    d[i, w - 1] = (Math.Abs(r[i, w - 1] - r[i, w - 2]) + Math.Abs(g[i, w - 1] - g[i, w - 2]) + Math.Abs(b[i, w - 1] - b[i, w - 2])) * 2;
            //    for (int j = 1; j < w - 1; j++)
            //    {
            //        d[i, j] = Math.Abs(r[i, j - 1] - r[i, j]) + Math.Abs(g[i, j - 1] - g[i, j]) + Math.Abs(b[i, j - 1] - b[i, j]) + Math.Abs(r[i, j + 1] - r[i, j]) + Math.Abs(g[i, j + 1] - g[i, j]) + Math.Abs(b[i, j + 1] - b[i, j]);
            //    }
            //}
            //for (int j = 0; j < w; j++)
            //{
            //    d[0,j] += (Math.Abs(r[1,j] - r[0,j]) + Math.Abs(g[1,j] - g[0,j]) + Math.Abs(b[1,j] - b[0,j])) * 2;
            //    d[h-1,j] += (Math.Abs(r[h-2, j] - r[h-1, j]) + Math.Abs(g[h-2, j] - g[h-1,j]) + Math.Abs(b[h-2, j] - b[h-1, j])) * 2;
            //    for (int i = 1; i < h - 1; i++)
            //    {
            //        d[i, j] += Math.Abs(r[i-1, j] - r[i, j]) + Math.Abs(g[i-1, j] - g[i, j]) + Math.Abs(b[i-1, j] - b[i, j]) + Math.Abs(r[i+1, j] - r[i, j]) + Math.Abs(g[i+1, j] - g[i, j]) + Math.Abs(b[i+1, j] - b[i, j]);
            //    }
            //}
            FormatConvertedBitmap greyImg = new FormatConvertedBitmap();
            greyImg.BeginInit();
            greyImg.Source = img;
            greyImg.DestinationFormat = PixelFormats.Gray8;
            greyImg.EndInit();
            int greyStride = (int)greyImg.PixelWidth * (greyImg.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[h * greyStride];
            greyImg.CopyPixels(pixels, greyStride, 0);
            byte[,] grey = new byte[h, w];
            int p = 0;
            for (int i = 0; i < h; ++i)
            {
                for (int j = 0; j < w; ++j)
                {
                    grey[i, j] = pixels[p++];
                }
            }
            d[0, 0] = (Math.Abs(grey[0, 1] - grey[0, 0]) + Math.Abs(grey[1, 0] - grey[0, 0])) * 2;
            d[0, w - 1] = (Math.Abs(grey[0, w - 2] - grey[0, w - 1]) + Math.Abs(grey[1, w - 1] - grey[0, w - 1])) * 2;
            d[h - 1, 0] = (Math.Abs(grey[h - 1, 1] - grey[h - 1, 0]) + Math.Abs(grey[h - 2, 0] - grey[h - 1, 0])) * 2;
            d[h - 1, w - 1] = (Math.Abs(grey[h - 1, w - 2] - grey[h - 1, w - 1]) + Math.Abs(grey[h - 2, w - 1] - grey[h - 1, w - 1])) * 2;

            for (int i = 1; i < w - 1; i++)
            {
                d[0, i] = Math.Abs(grey[0, i - 1] - grey[0, i]) + Math.Abs(grey[0, i + 1] - grey[0, i]) + Math.Abs(grey[1, i] - grey[0, i]) * 2;
                d[h - 1, i] = Math.Abs(grey[h - 1, i - 1] - grey[h - 1, i]) + Math.Abs(grey[h - 1, i + 1] - grey[h - 1, i]) + Math.Abs(grey[h - 2, i] - grey[h - 1, i]) * 2;
            }

            for (int i = 1; i < h - 1; i++)
            {
                d[i, 0] = Math.Abs(grey[i - 1, 0] - grey[i, 0]) + Math.Abs(grey[i + 1, 0] - grey[i, 0]) + Math.Abs(grey[i, 1] - grey[i, 0]) * 2;
                d[i, w - 1] = Math.Abs(grey[i - 1, w - 1] - grey[i, w - 1]) + Math.Abs(grey[i + 1, w - 1] - grey[i, w - 1]) + Math.Abs(grey[i, w - 2] - grey[i, w - 1]) * 2;
            }

            for (int i = 1; i < h - 1; i++)
            {
                for (int j = 1; j < w - 1; j++)
                {
                    d[i, j] = Math.Abs(grey[i - 1, j] - grey[i, j]) + Math.Abs(grey[i + 1, j] - grey[i, j]) + Math.Abs(grey[i, j - 1] - grey[i, j]) + Math.Abs(grey[i, j + 1] - grey[i, j]);
                }
            }

            return d;
        }

        private int[,] generateS(ref int[,] d, int h, int w)
        {
            int[,] s = new int[h, w];
            for(int j = 0; j < w; j++)
            {
                s[0,j] = d[0,j];
            }
            for (int i = 1; i < h; i++)
            {
                s[i, 0] = d[i, 0] + Math.Min(s[i - 1, 0], s[i - 1, 1]);
                for (int j = 1; j < w-1; j++)
                {
                    s[i, j] = d[i, j] + Math.Min(s[i - 1, j - 1], Math.Min(s[i - 1, j], s[i - 1, j + 1]));
                }
                s[i, w - 1] = d[i, w - 1] + Math.Min(s[i - 1, w - 1], s[i - 1, w - 2]);
            }
            return s;
        }

        private int[,] generateHS(ref int[,] d, int h, int w)
        {
            int[,] s = new int[h, w];
            for (int i = 0; i < h; i++)
            {
                s[i, 0] = d[i, 0];
            }
            for (int j = 1; j < w; j++)
            {
                s[0, j] = d[0, j] + Math.Min(s[0, j - 1], s[1, j - 1]);
                for (int i = 1; i < h - 1; i++)
                {
                    s[i, j] = d[i, j] + Math.Min(s[i - 1, j - 1], Math.Min(s[i, j - 1], s[i + 1, j - 1]));
                }
                s[h - 1, j] = d[h - 1, j] + Math.Min(s[h - 1, j - 1], s[h - 2, j - 1]);
            }
            return s;
        }

        private int[] generatePath(ref int[,] s, int h, int w)
        {
            int[] path = new int[h];
            //找到末行s的最小值
            int minS = Int32.MaxValue;
            for (int j = 0; j < w; j++)
            {
                if (s[h - 1, j] < minS)
                {
                    minS = s[h - 1, j];
                    path[h-1] = j;
                }
            }
            for (int i = h - 2, j = path[h - 1]; i >= 0; i--)
            {
                int minss = s[i, j];
                path[i] = j;
                if (j != 0 && j != w-1)
                {
                    if (s[i,j-1] < minss)
                    {
                        minss = s[i, j - 1];
                        path[i] = j - 1;
                    }
                    if (s[i,j+1] < minss)
                    {
                        minss = s[i, j + 1];
                        path[i] = j + 1;
                    }
                } else if (j == 0)
                {
                    if (s[i, j + 1] < minss)
                    {
                        minss = s[i, j + 1];
                        path[i] = j + 1;
                    }
                } else
                {
                    if (s[i, j - 1] < minss)
                    {
                        minss = s[i, j - 1];
                        path[i] = j - 1;
                    }
                }
                j = path[i];
            }
            return path;
        }

        private int[] generateHPath(ref int[,] s, int h, int w)
        {
            int[] path = new int[w];
            //找到末列s的最小值
            int minS = Int32.MaxValue;
            for (int i = 0; i < h; i++)
            {
                if (s[i, w-1] < minS)
                {
                    minS = s[i, w-1];
                    path[w - 1] = i;
                }
            }
            for (int j = w - 2, i = path[w - 1]; j >= 0; j--)
            {
                int minss = s[i, j];
                path[j] = i;
                if (i != 0 && i != h - 1)
                {
                    if (s[i - 1, j] < minss)
                    {
                        minss = s[i - 1, j];
                        path[j] = i - 1;
                    }
                    if (s[i + 1, j] < minss)
                    {
                        minss = s[i + 1, j];
                        path[j] = i + 1;
                    }
                }
                else if (i == 0)
                {
                    if (s[i + 1, j] < minss)
                    {
                        minss = s[i + 1, j];
                        path[j] = i + 1;
                    }
                }
                else
                {
                    if (s[i - 1, j] < minss)
                    {
                        minss = s[i - 1, j];
                        path[j] = i - 1;
                    }
                }
                i = path[j];
            }
            return path;
        }

        public BitmapSource heightResize(ref BitmapSource img)
        {
            //构造输入图像像素数组
            int h = img.PixelHeight;
            int w = img.PixelWidth;

            int stride = w * (img.Format.BitsPerPixel / 8);

            //获取像素数据
            byte[] imgPixels = bitmapSourceToArray(img, stride);
            //将像素数组分别转换为rgb数组
            byte[,] rband = new byte[h, w];
            byte[,] gband = new byte[h, w];
            byte[,] bband = new byte[h, w];
            byte[,] alpha = new byte[h, w];
            int p = 0;
            for (int i = 0; i < h; ++i)
            {
                for (int j = 0; j < w; ++j)
                {
                    // 每个像素的指针是按BGRA的顺序存储
                    alpha[i, j] = imgPixels[p + 3];
                    rband[i, j] = imgPixels[p + 2];
                    gband[i, j] = imgPixels[p + 1];
                    bband[i, j] = imgPixels[p];
                    p += 4;   // 偏移一个像素
                }
            }
            //生成破坏度数组d和s
            int[,] d = generateD(img, h, w);
            int[,] s = generateHS(ref d, h, w);
            //找到接缝
            int[] path = generateHPath(ref s, h, w);
            //根据接缝缩小图像高度
            for(int j = 0; j < w; j++)
            {
                int i = path[j];
                if (i != 0)
                {
                    rband[i - 1, j] = (byte)((rband[i - 1, j] + rband[i, j]) / 2);
                    gband[i - 1, j] = (byte)((gband[i - 1, j] + gband[i, j]) / 2);
                    bband[i - 1, j] = (byte)((bband[i - 1, j] + bband[i, j]) / 2);
                    if (i != h - 1)
                    {
                        rband[i + 1, j] = (byte)((rband[i + 1, j] + rband[i, j]) / 2);
                        gband[i + 1, j] = (byte)((gband[i + 1, j] + gband[i, j]) / 2);
                        bband[i + 1, j] = (byte)((bband[i + 1, j] + bband[i, j]) / 2);
                    }
                }
                while (i < h - 1)
                {
                    rband[i, j] = rband[i+1, j];
                    gband[i, j] = gband[i+1, j];
                    bband[i, j] = bband[i+1, j];
                    alpha[i, j] = alpha[i+1, j];
                    i++;
                }
            }

            //根据rgb数组构造返回byte数组
            int resW = w;
            int resH = h-1;
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
    }
}
