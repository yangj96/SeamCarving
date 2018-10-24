using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SeamCarving
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapImage inputImage;
        private BitmapSource outputImage;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Choose File";
            //初始目录
            //openFileDialog.InitialDirectory = @"C:\";
            //设定文件类型
            openFileDialog.Filter = "Image File(*.bmp,*.png,*.jpg,*.jpeg)|*.bmp;*.png;*.jpg;*.jpeg";
            openFileDialog.ShowDialog();

            //获得在打开文件对话框中选择的文件的路径
            string path = openFileDialog.FileName;
            if (path == "")
            {
                return;
            } 

            using (BinaryReader loader = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                FileInfo fd = new FileInfo(path);
                int Length = (int)fd.Length;
                byte[] buf = new byte[Length];
                buf = loader.ReadBytes((int)fd.Length);
                loader.Dispose();
                loader.Close();

                inputImage = new BitmapImage();
                inputImage.BeginInit();
                inputImage.StreamSource = new MemoryStream(buf);
                inputImage.EndInit();
                image.Source = inputImage;
                GC.Collect();
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            ResizeImage resize = new ResizeImage();
            outputImage = resize.halfResize(ref inputImage);
            image1.Source = outputImage;
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Image File(*.bmp,*.png,*.jpg,*.jpeg)|*.bmp;*.png;*.jpg;*.jpeg";
            saveFileDialog.RestoreDirectory = true; 
            if (saveFileDialog.ShowDialog() == true)
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)this.image1.Source));
                using (FileStream stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    encoder.Save(stream);
            }
        }
    }
}
