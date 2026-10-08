using ML.Core.Models;
using ML.Core.Preprocessing;

namespace ML.Core.Regression;

public sealed class LinearRegression : IRegressor
{
    private readonly StandardScaler1D _xScaler = new();
    private readonly StandardScaler1D _yScaler = new();
    private bool _trained;
    private double _weight;
    private double _bias;

    public double LearningRate { get; }
    public int MaxEpochs { get; }
    public double Tolerance { get; }

    public double Slope
    {
        get
        {
            EnsureTrained();
            return _yScaler.Std * _weight / _xScaler.Std;
        }
    }

    public double Intercept
    {
        get
        {
            EnsureTrained();
            return _yScaler.Mean + _yScaler.Std * _bias - Slope * _xScaler.Mean;
        }
    }

    public LinearRegression(double learningRate = 0.01, int maxEpochs = 10_000, double tolerance = 1e-7)
    {
        if (learningRate <= 0) throw new ArgumentOutOfRangeException(nameof(learningRate));
        if (maxEpochs <= 0) throw new ArgumentOutOfRangeException(nameof(maxEpochs));
        if (tolerance <= 0) throw new ArgumentOutOfRangeException(nameof(tolerance));

        LearningRate = learningRate;
        MaxEpochs = maxEpochs;
        Tolerance = tolerance;
    }

    public RegressionTrainingResult Fit(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        RegressionValidation.Validate(x, y);
        _xScaler.Fit(x);
        _yScaler.Fit(y);

        var nx = _xScaler.Transform(x);
        var ny = _yScaler.Transform(y);
        _weight = 0.0;
        _bias = 0.0;
        _trained = true;

        var history = new List<double>(MaxEpochs);
        int epochsRun = 0;

        for (int epoch = 0; epoch < MaxEpochs; epoch++)
        {
            double gradW = 0.0;
            double gradB = 0.0;
            double mse = 0.0;

            for (int i = 0; i < nx.Length; i++)
            {
                double prediction = _weight * nx[i] + _bias;
                double error = ny[i] - prediction;
                gradW += -2.0 * nx[i] * error;
                gradB += -2.0 * error;
                mse += error * error;
            }

            gradW = Math.Clamp(gradW / nx.Length, -1.0, 1.0);
            gradB = Math.Clamp(gradB / nx.Length, -1.0, 1.0);
            _weight -= LearningRate * gradW;
            _bias -= LearningRate * gradB;

            mse /= nx.Length;
            history.Add(mse);
            epochsRun = epoch + 1;

            if (!double.IsFinite(mse))
                throw new InvalidOperationException("Eğitim kararsız hale geldi. Learning rate değerini düşür.");
            if (mse < Tolerance)
                break;
        }

        return RegressionValidation.BuildResult(this, x, y, epochsRun, history);
    }

    public double Predict(double x)
    {
        EnsureTrained();
        double normalizedX = _xScaler.Transform(x);
        double normalizedY = _weight * normalizedX + _bias;
        return _yScaler.InverseTransform(normalizedY);
    }

    private void EnsureTrained()
    {
        if (!_trained)
            throw new InvalidOperationException("Model henüz eğitilmedi.");
    }
}
