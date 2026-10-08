namespace RegressionApp;

internal static class UiTheme
{
    public static readonly Color Background = Color.FromArgb(244, 248, 248);
    public static readonly Color Surface = Color.White;
    public static readonly Color SurfaceAlt = Color.FromArgb(235, 245, 244);
    public static readonly Color Border = Color.FromArgb(211, 225, 223);
    public static readonly Color Text = Color.FromArgb(31, 48, 48);
    public static readonly Color Muted = Color.FromArgb(103, 126, 124);
    public static readonly Color Primary = Color.FromArgb(15, 139, 141);
    public static readonly Color PrimaryDark = Color.FromArgb(9, 105, 107);
    public static readonly Color Accent = Color.FromArgb(46, 163, 115);
    public static readonly Color AccentSoft = Color.FromArgb(221, 244, 235);
    public static readonly Color Warning = Color.FromArgb(230, 145, 56);
    public static readonly Color Danger = Color.FromArgb(210, 75, 75);
    public static readonly Color Grid = Color.FromArgb(228, 236, 235);
    public static readonly Color Linear = Color.FromArgb(15, 139, 141);
    public static readonly Color Mlp = Color.FromArgb(46, 163, 115);
    public static readonly Color TrainPoint = Color.FromArgb(49, 70, 70);
    public static readonly Color TestPoint = Color.FromArgb(230, 145, 56);

    public static Button PrimaryButton(string text)
    {
        var button = BaseButton(text);
        button.BackColor = Primary;
        button.ForeColor = Color.White;
        return button;
    }

    public static Button SecondaryButton(string text)
    {
        var button = BaseButton(text);
        button.BackColor = SurfaceAlt;
        button.ForeColor = Text;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Border;
        return button;
    }

    public static Button AccentButton(string text)
    {
        var button = BaseButton(text);
        button.BackColor = Accent;
        button.ForeColor = Color.White;
        return button;
    }

    private static Button BaseButton(string text) => new()
    {
        Text = text,
        Height = 38,
        AutoSize = false,
        Width = 128,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        Font = new Font("Segoe UI Semibold", 9f),
        Margin = new Padding(4)
    };

    public static Label SectionTitle(string text) => new()
    {
        Text = text.ToUpperInvariant(),
        AutoSize = true,
        ForeColor = Muted,
        Font = new Font("Segoe UI Semibold", 8.5f),
        Margin = new Padding(0, 4, 0, 8)
    };

    public static Panel Card(int padding = 14) => new()
    {
        BackColor = Surface,
        Padding = new Padding(padding),
        Margin = new Padding(0, 0, 0, 12)
    };
}
