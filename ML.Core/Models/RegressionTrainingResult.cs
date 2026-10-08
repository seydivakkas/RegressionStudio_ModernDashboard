namespace ML.Core.Models;

public sealed class RegressionTrainingResult
{
    public int EpochsRun { get; init; }
    public double Mse { get; init; }
    public double Rmse { get; init; }
    public double Mae { get; init; }
    public double R2 { get; init; }
    public IReadOnlyList<double> ErrorHistory { get; init; } = Array.Empty<double>();
}
