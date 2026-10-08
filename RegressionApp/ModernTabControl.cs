namespace RegressionApp;

internal sealed class ModernTabControl : TabControl
{
    public ModernTabControl()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        ItemSize = new Size(90, 34);
        SizeMode = TabSizeMode.Fixed;
        Padding = new Point(14, 5);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        var page = TabPages[e.Index];
        bool selected = e.Index == SelectedIndex;
        var rect = GetTabRect(e.Index);
        using var background = new SolidBrush(selected ? UiTheme.Surface : UiTheme.Background);
        e.Graphics.FillRectangle(background, rect);
        using var font = new Font("Segoe UI Semibold", 8.5f);
        TextRenderer.DrawText(
            e.Graphics,
            page.Text,
            font,
            rect,
            selected ? UiTheme.PrimaryDark : UiTheme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        if (selected)
        {
            using var pen = new Pen(UiTheme.Primary, 3f);
            e.Graphics.DrawLine(pen, rect.Left + 8, rect.Bottom - 2, rect.Right - 8, rect.Bottom - 2);
        }
    }
}
