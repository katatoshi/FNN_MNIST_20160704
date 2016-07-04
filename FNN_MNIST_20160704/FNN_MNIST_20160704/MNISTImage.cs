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
    class MNISTImage
    {
        public byte Label { get; }
        public int Width { get; }
        public int Height { get; }
        public byte[,] Pixels { get; }

        MNISTImage(byte label, int width, int height, byte[,] pixels)
        {
            Label = label;
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        public static MNISTImage[] LoadData(string LabelsPath, string ImagesPath)
        {
            using (var LabelsFileStream = new FileStream(LabelsPath, FileMode.Open))
            using (var ImagesFileStream = new FileStream(ImagesPath, FileMode.Open))
            {
                using (var LabelsBinaryReader = new BinaryReader(LabelsFileStream))
                using (var ImagesBinaryReader = new BinaryReader(ImagesFileStream))
                {
                    var magicNumberLabels = LabelsBinaryReader.ReadInt32().ReverseBytes();
                    var numberOfLabels = LabelsBinaryReader.ReadInt32().ReverseBytes();

                    var magicNumberImages = ImagesBinaryReader.ReadInt32().ReverseBytes();
                    var numberOfImages = ImagesBinaryReader.ReadInt32().ReverseBytes();
                    var numberOfRows = ImagesBinaryReader.ReadInt32().ReverseBytes();
                    var numberOfColumns = ImagesBinaryReader.ReadInt32().ReverseBytes();

                    return Enumerable.Range(0, Math.Min(numberOfLabels, numberOfImages)).Select(_ =>
                    {
                        var pixelsArray = new byte[numberOfRows, numberOfColumns];
                        for (var i = 0; i < numberOfRows; i++)
                        {
                            for (var j = 0; j < numberOfColumns; j++)
                            {
                                pixelsArray[i, j] = ImagesBinaryReader.ReadByte();
                            }
                        }
                        return new MNISTImage(LabelsBinaryReader.ReadByte(), numberOfRows, numberOfColumns, pixelsArray);
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

    static class MNISTImageExtensions
    {
        public static int ReverseBytes(this int x) => BitConverter.ToInt32(BitConverter.GetBytes(x).Reverse().ToArray(), 0);

        public static double[,] ToDouble2DArray(this byte[,] byte2DArray)
        {
            var double2DArray = new double[byte2DArray.GetLength(0), byte2DArray.GetLength(1)];
            Array.Copy(byte2DArray, double2DArray, byte2DArray.Length);
            return double2DArray;
        }

        public static double[] ToLabelArray(this byte label)
        {
            return Enumerable.Range(0, 10).Select(n => n == label ? 1.0 : 0.0).ToArray();
        }

        public static Matrix<double> ToInput(this MNISTImage[] mnistImages)
        {
            return Matrix<double>.Build.DenseOfColumnArrays(mnistImages.Select(e => Matrix<double>.Build.DenseOfArray(e.Pixels.ToDouble2DArray()).ToRowWiseArray()).ToArray());
        }

        public static Matrix<double> ToOutput(this MNISTImage[] mnistImages)
        {
            return Matrix<double>.Build.DenseOfColumnArrays(mnistImages.Select(e => e.Label.ToLabelArray()).ToArray());
        }
    }
}
