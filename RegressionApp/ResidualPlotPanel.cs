using System.ComponentModel;
using System.Drawing.Drawing2D;
using ML.Core.Regression;

namespace RegressionApp;

internal sealed class ResidualPlotPanel : Panel
{
    public BindingList<DataPoint> Samples { get; set; } = new();
    public IRegressor? Model { get; set; }
    public string ModelLabel { get; set; } = "Aktif model";

    public ResidualPlotPanel()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        MinimumSize = new Size(400, 200);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var titleFont = new Font("Segoe UI Semibold", 10f);
        using var small = new Font("Segoe UI", 8f);
        using var text = new SolidBrush(UiTheme.Text);
        using var muted = new SolidBrush(UiTheme.Muted);
        g.DrawString("Residual analysis", titleFont, text, 14, 10);
        g.DrawString($"e = y - ŷ   •   {ModelLabel}", small, muted, 130, 13);

        var plot = new Rectangle(56, 38, Math.Max(1, Width - 76), Math.Max(1, Height - 70));
        using var grid = new Pen(UiTheme.Grid);
        for (int i = 0; i <= 4; i++)
        {
            float x = plot.Left + i * plot.Width / 4f;
            float y = plot.Top + i * plot.Height / 4f;
            g.DrawLine(grid, x, plot.Top, x, plot.Bottom);
            g.DrawLine(grid, plot.Left, y, plot.Right, y);
        }

        if (Model is null || Samples.Count == 0)
        {
            g.DrawString("Model eğitildiğinde residual dağılımı burada görünür.", small, muted, plot.Left + 16, plot.Top + 16);
            return;
        }

        var values = new List<(double X, double Residual, bool Test)>();
        foreach (var sample in Samples)
        {
            try
            {
                double residual = sample.Y - Model.Predict(sample.X);
                if (double.IsFinite(residual)) values.Add((sample.X, residual, sample.IsTest));
            }
            catch { }
        }
        if (values.Count == 0) return;

        double minX = values.Min(v => v.X);
        double maxX = values.Max(v => v.X);
        double maxAbs = Math.Max(0.1, values.Max(v => Math.Abs(v.Residual)) * 1.15);
        if (Math.Abs(maxX - minX) < 1e-9) { minX -= 1; maxX += 1; }

        float MapX(double x) => plot.Left + (float)((x - minX) / (maxX - minX) * plot.Width);
        float MapY(double r) => plot.Top + plot.Height / 2f - (float)(r / maxAbs * plot.Height / 2f);

        using var zeroPen = new Pen(Color.FromArgb(125, 145, 143), 1.4f) { DashStyle = DashStyle.Dash };
        g.DrawLine(zeroPen, plot.Left, MapY(0), plot.Right, MapY(0));

        foreach (var value in values)
        {
            float x = MapX(value.X);
            float y = MapY(value.Residual);
            if (value.Test)
            {
                using var pen = new Pen(UiTheme.TestPoint, 1.8f);
                g.DrawRectangle(pen, x - 3.5f, y - 3.5f, 7, 7);
            }
            else
            {
                using var brush = new SolidBrush(UiTheme.Primary);
                g.FillEllipse(brush, x - 3.5f, y - 3.5f, 7, 7);
            }
        }

        g.DrawString($"+{maxAbs:0.##}", small, muted, 8, plot.Top - 3);
        g.DrawString("0", small, muted, 28, MapY(0) - 7);
        g.DrawString($"-{maxAbs:0.##}", small, muted, 8, plot.Bottom - 12);
    }
}
