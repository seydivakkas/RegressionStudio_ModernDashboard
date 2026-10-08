using ML.Core.Models;

namespace ML.Core.Regression;

internal static class RegressionValidation
{
    public static void Validate(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        if (x.Count < 2)
            throw new ArgumentException("Regresyon için en az iki örnek gereklidir.", nameof(x));
        if (x.Count != y.Count)
            throw new ArgumentException("X ve Y örnek sayıları eşit olmalıdır.");
        if (x.Any(v => !double.IsFinite(v)) || y.Any(v => !double.IsFinite(v)))
            throw new ArgumentException("Veri NaN veya Infinity içeremez.");
    }

    public static RegressionTrainingResult BuildResult(
        IRegressor model,
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        int epochsRun,
        IReadOnlyList<double> history)
    {
        double mse = 0.0;
        double mae = 0.0;
        double meanY = y.Average();
        double ssRes = 0.0;
        double ssTot = 0.0;

        for (int i = 0; i < x.Count; i++)
        {
            double prediction = model.Predict(x[i]);
            double error = y[i] - prediction;
            mse += error * error;
            mae += Math.Abs(error);
            ssRes += error * error;
            double centered = y[i] - meanY;
            ssTot += centered * centered;
        }

        mse /= x.Count;
        mae /= x.Count;
        double r2 = ssTot < 1e-12 ? (ssRes < 1e-12 ? 1.0 : 0.0) : 1.0 - ssRes / ssTot;

        return new RegressionTrainingResult
        {
            EpochsRun = epochsRun,
            Mse = mse,
            Rmse = Math.Sqrt(mse),
            Mae = mae,
            R2 = r2,
            ErrorHistory = history
        };
    }
}
