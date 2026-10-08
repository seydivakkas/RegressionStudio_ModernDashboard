using ML.Core.Models;
using ML.Core.Preprocessing;

namespace ML.Core.Regression;

public sealed class MlpRegressor : IRegressor
{
    private readonly Random _random;
    private readonly StandardScaler1D _xScaler = new();
    private readonly StandardScaler1D _yScaler = new();
    private double[] _weights1 = Array.Empty<double>();
    private double[] _biases1 = Array.Empty<double>();
    private double[] _weights2 = Array.Empty<double>();
    private double _bias2;

    public int HiddenNeurons { get; }
    public double LearningRate { get; }
    public int MaxEpochs { get; }
    public double Momentum { get; }
    public double Tolerance { get; }

    public MlpRegressor(
        int hiddenNeurons = 12,
        double learningRate = 0.01,
        int maxEpochs = 10_000,
        double momentum = 0.9,
        double tolerance = 1e-7,
        int seed = 42)
    {
        if (hiddenNeurons <= 0) throw new ArgumentOutOfRangeException(nameof(hiddenNeurons));
        if (learningRate <= 0) throw new ArgumentOutOfRangeException(nameof(learningRate));
        if (maxEpochs <= 0) throw new ArgumentOutOfRangeException(nameof(maxEpochs));
        if (momentum is < 0 or >= 1) throw new ArgumentOutOfRangeException(nameof(momentum));
        if (tolerance <= 0) throw new ArgumentOutOfRangeException(nameof(tolerance));

        HiddenNeurons = hiddenNeurons;
        LearningRate = learningRate;
        MaxEpochs = maxEpochs;
        Momentum = momentum;
        Tolerance = tolerance;
        _random = new Random(seed);
    }

    public RegressionTrainingResult Fit(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        RegressionValidation.Validate(x, y);
        _xScaler.Fit(x);
        _yScaler.Fit(y);
        var nx = _xScaler.Transform(x);
        var ny = _yScaler.Transform(y);

        InitializeWeights();

        var velocityW1 = new double[HiddenNeurons];
        var velocityB1 = new double[HiddenNeurons];
        var velocityW2 = new double[HiddenNeurons];
        double velocityB2 = 0.0;

        var gradW1 = new double[HiddenNeurons];
        var gradB1 = new double[HiddenNeurons];
        var gradW2 = new double[HiddenNeurons];
        var hiddenZ = new double[HiddenNeurons];
        var hiddenA = new double[HiddenNeurons];
        var history = new List<double>(MaxEpochs);
        int epochsRun = 0;

        for (int epoch = 0; epoch < MaxEpochs; epoch++)
        {
            Array.Clear(gradW1, 0, gradW1.Length);
            Array.Clear(gradB1, 0, gradB1.Length);
            Array.Clear(gradW2, 0, gradW2.Length);
            double gradB2 = 0.0;
            double totalMse = 0.0;

            for (int sample = 0; sample < nx.Length; sample++)
            {
                double input = nx[sample];
                for (int h = 0; h < HiddenNeurons; h++)
                {
                    hiddenZ[h] = _weights1[h] * input + _biases1[h];
                    hiddenA[h] = Relu(hiddenZ[h]);
                }

                double prediction = _bias2;
                for (int h = 0; h < HiddenNeurons; h++)
                    prediction += _weights2[h] * hiddenA[h];

                double error = ny[sample] - prediction;
                totalMse += error * error;
                double dLossDOutput = -2.0 * error;

                for (int h = 0; h < HiddenNeurons; h++)
                    gradW2[h] += dLossDOutput * hiddenA[h];
                gradB2 += dLossDOutput;

                for (int h = 0; h < HiddenNeurons; h++)
                {
                    double hiddenGradient = dLossDOutput * _weights2[h] * ReluDerivative(hiddenZ[h]);
                    gradW1[h] += hiddenGradient * input;
                    gradB1[h] += hiddenGradient;
                }
            }

            for (int h = 0; h < HiddenNeurons; h++)
            {
                double gW1 = Clip(gradW1[h] / nx.Length);
                double gB1 = Clip(gradB1[h] / nx.Length);
                double gW2 = Clip(gradW2[h] / nx.Length);

                velocityW1[h] = Momentum * velocityW1[h] - LearningRate * gW1;
                velocityB1[h] = Momentum * velocityB1[h] - LearningRate * gB1;
                velocityW2[h] = Momentum * velocityW2[h] - LearningRate * gW2;

                _weights1[h] += velocityW1[h];
                _biases1[h] += velocityB1[h];
                _weights2[h] += velocityW2[h];
            }

            gradB2 = Clip(gradB2 / nx.Length);
            velocityB2 = Momentum * velocityB2 - LearningRate * gradB2;
            _bias2 += velocityB2;

            double mse = totalMse / nx.Length;
            if (!double.IsFinite(mse))
                throw new InvalidOperationException("Eğitim kararsız hale geldi. Learning rate değerini düşür.");

            history.Add(mse);
            epochsRun = epoch + 1;
            if (mse < Tolerance)
                break;
        }

        return RegressionValidation.BuildResult(this, x, y, epochsRun, history);
    }

    public double Predict(double x)
    {
        EnsureTrained();
        double input = _xScaler.Transform(x);
        double output = _bias2;
        for (int h = 0; h < HiddenNeurons; h++)
        {
            double activation = Relu(_weights1[h] * input + _biases1[h]);
            output += _weights2[h] * activation;
        }
        return _yScaler.InverseTransform(output);
    }

    private void InitializeWeights()
    {
        _weights1 = new double[HiddenNeurons];
        _biases1 = new double[HiddenNeurons];
        _weights2 = new double[HiddenNeurons];
        _bias2 = 0.0;

        double firstLayerScale = Math.Sqrt(2.0);
        double secondLayerScale = Math.Sqrt(2.0 / HiddenNeurons);
        for (int h = 0; h < HiddenNeurons; h++)
        {
            _weights1[h] = NextGaussian() * firstLayerScale;
            _weights2[h] = NextGaussian() * secondLayerScale;
            _biases1[h] = 0.01;
        }
    }

    private double NextGaussian()
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    private static double Relu(double x) => Math.Max(0.0, x);
    private static double ReluDerivative(double x) => x > 0.0 ? 1.0 : 0.0;
    private static double Clip(double value) => Math.Clamp(value, -1.0, 1.0);

    private void EnsureTrained()
    {
        if (_weights1.Length == 0)
            throw new InvalidOperationException("Model henüz eğitilmedi.");
    }
}
