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

namespace FNN_MNIST_20160704
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MNIST[] testData;
        int index;
        FeedforwardNeuralNetwork fnn;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void clickLoad(object sender, RoutedEventArgs e)
        {
            previousButton.IsEnabled = false;
            nextButton.IsEnabled = false;
            testButton.IsEnabled = false;
            loadButton.IsEnabled = false;

            Task.Run(() =>
            {
                var learnedFileName = @"D:\learned.xml";
                if (File.Exists(learnedFileName))
                {
                    var parameterArrays = ParameterArrays.Deserialize(learnedFileName);
                    var trainData = MNIST.LoadData(@"D:\train-labels.idx1-ubyte", @"D:\train-images.idx3-ubyte");
                    var meanArray = trainData.ToInputMatrix().ToRowArrays().Select(row => row.Average()).ToArray();
                    fnn = FeedforwardNeuralNetwork.FromParameterArrays(parameterArrays, meanArray);
                }
                else
                {
                    fnn = FeedforwardNeuralNetwork.Initialize();
                    fnn.Learn(@"D:\train-labels.idx1-ubyte", @"D:\train-images.idx3-ubyte");
                    ParameterArrays.FromMatrices(fnn.Weight2, fnn.Bias2, fnn.Weight3, fnn.Bias3).Serialize();
                }

                testData = MNIST.LoadData(@"D:\t10k-labels.idx1-ubyte", @"D:\t10k-images.idx3-ubyte");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    index = 0;
                    var mnist = testData[index];
                    mnistLabelTextBlock.Text = $"Label: {mnist.Label} (Index: {index})";
                    mnistPixelsImage.Source = mnist.ToBitmapSource();
                    var estimate = fnn.Run(new[] { mnist })[0];
                    estimatedTextBlock.Text = $"Estimate: {estimate.Select(x => x.ToString()).Aggregate((acc, x) => acc + " or " + x)}";

                    previousButton.IsEnabled = true;
                    nextButton.IsEnabled = true;
                    testButton.IsEnabled = true;
                    loadButton.IsEnabled = true;
                });
            });
        }

        private void clickTest(object sender, RoutedEventArgs e)
        {
            if (fnn == null)
            {
                return;
            }

            previousButton.IsEnabled = false;
            nextButton.IsEnabled = false;
            testButton.IsEnabled = false;
            loadButton.IsEnabled = false;

            Task.Run(() =>
            {
                var errorRate = fnn.TestWithLog(testData);
                Console.WriteLine($"Test Finished: error rate = {errorRate}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    previousButton.IsEnabled = true;
                    nextButton.IsEnabled = true;
                    testButton.IsEnabled = true;
                    loadButton.IsEnabled = true;
                });
            });
        }

        private void clickPrevious(object sender, RoutedEventArgs e)
        {
            if (testData == null || index <= 0)
            {
                return;
            }

            index--;
            var mnist = testData[index];
            mnistLabelTextBlock.Text = $"Label: {mnist.Label} (Index: {index})";
            mnistPixelsImage.Source = mnist.ToBitmapSource();
            var estimate = fnn.Run(new[] { mnist })[0];
            estimatedTextBlock.Text = $"Estimate: {estimate.Select(x => x.ToString()).Aggregate((acc, x) => acc + " or " + x)}";
        }

        private void clickNext(object sender, RoutedEventArgs e)
        {
            if (testData == null || testData.Length < index)
            {
                return;
            }

            index++;
            var mnist = testData[index];
            mnistLabelTextBlock.Text = $"Label: {mnist.Label} (Index: {index})";
            mnistPixelsImage.Source = mnist.ToBitmapSource();
            var estimate = fnn.Run(new[] { mnist })[0];
            estimatedTextBlock.Text = $"Estimate: {estimate.Select(x => x.ToString()).Aggregate((acc, x) => acc + " or " + x)}";
        }
    }
}
