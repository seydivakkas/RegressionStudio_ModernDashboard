using ML.Core.Regression;

namespace RegressionApp;

internal sealed class RegressionRun
{
    public required string Name { get; init; }
    public required string ModelType { get; init; }
    public required IRegressor Model { get; init; }
    public required RegressionMetrics TrainMetrics { get; init; }
    public required RegressionMetrics TestMetrics { get; init; }
    public required IReadOnlyList<double> LossHistory { get; init; }
    public required int EpochsRun { get; init; }
    public required double LearningRate { get; init; }
    public int HiddenNeurons { get; init; }
    public double Momentum { get; init; }
    public DateTime TrainedAt { get; init; } = DateTime.Now;
}
