namespace ML.Core.Preprocessing;

public sealed class StandardScaler1D
{
    public double Mean { get; private set; }
    public double Std { get; private set; } = 1.0;
    public bool IsFitted { get; private set; }

    public void Fit(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            throw new ArgumentException("En az bir değer gereklidir.", nameof(values));

        Mean = values.Average();
        double variance = 0.0;
        for (int i = 0; i < values.Count; i++)
        {
            double diff = values[i] - Mean;
            variance += diff * diff;
        }

        Std = Math.Sqrt(variance / values.Count);
        if (Std < 1e-12)
            Std = 1.0;
        IsFitted = true;
    }

    public double Transform(double value)
    {
        EnsureFitted();
        return (value - Mean) / Std;
    }

    public double InverseTransform(double value)
    {
        EnsureFitted();
        return value * Std + Mean;
    }

    public double[] Transform(IReadOnlyList<double> values)
    {
        EnsureFitted();
        var result = new double[values.Count];
        for (int i = 0; i < values.Count; i++)
            result[i] = Transform(values[i]);
        return result;
    }

    private void EnsureFitted()
    {
        if (!IsFitted)
            throw new InvalidOperationException("Scaler önce Fit edilmelidir.");
    }
}
