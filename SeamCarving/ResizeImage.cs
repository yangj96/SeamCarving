using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SeamCarving
{
    class ResizeImage
    {
        private int[,] d;
        private int[,] s;
        private int h;
        private int w;
        public BitmapImage halfResize(ref BitmapImage img)
        {
            //构造输入图像像素数组
            h = img.PixelHeight;
            w = img.PixelWidth;
            d = new int[h, w];
            s = new int[h, w];
            //根据返回数组生成输出图像
            BitmapImage result = new BitmapImage();
            

            return result;
        }


    }
}
