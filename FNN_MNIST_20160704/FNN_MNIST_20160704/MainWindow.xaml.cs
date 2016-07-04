using System;
using System.Collections.Generic;
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

namespace FNN_MNIST_20160704
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MNISTImage[] trainData;
        MNISTImage[] testData;
        Random random = new Random();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void clickRun(object sender, RoutedEventArgs e)
        {
            runButton.IsEnabled = false;
            Task.Run(() =>
            {
                if (trainData == null)
                {
                    trainData = MNISTImage.LoadData(@"D:\train-labels.idx1-ubyte", @"D:\train-images.idx3-ubyte");
                }

                if (testData == null)
                {
                    testData = MNISTImage.LoadData(@"D:\t10k-labels.idx1-ubyte", @"D:\t10k-images.idx3-ubyte");
                }

                //var feedforwardNeuralNetwork = new FeedforwardNeuralNetwork();
                //feedforwardNeuralNetwork.Learn(trainData.ToInput(), trainData.ToOutput());

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var index = random.Next(0, trainData.Length);
                    var mnistImage = trainData[index];
                    mnistLabelTextBlock.Text = $"Label: {mnistImage.Label} (Index: {index})";
                    mnistPixelsImage.Source = mnistImage.ToBitmapSource();
                    runButton.IsEnabled = true;
                });
            });
        }
    }
}
