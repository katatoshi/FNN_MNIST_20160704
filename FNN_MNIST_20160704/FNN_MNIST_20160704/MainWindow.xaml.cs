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
        //MNIST[] trainData;
        //MNIST[] testData;
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
                //if (trainData == null)
                //{
                //    trainData = MNIST.LoadData(@"D:\train-labels.idx1-ubyte", @"D:\train-images.idx3-ubyte");
                //}

                //if (testData == null)
                //{
                //    testData = MNIST.LoadData(@"D:\t10k-labels.idx1-ubyte", @"D:\t10k-images.idx3-ubyte");
                //}

                var fnn = FeedforwardNeuralNetwork.Initialize();
                fnn.Learn(@"D:\train-labels.idx1-ubyte", @"D:\train-images.idx3-ubyte");
                ParameterArrays.FromMatrices(fnn.Weight2, fnn.Bias2, fnn.Weight3, fnn.Bias3).Serialize();
                var errorRate = fnn.TestWithLog(@"D:\t10k-labels.idx1-ubyte", @"D:\t10k-images.idx3-ubyte");
                Console.WriteLine($"errorRate = {errorRate}");

                //var parameterArrays = ParameterArrays.Deserialize("parameter_arrays_20160706022111.xml");
                //var fnn2 = FeedforwardNeuralNetwork.FromParameterArrays(parameterArrays);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    //var index = random.Next(0, trainData.Length);
                    //var mnistImage = trainData[index];
                    //mnistLabelTextBlock.Text = $"Label: {mnistImage.Label} (Index: {index})";
                    //mnistPixelsImage.Source = mnistImage.ToBitmapSource();
                    runButton.IsEnabled = true;
                });
            });
        }
    }
}
