using System.Drawing.Drawing2D;

namespace RegressionApp;

internal sealed class ExperimentChartPanel : Panel
{
    public IReadOnlyList<ExperimentResult> Results { get; set; } = Array.Empty<ExperimentResult>();
    public string Title { get; set; } = "Experiment comparison";

    public ExperimentChartPanel()
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
        using var small = new Font("Segoe UI", 7.8f);
        using var titleBrush = new SolidBrush(UiTheme.Text);
        using var mutedBrush = new SolidBrush(UiTheme.Muted);
        g.DrawString(Title, titleFont, titleBrush, 14, 10);
        g.DrawString("Düşük test MSE daha iyidir", small, mutedBrush, 14, 30);

        if (Results.Count == 0)
        {
            g.DrawString("LR Sweep veya Epoch Sweep çalıştır.", small, mutedBrush, 18, 58);
            return;
        }

        var plot = new Rectangle(54, 48, Math.Max(1, Width - 72), Math.Max(1, Height - 88));
        double max = Math.Max(1e-9, Results.Max(r => r.TestMse));
        using var grid = new Pen(UiTheme.Grid);
        for (int i = 0; i <= 4; i++)
        {
            float y = plot.Top + i * plot.Height / 4f;
            g.DrawLine(grid, plot.Left, y, plot.Right, y);
        }

        using var pen = new Pen(UiTheme.Primary, 2.4f) { LineJoin = LineJoin.Round };
        using var brush = new SolidBrush(UiTheme.Accent);
        PointF? previous = null;
        for (int i = 0; i < Results.Count; i++)
        {
            float x = plot.Left + (Results.Count == 1 ? plot.Width / 2f : i * plot.Width / (Results.Count - 1f));
            float y = plot.Bottom - (float)(Results[i].TestMse / max) * plot.Height;
            var point = new PointF(x, y);
            if (previous is not null) g.DrawLine(pen, previous.Value, point);
            g.FillEllipse(brush, x - 4, y - 4, 8, 8);
            string label = Results[i].Parameter;
            var size = g.MeasureString(label, small);
            g.DrawString(label, small, mutedBrush, x - size.Width / 2f, plot.Bottom + 8);
            previous = point;
        }
        g.DrawString(max.ToString("0.####"), small, mutedBrush, 4, plot.Top - 4);
        g.DrawString("0", small, mutedBrush, 34, plot.Bottom - 9);
    }
}
