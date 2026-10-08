namespace ML.Core.Regression;

public sealed class RegressionMetrics
{
    public int Count { get; init; }
    public double Mse { get; init; }
    public double Rmse { get; init; }
    public double Mae { get; init; }
    public double R2 { get; init; }

    public static RegressionMetrics Evaluate(
        IRegressor model,
        IReadOnlyList<double> x,
        IReadOnlyList<double> y)
    {
        if (x.Count == 0 || y.Count == 0)
            return new RegressionMetrics();
        if (x.Count != y.Count)
            throw new ArgumentException("X ve Y örnek sayıları eşit olmalıdır.");

        double meanY = y.Average();
        double sumSquared = 0.0;
        double sumAbsolute = 0.0;
        double totalVariance = 0.0;

        for (int i = 0; i < x.Count; i++)
        {
            double prediction = model.Predict(x[i]);
            double error = y[i] - prediction;
            sumSquared += error * error;
            sumAbsolute += Math.Abs(error);

            double centered = y[i] - meanY;
            totalVariance += centered * centered;
        }

        double mse = sumSquared / x.Count;
        double r2 = totalVariance < 1e-12
            ? (sumSquared < 1e-12 ? 1.0 : 0.0)
            : 1.0 - sumSquared / totalVariance;

        return new RegressionMetrics
        {
            Count = x.Count,
            Mse = mse,
            Rmse = Math.Sqrt(mse),
            Mae = sumAbsolute / x.Count,
            R2 = r2
        };
    }
}
