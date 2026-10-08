using System.Drawing.Drawing2D;

namespace RegressionApp;

internal sealed class LearningCurvePanel : Panel
{
    public IReadOnlyList<double> LinearHistory { get; set; } = Array.Empty<double>();
    public IReadOnlyList<double> MlpHistory { get; set; } = Array.Empty<double>();

    public LearningCurvePanel()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        MinimumSize = new Size(320, 220);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var titleFont = new Font("Segoe UI Semibold", 10f);
        using var small = new Font("Segoe UI", 8f);
        using var titleBrush = new SolidBrush(UiTheme.Text);
        using var mutedBrush = new SolidBrush(UiTheme.Muted);
        g.DrawString("Learning curve", titleFont, titleBrush, 14, 10);
        g.DrawString("Normalize edilmiş eğitim MSE'si", small, mutedBrush, 114, 13);

        var linear = LinearHistory.Where(double.IsFinite).ToArray();
        var mlp = MlpHistory.Where(double.IsFinite).ToArray();
        if (linear.Length < 2 && mlp.Length < 2)
        {
            g.DrawString("Eğitim sonrasında loss geçmişi burada görünür.", small, mutedBrush, 18, 48);
            return;
        }

        var all = linear.Concat(mlp).Where(v => v >= 0).ToArray();
        double max = all.Length == 0 ? 1 : Math.Max(1e-9, all.Max());
        double min = all.Length == 0 ? 0 : Math.Max(0, all.Min());
        if (Math.Abs(max - min) < 1e-12) max = min + 1;
        int maxCount = Math.Max(linear.Length, mlp.Length);
        var plot = new Rectangle(52, 42, Math.Max(1, Width - 70), Math.Max(1, Height - 76));

        using var grid = new Pen(UiTheme.Grid);
        for (int i = 0; i <= 4; i++)
        {
            float y = plot.Top + i * plot.Height / 4f;
            g.DrawLine(grid, plot.Left, y, plot.Right, y);
        }

        DrawSeries(g, plot, linear, maxCount, min, max, UiTheme.Linear, DashStyle.Dash);
        DrawSeries(g, plot, mlp, maxCount, min, max, UiTheme.Mlp, DashStyle.Solid);

        using var linearPen = new Pen(UiTheme.Linear, 2f) { DashStyle = DashStyle.Dash };
        using var mlpPen = new Pen(UiTheme.Mlp, 2f);
        g.DrawLine(linearPen, plot.Left, plot.Bottom + 20, plot.Left + 24, plot.Bottom + 20);
        g.DrawString("Linear", small, mutedBrush, plot.Left + 30, plot.Bottom + 14);
        g.DrawLine(mlpPen, plot.Left + 90, plot.Bottom + 20, plot.Left + 114, plot.Bottom + 20);
        g.DrawString("MLP", small, mutedBrush, plot.Left + 120, plot.Bottom + 14);
        g.DrawString(max.ToString("0.####"), small, mutedBrush, 4, plot.Top - 5);
        g.DrawString(min.ToString("0.####"), small, mutedBrush, 4, plot.Bottom - 10);
    }

    private static void DrawSeries(Graphics g, Rectangle plot, IReadOnlyList<double> values, int maxCount, double min, double max, Color color, DashStyle dash)
    {
        if (values.Count < 2) return;
        using var pen = new Pen(color, 2.2f) { DashStyle = dash, LineJoin = LineJoin.Round };
        PointF? previous = null;
        int step = Math.Max(1, values.Count / Math.Max(200, plot.Width));
        for (int i = 0; i < values.Count; i += step)
        {
            float x = plot.Left + (float)i / Math.Max(1, maxCount - 1) * plot.Width;
            double normalized = (values[i] - min) / (max - min);
            float y = plot.Bottom - (float)Math.Clamp(normalized, 0, 1) * plot.Height;
            var point = new PointF(x, y);
            if (previous is not null) g.DrawLine(pen, previous.Value, point);
            previous = point;
        }
        if ((values.Count - 1) % step != 0)
        {
            int i = values.Count - 1;
            float x = plot.Left + (float)i / Math.Max(1, maxCount - 1) * plot.Width;
            double normalized = (values[i] - min) / (max - min);
            float y = plot.Bottom - (float)Math.Clamp(normalized, 0, 1) * plot.Height;
            var point = new PointF(x, y);
            if (previous is not null) g.DrawLine(pen, previous.Value, point);
        }
    }
}
