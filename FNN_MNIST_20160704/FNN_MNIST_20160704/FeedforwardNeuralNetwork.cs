using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FNN_MNIST_20160704
{
    class FeedforwardNeuralNetwork
    {
        static double rectifiedLinear(double u) => Math.Max(u, 0);

        static double rectifiedLinearDerivative(double u) => (0 <= u) ? 1 : 0;

        static Vector<double> softmax(Vector<double> u) => u.Select(x => Math.Exp(x - u.Max()) / u.Sum(y => Math.Exp(y - u.Max()))).ToVector(); // Exp 内で u.Max() を引いているのは Exp のオーバーフロー対策

        static double crossEntropy(Matrix<double> d, Matrix<double> y)
        {
            if (d.RowCount != y.RowCount || d.ColumnCount != y.ColumnCount)
            {
                throw new ArgumentException();
            }

            return -((MatrixWrappers.Ones(d.RowCount).Transpose() * d.PointwiseMultiply(y.MapElements(e => Math.Log(e))) * MatrixWrappers.Ones(y.ColumnCount))[0, 0]);
        }

        public readonly static int NumberOfUnits1 = 28 * 28;
        public readonly static int NumberOfUnits2 = 100;
        public readonly static int NumberOfUnits3 = 10;

        public readonly static int SizeOfMinibatch = 100;

        public readonly static double LearningRate = 0.005;
        public readonly static double InitializeStdDev = 0.01;
        public readonly static double InitialBias = 0.01;

        public readonly static int MaxIteration = 600;
        public readonly static double EpsilonGradient = 0.001;

        public double[] MeanArray { get; private set; }

        public Matrix<double> Weight2 { get; private set; }
        public Matrix<double> Bias2 { get; private set; }

        public Matrix<double> Weight3 { get; private set; }
        public Matrix<double> Bias3 { get; private set; }

        public Matrix<double> RoundDWeight2 { get; private set; }
        public Matrix<double> RoundDBias2 { get; private set; }

        public Matrix<double> RoundDWeight3 { get; private set; }
        public Matrix<double> RoundDBias3 { get; private set; }

        public Vector<double> Gradient => RoundDWeight2.ToRowWiseArray()
            .Concat(RoundDBias2.ToRowWiseArray())
            .Concat(RoundDWeight3.ToRowWiseArray())
            .Concat(RoundDBias3.ToRowWiseArray())
            .ToVector();

        public static FeedforwardNeuralNetwork Initialize()
        {
            var distribution = new Normal(0, InitializeStdDev);

            var weight2 = Matrix<double>.Build.Random(NumberOfUnits2, NumberOfUnits1, distribution);
            var bias2 = MatrixWrappers.Scalars(NumberOfUnits2, InitialBias);

            var weight3 = Matrix<double>.Build.Random(NumberOfUnits3, NumberOfUnits2, distribution);
            var bias3 = MatrixWrappers.Scalars(NumberOfUnits3, InitialBias);

            return new FeedforwardNeuralNetwork(weight2, bias2, weight3, bias3);
        }

        public static FeedforwardNeuralNetwork FromParameterArrays(ParameterArrays parameterArrays, double[] meanArray)
        {
            var weight2 = parameterArrays.Weight2;
            var bias2 = parameterArrays.Bias2;

            var weight3 = parameterArrays.Weight3;
            var bias3 = parameterArrays.Bias3;

            var fnn = new FeedforwardNeuralNetwork(weight2, bias2, weight3, bias3);
            fnn.MeanArray = meanArray;

            return fnn;
        }

        FeedforwardNeuralNetwork(Matrix<double> weight2, Matrix<double> bias2, Matrix<double> weight3, Matrix<double> bias3)
        {
            Weight2 = weight2;
            Bias2 = bias2;

            Weight3 = weight3;
            Bias3 = bias3;
        }

        public double Learn(string trainLabelsPath, string trainImagesPath)
        {
            var trainData = MNIST.LoadData(trainLabelsPath, trainImagesPath);
            if (trainData.Length % SizeOfMinibatch != 0)
            {
                Console.WriteLine("警告：ミニバッチのサイズが学習サンプルの数の約数になっていないため，配列の末尾の一部のサンプルが学習に利用されません．");
            }

            MeanArray = trainData.ToInputMatrix().ToRowArrays().Select(row => row.Average()).ToArray();

            var lastError = double.PositiveInfinity;
            for (var k = 1; k <= MaxIteration; k++)
            {
                var xMinibatch = trainData.Skip((k * SizeOfMinibatch) % trainData.Length).Take(SizeOfMinibatch).ToArray().ToInputMatrix() - MeanArray.ToVector().ToColumnMatrix() * MatrixWrappers.Ones(SizeOfMinibatch).Transpose();
                var dMinibatch = trainData.Skip((k * SizeOfMinibatch) % trainData.Length).Take(SizeOfMinibatch).ToArray().ToOutputMatrix();
                lastError = LearnMinibatch(xMinibatch, dMinibatch);
                var norm = Gradient.Norm(2);
                Console.WriteLine($"Learning (iteration {k}): error = {lastError}, norm of gradient = {norm}");
                if (norm < EpsilonGradient)
                {
                    return lastError;
                }
            }
            return lastError;
        }

        public double LearnMinibatch(Matrix<double> x, Matrix<double> d)
        {
            if (x.ColumnCount != d.ColumnCount || x.RowCount != NumberOfUnits1 || d.RowCount != NumberOfUnits3)
            {
                throw new ArgumentException();
            }

            var n = x.ColumnCount;

            var z1 = x;

            var u2 = Weight2 * z1 + Bias2 * MatrixWrappers.Ones(n).Transpose();
            var z2 = u2.MapElements(rectifiedLinear);

            var u3 = Weight3 * z2 + Bias3 * MatrixWrappers.Ones(n).Transpose();
            var z3 = u3.MapColumnVectors(softmax);

            var delta3 = z3 - d;
            var delta2 = u2.MapElements(rectifiedLinearDerivative).PointwiseMultiply(Weight3.Transpose() * delta3);

            Parallel.Invoke(new Action[]
            {
                () => RoundDWeight2 = (delta2 * z1.Transpose()).Multiply(1.0 / n),
                () => RoundDBias2 = (delta2 * MatrixWrappers.Ones(n)).Multiply(1.0 / n),
                () => RoundDWeight3 = (delta3 * z2.Transpose()).Multiply(1.0 / n),
                () => RoundDBias3 = (delta3 * MatrixWrappers.Ones(n)).Multiply(1.0 / n),
            });

            Parallel.Invoke(new Action[]
            {
                () => Weight2 = Weight2 - RoundDWeight2.Multiply(LearningRate),
                () => Bias2 = Bias2 - RoundDBias2.Multiply(LearningRate),
                () => Weight3 = Weight3 - RoundDWeight3.Multiply(LearningRate),
                () => Bias3 = Bias3 - RoundDBias3.Multiply(LearningRate),
            });

            return crossEntropy(d, z3);
        }


        public double TestWithLog(string testLabelsPath, string testImagesPath)
        {
            var testData = MNIST.LoadData(testLabelsPath, testImagesPath);
            return TestWithLog(testData);
        }

        public double TestWithLog(MNIST[] testData)
        {
            var expected = testData.Select(e => e.Label).ToArray();
            var result = Run(testData);
            var error = 0;
            for (var i = 0; i < expected.Length; i++)
            {
                var success = result[i].Contains(expected[i]);
                if (!success)
                {
                    error++;
                }
                Console.WriteLine($"Testing (sample {i}): success = {success}, expected = {expected[i]}, most likely labels = [{result[i].Select(e => e.ToString()).Aggregate((acc, x) => acc + ", " + x)}], error rate = {error} / {expected.Length}");
            }
            return error / (double)expected.Length;
        }

        public List<byte>[] Run(MNIST[] mnists)
        {
            var n = mnists.Length;

            var z1 = mnists.ToInputMatrix() - MeanArray.ToVector().ToColumnMatrix() * MatrixWrappers.Ones(n).Transpose();

            var u2 = Weight2 * z1 + Bias2 * MatrixWrappers.Ones(n).Transpose();
            var z2 = u2.MapElements(rectifiedLinear);

            var u3 = Weight3 * z2 + Bias3 * MatrixWrappers.Ones(n).Transpose();
            var z3 = u3.MapColumnVectors(softmax);

            return z3.ToMostLikelyLabelsArray();
        }
    }

    /// <summary>
    /// パラメータをシリアライズ，デシリアライズするためのクラス
    /// </summary>
    public class ParameterArrays
    {
        public static ParameterArrays FromMatrices(Matrix<double> weight2, Matrix<double> bias2, Matrix<double> weight3, Matrix<double> bias3)
        {
            return new ParameterArrays
            {
                WeightColumnArrays2 = weight2.ToColumnArrays(),
                BiasArray2 = bias2.ToColumnArrays()[0],
                WeightColumnArrays3 = weight3.ToColumnArrays(),
                BiasArray3 = bias3.ToColumnArrays()[0]
            };
        }

        /// <summary>
        /// 参考：http://dobon.net/vb/dotnet/file/xmlserializer.html
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static ParameterArrays Deserialize(string fileName)
        {
            // TODO ファイルが存在しない場合の処理
            using (var streamReader = new StreamReader(fileName, new UTF8Encoding(false)))
            {
                return (ParameterArrays)new XmlSerializer(typeof(ParameterArrays)).Deserialize(streamReader);
            }
        }

        public double[][] WeightColumnArrays2;
        public double[] BiasArray2;
        public double[][] WeightColumnArrays3;
        public double[] BiasArray3;

        public Matrix<double> Weight2 => WeightColumnArrays2.ToMatrixAsColumnArrays();

        public Matrix<double> Bias2 => BiasArray2.ToVector().ToColumnMatrix();

        public Matrix<double> Weight3 => WeightColumnArrays3.ToMatrixAsColumnArrays();

        public Matrix<double> Bias3 => BiasArray3.ToVector().ToColumnMatrix();

        /// <summary>
        /// 参考：http://dobon.net/vb/dotnet/file/xmlserializer.html
        /// </summary>
        public void Serialize()
        {
            // TODO ファイルが既に存在する場合の処理
            var fileName = $"parameter_arrays_{DateTime.Now.ToString("yyyyMMddhhmmss")}.xml";
            using (var streamWriter = new StreamWriter(fileName, false, new UTF8Encoding(false)))
            {
                new XmlSerializer(typeof(ParameterArrays)).Serialize(streamWriter, this);
            }
        }
    }

    class VectorWrappers
    {
        public static Vector<double> Ones(int length) => Vector<double>.Build.Dense(length, 1.0);

        public static Vector<double> Scalars(int length, double value) => Vector<double>.Build.Dense(length, value);
    }

    class MatrixWrappers
    {
        public static Matrix<double> Ones(int length) => VectorWrappers.Ones(length).ToColumnMatrix();

        public static Matrix<double> Scalars(int length, double value) => VectorWrappers.Scalars(length, value).ToColumnMatrix();

        public static Matrix<double> Identity(int order) => Matrix<double>.Build.DenseIdentity(order);
    }

    static class LinearAlgebraExtensions
    {
        public static Vector<T> ToVector<T>(this IEnumerable<T> e) where T : struct, IEquatable<T>, IFormattable
        {
            return Vector<T>.Build.DenseOfEnumerable(e);
        }

        public static Vector<T> ToVector<T>(this T[] e) where T : struct, IEquatable<T>, IFormattable
        {
            return Vector<T>.Build.DenseOfArray(e);
        }

        public static Matrix<T> ToMatrix<T>(this T[,] e) where T : struct, IEquatable<T>, IFormattable
        {
            return Matrix<T>.Build.DenseOfArray(e);
        }

        public static Matrix<T> ToMatrixAsColumnArrays<T>(this T[][] columns) where T : struct, IEquatable<T>, IFormattable
        {
            return Matrix<T>.Build.DenseOfColumnArrays(columns);
        }

        public static Matrix<T> ToMatrixAsRowArrays<T>(this T[][] rows) where T : struct, IEquatable<T>, IFormattable
        {
            return Matrix<T>.Build.DenseOfRowArrays(rows);
        }

        public static Matrix<T> ToMatrixAsColumnVectors<T>(this Vector<T>[] columns) where T : struct, IEquatable<T>, IFormattable
        {
            return Matrix<T>.Build.DenseOfColumnVectors(columns);
        }

        public static Matrix<T> ToMatrixAsRowVectors<T>(this Vector<T>[] rows) where T : struct, IEquatable<T>, IFormattable
        {
            return Matrix<T>.Build.DenseOfRowVectors(rows);
        }

        public static Matrix<T> MapElements<T>(this Matrix<T> matrix, Func<T, T> f) where T : struct, IEquatable<T>, IFormattable
        {
            return matrix.ToColumnArrays().Select(column => column.Select(f).ToVector()).ToArray().ToMatrixAsColumnVectors();
        }

        public static Matrix<T> MapColumnVectors<T>(this Matrix<T> matrix, Func<Vector<T>, Vector<T>> f) where T : struct, IEquatable<T>, IFormattable
        {
            return matrix.ToColumnArrays().Select(column => f(column.ToVector())).ToArray().ToMatrixAsColumnVectors();
        }
    }
}
