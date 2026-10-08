namespace RegressionApp;

internal sealed class CardPanel : Panel
{
    public Color BorderColor { get; set; } = UiTheme.Border;

    public CardPanel()
    {
        BackColor = UiTheme.Surface;
        Padding = new Padding(14);
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(BorderColor);
        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;
        e.Graphics.DrawRectangle(pen, rect);
    }
}
