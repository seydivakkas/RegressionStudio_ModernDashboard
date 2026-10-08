using ML.Core.Regression;

namespace RegressionApp;

internal sealed class ModelSummaryPanel : Panel
{
    private readonly Label _typeLabel;
    private readonly Label _mainLabel;
    private readonly Label _detailsLabel;

    public ModelSummaryPanel()
    {
        Height = 122;
        BackColor = UiTheme.SurfaceAlt;
        Padding = new Padding(14);

        _typeLabel = new Label
        {
            Text = "MODEL",
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8f),
            Location = new Point(14, 12)
        };
        _mainLabel = new Label
        {
            Text = "Henüz eğitilmedi",
            ForeColor = UiTheme.PrimaryDark,
            AutoSize = false,
            Size = new Size(315, 32),
            Font = new Font("Segoe UI Semibold", 13f),
            Location = new Point(14, 34)
        };
        _detailsLabel = new Label
        {
            Text = "Model ayarları burada özetlenir.",
            ForeColor = UiTheme.Muted,
            AutoSize = false,
            Size = new Size(315, 44),
            Font = new Font("Segoe UI", 8.5f),
            Location = new Point(14, 72)
        };
        Controls.AddRange([_typeLabel, _mainLabel, _detailsLabel]);
    }

    public void ShowLinear(double slope, double intercept, RegressionMetrics test)
    {
        _typeLabel.Text = "LINEAR REGRESSION";
        _mainLabel.Text = $"y = {slope:0.####}x {(intercept >= 0 ? "+" : "-")} {Math.Abs(intercept):0.####}";
        _detailsLabel.Text = $"Test R² {test.R2:0.####}   •   RMSE {test.Rmse:0.####}   •   MAE {test.Mae:0.####}";
    }

    public void ShowMlp(int hidden, double momentum, RegressionMetrics test)
    {
        _typeLabel.Text = "MLP REGRESSION";
        _mainLabel.Text = $"1  →  {hidden} ReLU  →  1";
        _detailsLabel.Text = $"Momentum {momentum:0.00}   •   Test R² {test.R2:0.####}   •   RMSE {test.Rmse:0.####}";
    }

    public void Reset()
    {
        _typeLabel.Text = "MODEL";
        _mainLabel.Text = "Henüz eğitilmedi";
        _detailsLabel.Text = "Model ayarları burada özetlenir.";
    }
}
