using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FNN_MNIST_20160704
{
    /// <summary>
    /// 参考: https://msdn.microsoft.com/ja-jp/magazine/dn745868.aspx
    /// </summary>
    class MNIST
    {
        public byte Label { get; }
        public int Width { get; }
        public int Height { get; }
        public byte[,] Pixels { get; }

        MNIST(byte label, int width, int height, byte[,] pixels)
        {
            Label = label;
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        /// <summary>
        /// http://yann.lecun.com/exdb/mnist/ からダウンロードしたラベルファイル，画像ファイルを読み込んで MNIST 画像とラベルの配列を返します．
        /// </summary>
        /// <param name="labelsPath">ラベルファイルのパス</param>
        /// <param name="imagesPath">画像ファイルのパス</param>
        /// <returns>読み込んだ MNIST 画像とラベルの配列</returns>
        public static MNIST[] LoadData(string labelsPath, string imagesPath)
        {
            using (var labelsFileStream = new FileStream(labelsPath, FileMode.Open))
            using (var imagesFileStream = new FileStream(imagesPath, FileMode.Open))
            {
                using (var labelsBinaryReader = new BinaryReader(labelsFileStream))
                using (var imagesBinaryReader = new BinaryReader(imagesFileStream))
                {
                    var magicNumberLabels = labelsBinaryReader.ReadInt32().ReverseBytes();
                    var numberOfLabels = labelsBinaryReader.ReadInt32().ReverseBytes();

                    var magicNumberImages = imagesBinaryReader.ReadInt32().ReverseBytes();
                    var numberOfImages = imagesBinaryReader.ReadInt32().ReverseBytes();
                    var numberOfRows = imagesBinaryReader.ReadInt32().ReverseBytes();
                    var numberOfColumns = imagesBinaryReader.ReadInt32().ReverseBytes();

                    return Enumerable.Range(0, Math.Min(numberOfLabels, numberOfImages)).Select(_ =>
                    {
                        var pixelsArray = new byte[numberOfRows, numberOfColumns];
                        for (var i = 0; i < numberOfRows; i++)
                        {
                            for (var j = 0; j < numberOfColumns; j++)
                            {
                                pixelsArray[i, j] = imagesBinaryReader.ReadByte();
                            }
                        }
                        return new MNIST(labelsBinaryReader.ReadByte(), numberOfRows, numberOfColumns, pixelsArray);
                    }).ToArray();
                }
            }
        }

        public BitmapSource ToBitmapSource()
        {
            return ToBitmapSource(96, 96);
        }

        public BitmapSource ToBitmapSource(double dpiX, double dpiY)
        {
            var stride = (Width * PixelFormats.Gray8.BitsPerPixel + 7) / 8;
            var pixels1DArray = new byte[Pixels.Length];
            Buffer.BlockCopy(Pixels, 0, pixels1DArray, 0, Pixels.Length);
            return BitmapSource.Create(Width, Height, dpiX, dpiY, PixelFormats.Gray8, null, pixels1DArray, stride);
        }
    }

    static class MNISTExtensions
    {
        public static Matrix<double> ToInputMatrix(this MNIST[] mnistImages)
        {
            return mnistImages.Select(e => e.Pixels.ToDouble2DArray().ToMatrix().ToRowWiseArray()).ToArray().ToMatrixAsColumnArrays();
        }

        public static Matrix<double> ToOutputMatrix(this MNIST[] mnistImages)
        {
            return mnistImages.Select(e => e.Label.ToClassesArray()).ToArray().ToMatrixAsColumnArrays();
        }

        public static double[] ToClassesArray(this byte label)
        {
            return Enumerable.Range(0, 10).Select(n => n == label ? 1.0 : 0.0).ToArray();
        }

        public static int ReverseBytes(this int x) => BitConverter.ToInt32(BitConverter.GetBytes(x).Reverse().ToArray(), 0);

        public static double[,] ToDouble2DArray(this byte[,] byte2DArray)
        {
            var double2DArray = new double[byte2DArray.GetLength(0), byte2DArray.GetLength(1)];
            Array.Copy(byte2DArray, double2DArray, byte2DArray.Length);
            return double2DArray;
        }
    }
}
