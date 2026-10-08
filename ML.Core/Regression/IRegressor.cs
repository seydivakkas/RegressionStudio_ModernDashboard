using ML.Core.Models;

namespace ML.Core.Regression;

public interface IRegressor
{
    RegressionTrainingResult Fit(IReadOnlyList<double> x, IReadOnlyList<double> y);
    double Predict(double x);
}
