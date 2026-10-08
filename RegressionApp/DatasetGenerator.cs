namespace RegressionApp;

internal static class DatasetGenerator
{
    public static List<DataPoint> Generate(string kind, int count, double noiseStd, int seed)
    {
        count = Math.Clamp(count, 8, 500);
        var random = new Random(seed);
        var result = new List<DataPoint>(count);

        for (int i = 0; i < count; i++)
        {
            double x = -5.0 + 10.0 * i / Math.Max(1, count - 1);
            x += NextGaussian(random) * 0.035;

            double baseY = kind switch
            {
                "Linear" => 1.75 * x + 0.85,
                "Quadratic" => 0.55 * x * x - 0.85 * x - 1.2,
                "Sinusoidal" => 2.5 * Math.Sin(1.15 * x) + 0.22 * x,
                "Noisy Linear" => 1.55 * x - 0.45,
                "Nonlinear" => 0.10 * x * x * x - 0.70 * x + 1.25 * Math.Sin(1.35 * x),
                _ => 1.75 * x + 0.85
            };

            double effectiveNoise = kind == "Noisy Linear" ? Math.Max(noiseStd, 0.65) : noiseStd;
            double y = baseY + NextGaussian(random) * effectiveNoise;
            result.Add(new DataPoint(x, y));
        }

        return result.OrderBy(p => p.X).ToList();
    }

    private static double NextGaussian(Random random)
    {
        double u1 = 1.0 - random.NextDouble();
        double u2 = 1.0 - random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
