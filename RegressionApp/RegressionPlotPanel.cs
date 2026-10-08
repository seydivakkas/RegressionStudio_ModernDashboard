using System.ComponentModel;
using System.Drawing.Drawing2D;
using ML.Core.Regression;

namespace RegressionApp;

public sealed class RegressionPlotPanel : Panel
{
    private const int LeftPad = 64;
    private const int RightPad = 24;
    private const int TopPad = 44;
    private const int BottomPad = 52;

    private int _dragIndex = -1;
    private Bounds _bounds = new(-5, 5, -5, 5);

    public BindingList<DataPoint> Samples { get; set; } = new();
    public IRegressor? LinearModel { get; set; }
    public IRegressor? MlpModel { get; set; }
    public IRegressor? ActiveModel { get; set; }
    public bool ShowLinearCurve { get; set; } = true;
    public bool ShowMlpCurve { get; set; } = true;
    public bool ShowResidualLines { get; set; } = true;

    public event EventHandler? DataChanged;

    public RegressionPlotPanel()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        Cursor = Cursors.Cross;
        MinimumSize = new Size(520, 400);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        _bounds = CalculateBounds();

        DrawTitle(g);
        DrawGridAndAxes(g);
        if (ShowResidualLines && ActiveModel is not null)
            DrawResiduals(g, ActiveModel);
        if (ShowLinearCurve && LinearModel is not null)
            DrawCurve(g, LinearModel, UiTheme.Linear, DashStyle.Dash, 2.6f);
        if (ShowMlpCurve && MlpModel is not null)
            DrawCurve(g, MlpModel, UiTheme.Mlp, DashStyle.Solid, 3.0f);
        DrawSamples(g);
        DrawLegend(g);
    }

    private void DrawTitle(Graphics g)
    {
        using var titleFont = new Font("Segoe UI Semibold", 12f);
        using var subFont = new Font("Segoe UI", 8.5f);
        using var titleBrush = new SolidBrush(UiTheme.Text);
        using var mutedBrush = new SolidBrush(UiTheme.Muted);
        g.DrawString("Curve fitting workspace", titleFont, titleBrush, 16, 10);
        g.DrawString("Sol tık: ekle / sürükle   •   Sağ tık: sil", subFont, mutedBrush, 190, 14);
    }

    private void DrawGridAndAxes(Graphics g)
    {
        var plot = PlotRectangle;
        using var gridPen = new Pen(UiTheme.Grid, 1f);
        using var axisPen = new Pen(Color.FromArgb(135, 155, 153), 1.35f);
        using var labelFont = new Font("Segoe UI", 8f);
        using var labelBrush = new SolidBrush(UiTheme.Muted);

        for (int i = 0; i <= 5; i++)
        {
            float px = plot.Left + i * plot.Width / 5f;
            float py = plot.Top + i * plot.Height / 5f;
            g.DrawLine(gridPen, px, plot.Top, px, plot.Bottom);
            g.DrawLine(gridPen, plot.Left, py, plot.Right, py);

            double xValue = _bounds.MinX + (_bounds.MaxX - _bounds.MinX) * i / 5.0;
            double yValue = _bounds.MaxY - (_bounds.MaxY - _bounds.MinY) * i / 5.0;
            g.DrawString(xValue.ToString("0.##"), labelFont, labelBrush, px - 15, plot.Bottom + 8);
            g.DrawString(yValue.ToString("0.##"), labelFont, labelBrush, 8, py - 8);
        }

        if (_bounds.MinX <= 0 && _bounds.MaxX >= 0)
        {
            float x0 = ToPixelX(0);
            g.DrawLine(axisPen, x0, plot.Top, x0, plot.Bottom);
        }
        if (_bounds.MinY <= 0 && _bounds.MaxY >= 0)
        {
            float y0 = ToPixelY(0);
            g.DrawLine(axisPen, plot.Left, y0, plot.Right, y0);
        }

        using var axisFont = new Font("Segoe UI Semibold", 8.5f);
        g.DrawString("X", axisFont, labelBrush, plot.Right - 4, plot.Bottom + 29);
        g.DrawString("Y", axisFont, labelBrush, plot.Left - 34, plot.Top - 20);
    }

    private void DrawResiduals(Graphics g, IRegressor model)
    {
        using var pen = new Pen(Color.FromArgb(105, UiTheme.Warning), 1.25f) { DashStyle = DashStyle.Dot };
        foreach (var sample in Samples)
        {
            double prediction;
            try { prediction = model.Predict(sample.X); }
            catch { continue; }
            if (!double.IsFinite(prediction)) continue;
            g.DrawLine(pen, ToPixelX(sample.X), ToPixelY(sample.Y), ToPixelX(sample.X), ToPixelY(prediction));
        }
    }

    private void DrawCurve(Graphics g, IRegressor model, Color color, DashStyle dash, float width)
    {
        using var pen = new Pen(color, width) { DashStyle = dash, LineJoin = LineJoin.Round };
        PointF? previous = null;
        const int steps = 220;
        for (int i = 0; i < steps; i++)
        {
            double x = _bounds.MinX + (_bounds.MaxX - _bounds.MinX) * i / (steps - 1.0);
            double y;
            try { y = model.Predict(x); }
            catch { previous = null; continue; }
            if (!double.IsFinite(y) || y < _bounds.MinY - 3 * (_bounds.MaxY - _bounds.MinY) || y > _bounds.MaxY + 3 * (_bounds.MaxY - _bounds.MinY))
            {
                previous = null;
                continue;
            }
            var point = new PointF(ToPixelX(x), ToPixelY(y));
            if (previous is not null)
                g.DrawLine(pen, previous.Value, point);
            previous = point;
        }
    }

    private void DrawSamples(Graphics g)
    {
        for (int i = 0; i < Samples.Count; i++)
        {
            var sample = Samples[i];
            float x = ToPixelX(sample.X);
            float y = ToPixelY(sample.Y);
            if (sample.IsTest)
            {
                using var fill = new SolidBrush(Color.White);
                using var pen = new Pen(UiTheme.TestPoint, 2.2f);
                g.FillRectangle(fill, x - 5, y - 5, 10, 10);
                g.DrawRectangle(pen, x - 5, y - 5, 10, 10);
            }
            else
            {
                using var fill = new SolidBrush(UiTheme.TrainPoint);
                using var outline = new Pen(Color.White, 1.5f);
                g.FillEllipse(fill, x - 5, y - 5, 10, 10);
                g.DrawEllipse(outline, x - 5, y - 5, 10, 10);
            }
        }
    }

    private void DrawLegend(Graphics g)
    {
        int x = Math.Max(LeftPad + 10, Width - 370);
        int y = 15;
        using var font = new Font("Segoe UI", 8f);
        using var text = new SolidBrush(UiTheme.Muted);
        using var train = new SolidBrush(UiTheme.TrainPoint);
        using var testPen = new Pen(UiTheme.TestPoint, 2f);
        using var linearPen = new Pen(UiTheme.Linear, 2.2f) { DashStyle = DashStyle.Dash };
        using var mlpPen = new Pen(UiTheme.Mlp, 2.4f);

        g.FillEllipse(train, x, y + 4, 8, 8);
        g.DrawString("Train", font, text, x + 12, y);
        g.DrawRectangle(testPen, x + 58, y + 3, 9, 9);
        g.DrawString("Test", font, text, x + 72, y);
        g.DrawLine(linearPen, x + 116, y + 8, x + 144, y + 8);
        g.DrawString("Linear", font, text, x + 149, y);
        g.DrawLine(mlpPen, x + 204, y + 8, x + 232, y + 8);
        g.DrawString("MLP", font, text, x + 237, y);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        int nearest = FindNearest(e.Location, 12f);
        if (e.Button == MouseButtons.Right)
        {
            if (nearest >= 0)
            {
                Samples.RemoveAt(nearest);
                DataChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
            return;
        }

        if (e.Button != MouseButtons.Left || !PlotRectangle.Contains(e.Location)) return;
        if (nearest >= 0)
        {
            _dragIndex = nearest;
            Cursor = Cursors.SizeAll;
        }
        else
        {
            Samples.Add(new DataPoint(ToDataX(e.X), ToDataY(e.Y)));
            _dragIndex = Samples.Count - 1;
            DataChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragIndex < 0 || e.Button != MouseButtons.Left || _dragIndex >= Samples.Count) return;
        var plot = PlotRectangle;
        int px = Math.Clamp(e.X, plot.Left, plot.Right);
        int py = Math.Clamp(e.Y, plot.Top, plot.Bottom);
        Samples[_dragIndex].X = ToDataX(px);
        Samples[_dragIndex].Y = ToDataY(py);
        DataChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_dragIndex >= 0)
            DataChanged?.Invoke(this, EventArgs.Empty);
        _dragIndex = -1;
        Cursor = Cursors.Cross;
    }

    private int FindNearest(Point point, float threshold)
    {
        int nearest = -1;
        double best = threshold * threshold;
        for (int i = 0; i < Samples.Count; i++)
        {
            double dx = ToPixelX(Samples[i].X) - point.X;
            double dy = ToPixelY(Samples[i].Y) - point.Y;
            double d2 = dx * dx + dy * dy;
            if (d2 < best)
            {
                best = d2;
                nearest = i;
            }
        }
        return nearest;
    }

    private Bounds CalculateBounds()
    {
        if (Samples.Count == 0) return new Bounds(-5, 5, -5, 5);
        double minX = Samples.Min(p => p.X);
        double maxX = Samples.Max(p => p.X);
        double minY = Samples.Min(p => p.Y);
        double maxY = Samples.Max(p => p.Y);

        if (Math.Abs(maxX - minX) < 1e-9) { minX -= 1; maxX += 1; }
        if (Math.Abs(maxY - minY) < 1e-9) { minY -= 1; maxY += 1; }

        double xMargin = (maxX - minX) * 0.12 + 0.25;
        double yMargin = (maxY - minY) * 0.14 + 0.25;
        return new Bounds(minX - xMargin, maxX + xMargin, minY - yMargin, maxY + yMargin);
    }

    private Rectangle PlotRectangle => new(
        LeftPad,
        TopPad,
        Math.Max(1, Width - LeftPad - RightPad),
        Math.Max(1, Height - TopPad - BottomPad));

    private float ToPixelX(double x) => PlotRectangle.Left + (float)((x - _bounds.MinX) / (_bounds.MaxX - _bounds.MinX) * PlotRectangle.Width);
    private float ToPixelY(double y) => PlotRectangle.Bottom - (float)((y - _bounds.MinY) / (_bounds.MaxY - _bounds.MinY) * PlotRectangle.Height);
    private double ToDataX(float x) => _bounds.MinX + (x - PlotRectangle.Left) / PlotRectangle.Width * (_bounds.MaxX - _bounds.MinX);
    private double ToDataY(float y) => _bounds.MinY + (PlotRectangle.Bottom - y) / PlotRectangle.Height * (_bounds.MaxY - _bounds.MinY);

    private readonly record struct Bounds(double MinX, double MaxX, double MinY, double MaxY);
}
