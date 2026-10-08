namespace RegressionApp;

internal sealed class ExperimentResult
{
    public required string Parameter { get; init; }
    public required double TestMse { get; init; }
    public required double TestR2 { get; init; }
    public required int EpochsRun { get; init; }
}
