using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FNN_MNIST_20160704
{
    class FeedforwardNeuralNetwork
    {
        static double rectifiedLinear(double u) => Math.Max(u, 0);
        static double derivativeOfRectifiedLinear(double u) => (0 <= u) ? 1 : 0;
        //static Vector<double> softmax(Vector<double> u) => Vector<double>.Build.DenseOfEnumerable(u.Select(x => Math.Exp(x) / u.Sum(y => Math.Exp(y))));
        static Vector<double> softmax(Vector<double> u) => Vector<double>.Build.DenseOfEnumerable(u.Select(x => Math.Exp(x - u.Max()) / u.Sum(y => Math.Exp(y - u.Max())))); // exp オーバーフロー対策

        public readonly static int NumberOfUnits1 = 28 * 28;
        public readonly static int NumberOfUnits2 = 100;
        public readonly static int NumberOfUnits3 = 10;
        public readonly static double LearningRate = 0.005;
        public readonly static double InitializeStddev = 0.01;
        public readonly static double InitializeBias = 0.01;
        public readonly static int SizeOfMinibatch = 100;

        public Matrix<double> Weight2 { get; private set; }
        public Matrix<double> Bias2 { get; private set; }

        public Matrix<double> Weight3 { get; private set; }
        public Matrix<double> Bias3 { get; private set; }

        public Matrix<double> RoundDWeight2 { get; private set; }
        public Matrix<double> RoundDBias2 { get; private set; }

        public Matrix<double> RoundDWeight3 { get; private set; }
        public Matrix<double> RoundDBias3 { get; private set; }

        public FeedforwardNeuralNetwork()
        {
            var distribution = new Normal(0, InitializeStddev);

            Weight2 = Matrix<double>.Build.Random(NumberOfUnits2, NumberOfUnits1, distribution);
            Bias2 = Vector<double>.Build.Dense(NumberOfUnits2, InitializeBias).ToColumnMatrix();

            Weight3 = Matrix<double>.Build.Random(NumberOfUnits3, NumberOfUnits2, distribution);
            Bias3 = Vector<double>.Build.Dense(NumberOfUnits3, InitializeBias).ToColumnMatrix();
        }

        public double[] Output(double[] x)
        {
            if (x.Length != NumberOfUnits1)
            {
                throw new ArgumentException();
            }

            var z1 = Vector<double>.Build.DenseOfArray(x).ToColumnMatrix();

            var u2 = Weight2 * z1 + Bias2 * Vector<double>.Build.Dense(z1.ColumnCount, 1).ToRowMatrix();
            var z2 = u2.MapElements(rectifiedLinear);

            var u3 = Weight3 * z2 + Bias3 * Vector<double>.Build.Dense(z2.ColumnCount, 1).ToRowMatrix();
            var z3 = u3.MapColumnVectors(softmax);

            if (z3.ColumnCount != 1)
            {
                throw new InvalidOperationException();
            }

            return z3.ToColumnArrays()[0];
        }

        public double NormOfGradient()
        {
            var gradient = Vector<double>.Build.DenseOfEnumerable(RoundDWeight2.ToRowWiseArray()
                .Concat(RoundDWeight3.ToRowWiseArray()
                .Concat(RoundDBias2.ToRowWiseArray()
                .Concat(RoundDBias3.ToRowWiseArray()))));
            return gradient.Norm(2);
        }

        public void Learn(Matrix<double> x, Matrix<double> d)
        {
            if (x.ColumnCount != d.ColumnCount || x.RowCount != NumberOfUnits1 || d.RowCount != NumberOfUnits3)
            {
                throw new ArgumentException();
            }

            var mean = Vector<double>.Build.DenseOfEnumerable(x.ToRowArrays().Select(e => e.Average())).ToColumnMatrix();

            var random = new Random();

            for (var t = 0; t < 100; t++)
            {
                var minibatchIndexes = Enumerable.Range(0, SizeOfMinibatch).Select(_ => random.Next(0, 60000));
                var minibatchX = Matrix<double>.Build.DenseOfColumnArrays(minibatchIndexes.Select(i => x.ToColumnArrays()[i]).ToArray()) - mean * Vector<double>.Build.Dense(SizeOfMinibatch, 1).ToRowMatrix();
                var minibatchD = Matrix<double>.Build.DenseOfColumnArrays(minibatchIndexes.Select(i => d.ToColumnArrays()[i]).ToArray());
                LearnMinibatch(minibatchX, minibatchD);
                var norm = NormOfGradient();
                Console.WriteLine($"norm = {norm}");
            }
        }

        public void LearnMinibatch(Matrix<double> x, Matrix<double> d)
        {
            if (x.ColumnCount != d.ColumnCount || x.RowCount != NumberOfUnits1 || d.RowCount != NumberOfUnits3)
            {
                throw new ArgumentException();
            }

            var z1 = x;

            var u2 = Weight2 * z1 + Bias2 * Vector<double>.Build.Dense(z1.ColumnCount, 1).ToRowMatrix();
            var z2 = u2.MapElements(rectifiedLinear);

            var u3 = Weight3 * z2 + Bias3 * Vector<double>.Build.Dense(z2.ColumnCount, 1).ToRowMatrix();
            var z3 = u3.MapColumnVectors(softmax);

            var delta3 = d - z3;
            var delta2 = u2.MapElements(derivativeOfRectifiedLinear).PointwiseMultiply(Weight3.Transpose() * delta3);

            Parallel.Invoke(new Action[]
            {
                () => RoundDWeight2 = (delta2 * z1.Transpose()).Multiply(1.0 / x.ColumnCount),
                () => RoundDBias2 = (delta2 * Vector<double>.Build.Dense(x.ColumnCount, 1).ToColumnMatrix()).Multiply(1.0 / x.ColumnCount),
                () => RoundDWeight3 = (delta3 * z2.Transpose()).Multiply(1.0 / x.ColumnCount),
                () => RoundDBias3 = (delta3 * Vector<double>.Build.Dense(x.ColumnCount, 1).ToColumnMatrix()).Multiply(1.0 / x.ColumnCount),
            });

            Parallel.Invoke(new Action[]
            {
                () => Weight2 = Weight2 - RoundDWeight2.Multiply(LearningRate),
                () => Bias2 = Bias2 - RoundDBias2.Multiply(LearningRate),
                () => Weight3 = Weight3 - RoundDWeight3.Multiply(LearningRate),
                () => Bias3 = Bias3 - RoundDBias3.Multiply(LearningRate),
            });
        }
    }

    static class FeedforwardNeuralNetworkExtensions
    {
        public static Matrix<T> MapElements<T>(this Matrix<T> x, Func<T, T> f) where T : struct, IEquatable<T>, IFormattable
        {
            return Matrix<T>.Build.DenseOfColumnVectors(x.ToColumnArrays().Select(e => Vector<T>.Build.DenseOfEnumerable(e.Select(f))).ToArray());
        }

        public static Matrix<T> MapColumnVectors<T>(this Matrix<T> x, Func<Vector<T>, Vector<T>> f) where T : struct, IEquatable<T>, IFormattable
        {
            return Matrix<T>.Build.DenseOfColumnVectors(x.ToColumnArrays().Select(e => f(Vector<T>.Build.DenseOfEnumerable(e))).ToArray());
        }

        public static Matrix<double> Standardize(this Matrix<double> x)
        {
            return Matrix<double>.Build.DenseOfRowArrays(x.ToRowArrays().Select(rowArray => rowArray.Select(e => (e - Statistics.Mean(rowArray)) / Statistics.PopulationStandardDeviation(rowArray)).ToArray()).ToArray());
        }
    }
}
