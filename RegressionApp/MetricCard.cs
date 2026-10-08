namespace RegressionApp;

internal sealed class MetricCard : Panel
{
    private readonly Label _value;
    private readonly Label _caption;

    public MetricCard(string caption)
    {
        Width = 82;
        Height = 72;
        BackColor = UiTheme.SurfaceAlt;
        Padding = new Padding(10, 8, 10, 8);
        Margin = new Padding(4);

        _caption = new Label
        {
            Text = caption,
            AutoSize = true,
            ForeColor = UiTheme.Muted,
            Font = new Font("Segoe UI Semibold", 8f),
            Location = new Point(10, 8)
        };
        _value = new Label
        {
            Text = "—",
            AutoSize = true,
            ForeColor = UiTheme.Text,
            Font = new Font("Segoe UI Semibold", 10.5f),
            Location = new Point(10, 31)
        };
        Controls.Add(_caption);
        Controls.Add(_value);
    }

    public string Value
    {
        get => _value.Text;
        set => _value.Text = value;
    }

    public void Reset() => _value.Text = "—";
}
