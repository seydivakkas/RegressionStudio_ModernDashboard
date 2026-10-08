using System.ComponentModel;
using System.Globalization;
using ML.Core.Models;
using ML.Core.Regression;

namespace RegressionApp;

public sealed class MainForm : Form
{
    private readonly BindingList<DataPoint> _samples = new();
    private readonly RegressionPlotPanel _plot = new() { Dock = DockStyle.Fill };
    private readonly ResidualPlotPanel _residualPlot = new() { Dock = DockStyle.Fill };
    private readonly LearningCurvePanel _learningCurve = new() { Dock = DockStyle.Fill };
    private readonly ExperimentChartPanel _experimentChart = new() { Dock = DockStyle.Fill };
    private readonly ModelSummaryPanel _modelSummary = new() { Dock = DockStyle.Top };

    private readonly DataGridView _dataGrid = new();
    private readonly DataGridView _comparisonGrid = new();
    private readonly DataGridView _experimentGrid = new();

    private readonly ComboBox _datasetType = Combo(["Linear", "Quadratic", "Sinusoidal", "Noisy Linear", "Nonlinear"]);
    private readonly NumericUpDown _sampleCount = Num(8, 500, 60, 1, 0);
    private readonly NumericUpDown _seed = Num(1, 999999, 42, 1, 0);
    private readonly NumericUpDown _testPercent = Num(10, 50, 25, 5, 0);
    private readonly TrackBar _noise = new() { Minimum = 0, Maximum = 200, Value = 30, TickFrequency = 25, Width = 210 };
    private readonly Label _noiseValue = SmallValue("0.30 σ");

    private readonly ComboBox _algorithm = Combo(["Linear Regression", "MLP Regression"]);
    private readonly NumericUpDown _learningRate = Num(0.0001m, 1m, 0.01m, 0.001m, 4);
    private readonly NumericUpDown _epochs = Num(10, 100000, 5000, 250, 0);
    private readonly NumericUpDown _momentum = Num(0m, 0.99m, 0.90m, 0.05m, 2);
    private readonly NumericUpDown _hidden = Num(2, 128, 12, 1, 0);
    private readonly NumericUpDown _predictX = Num(-100m, 100m, 1m, 0.1m, 2);

    private readonly CheckBox _showLinear = Check("Linear curve", true);
    private readonly CheckBox _showMlp = Check("MLP curve", true);
    private readonly CheckBox _showResiduals = Check("Residual lines", true);

    private readonly Button _generateButton = UiTheme.PrimaryButton("Generate");
    private readonly Button _clearDataButton = UiTheme.SecondaryButton("Clear data");
    private readonly Button _importButton = UiTheme.SecondaryButton("Import CSV");
    private readonly Button _exportButton = UiTheme.SecondaryButton("Export CSV");
    private readonly Button _trainSelectedButton = UiTheme.PrimaryButton("Train selected");
    private readonly Button _trainBothButton = UiTheme.AccentButton("Train both");
    private readonly Button _resetModelsButton = UiTheme.SecondaryButton("Reset models");
    private readonly Button _predictButton = UiTheme.PrimaryButton("Predict");
    private readonly Button _lrSweepButton = UiTheme.PrimaryButton("LR Sweep");
    private readonly Button _epochSweepButton = UiTheme.AccentButton("Epoch Sweep");

    private readonly Label _datasetStats = new() { AutoSize = true, ForeColor = UiTheme.Muted, Font = new Font("Segoe UI", 8.5f) };
    private readonly Label _predictionLinear = new() { AutoSize = true, ForeColor = UiTheme.Linear, Font = new Font("Segoe UI Semibold", 10f) };
    private readonly Label _predictionMlp = new() { AutoSize = true, ForeColor = UiTheme.Mlp, Font = new Font("Segoe UI Semibold", 10f) };

    private readonly MetricCard _trainMse = new("TRAIN MSE");
    private readonly MetricCard _trainRmse = new("TRAIN RMSE");
    private readonly MetricCard _trainMae = new("TRAIN MAE");
    private readonly MetricCard _trainR2 = new("TRAIN R²");
    private readonly MetricCard _testMse = new("TEST MSE");
    private readonly MetricCard _testRmse = new("TEST RMSE");
    private readonly MetricCard _testMae = new("TEST MAE");
    private readonly MetricCard _testR2 = new("TEST R²");

    private readonly ToolStripStatusLabel _statusLabel = new("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripProgressBar _progress = new() { Width = 150, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 25, Visible = false };

    private RegressionRun? _linearRun;
    private RegressionRun? _mlpRun;
    private RegressionRun? _activeRun;
    private bool _suppressDataEvents;

    public MainForm()
    {
        Text = "Regression Studio — Curve Fitting & Numerical Analysis";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1320, 780);
        Size = new Size(1540, 930);
        BackColor = UiTheme.Background;
        Font = new Font("Segoe UI", 9.5f);

        _datasetType.SelectedIndex = 0;
        _algorithm.SelectedIndex = 0;
        ConfigureDataGrid();
        ConfigureComparisonGrid();
        ConfigureExperimentGrid();

        _plot.Samples = _samples;
        _residualPlot.Samples = _samples;
        _samples.ListChanged += Samples_ListChanged;

        WireEvents();
        Controls.Add(BuildRoot());
        GenerateDataset();
        UpdateAlgorithmState();
    }

    private Control BuildRoot()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildWorkspace(), 0, 1);
        root.Controls.Add(BuildStatusStrip(), 0, 2);
        return root;
    }

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(22, 12, 22, 8) };
        var title = new Label
        {
            Text = "REGRESSION STUDIO",
            AutoSize = true,
            ForeColor = UiTheme.PrimaryDark,
            Font = new Font("Segoe UI Semibold", 20f),
            Location = new Point(22, 10)
        };
        var subtitle = new Label
        {
            Text = "Curve fitting • residual analysis • model comparison • from-scratch optimization",
            AutoSize = true,
            ForeColor = UiTheme.Muted,
            Font = new Font("Segoe UI", 9f),
            Location = new Point(25, 48)
        };
        var badge = new Label
        {
            Text = ".NET 8  |  C#  |  NO ML LIBRARY",
            AutoSize = true,
            ForeColor = UiTheme.PrimaryDark,
            BackColor = UiTheme.SurfaceAlt,
            Font = new Font("Segoe UI Semibold", 8.5f),
            Padding = new Padding(10, 6, 10, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(Width - 300, 21)
        };
        header.Resize += (_, _) => badge.Left = header.ClientSize.Width - badge.Width - 22;
        header.Controls.AddRange([title, subtitle, badge]);
        return header;
    }

    private Control BuildWorkspace()
    {
        var workspace = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(14, 12, 14, 12),
            BackColor = UiTheme.Background
        };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 292));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 410));
        workspace.Controls.Add(BuildDataSidebar(), 0, 0);
        workspace.Controls.Add(BuildVisualizationArea(), 1, 0);
        workspace.Controls.Add(BuildAnalysisSidebar(), 2, 0);
        return workspace;
    }

    private Control BuildDataSidebar()
    {
        var host = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background, Padding = new Padding(0, 0, 12, 0) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 450));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var controls = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(14), Margin = new Padding(0, 0, 0, 10) };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        flow.Controls.Add(UiTheme.SectionTitle("Dataset builder"));
        flow.Controls.Add(Field("Function", _datasetType));
        flow.Controls.Add(Field("Sample count", _sampleCount));

        var noiseHost = new Panel { Width = 238, Height = 66 };
        noiseHost.Controls.Add(new Label { Text = "Gaussian noise", AutoSize = true, ForeColor = UiTheme.Text, Font = new Font("Segoe UI Semibold", 8.5f), Location = new Point(0, 0) });
        _noise.Location = new Point(-6, 20);
        _noiseValue.Location = new Point(179, 2);
        noiseHost.Controls.AddRange([_noise, _noiseValue]);
        flow.Controls.Add(noiseHost);
        flow.Controls.Add(Field("Train / test split (% test)", _testPercent));
        flow.Controls.Add(Field("Random seed", _seed));

        var generateRow = ButtonRow(_generateButton, _clearDataButton);
        var csvRow = ButtonRow(_importButton, _exportButton);
        flow.Controls.Add(generateRow);
        flow.Controls.Add(csvRow);
        _datasetStats.Margin = new Padding(2, 6, 0, 0);
        flow.Controls.Add(_datasetStats);
        controls.Controls.Add(flow);

        var tableCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        var tableLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tableLayout.Controls.Add(UiTheme.SectionTitle("Editable X / Y data"), 0, 0);
        tableLayout.Controls.Add(_dataGrid, 0, 1);
        tableCard.Controls.Add(tableLayout);

        layout.Controls.Add(controls, 0, 0);
        layout.Controls.Add(tableCard, 0, 1);
        host.Controls.Add(layout);
        return host;
    }

    private Control BuildVisualizationArea()
    {
        var host = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background, Padding = new Padding(0, 0, 12, 0) };
        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(0) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 68));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 32));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(12, 8, 8, 4),
            BackColor = UiTheme.SurfaceAlt
        };
        toolbar.Controls.Add(new Label { Text = "VIEW", AutoSize = true, ForeColor = UiTheme.Muted, Font = new Font("Segoe UI Semibold", 8f), Margin = new Padding(0, 6, 12, 0) });
        toolbar.Controls.AddRange([_showLinear, _showMlp, _showResiduals]);

        layout.Controls.Add(toolbar, 0, 0);
        layout.Controls.Add(_plot, 0, 1);
        layout.Controls.Add(_residualPlot, 0, 2);
        card.Controls.Add(layout);
        host.Controls.Add(card);
        return host;
    }

    private Control BuildAnalysisSidebar()
    {
        var tabs = new ModernTabControl { Dock = DockStyle.Fill, BackColor = UiTheme.Background };
        tabs.TabPages.Add(BuildModelTab());
        tabs.TabPages.Add(BuildMetricsTab());
        tabs.TabPages.Add(BuildLearningTab());
        tabs.TabPages.Add(BuildExperimentsTab());
        return tabs;
    }

    private TabPage BuildModelTab()
    {
        var page = Tab("MODEL");
        var host = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(10) };

        var settings = new CardPanel { Width = 365, Height = 312 };
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        AddRow(grid, 0, "Algorithm", _algorithm);
        AddRow(grid, 1, "Learning rate", _learningRate);
        AddRow(grid, 2, "Max epoch", _epochs);
        AddRow(grid, 3, "Momentum", _momentum);
        AddRow(grid, 4, "Hidden neurons", _hidden);
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        grid.Controls.Add(ButtonRow(_trainSelectedButton, _trainBothButton), 0, 5);
        grid.SetColumnSpan(grid.GetControlFromPosition(0, 5)!, 2);
        settings.Controls.Add(grid);
        host.Controls.Add(UiTheme.SectionTitle("Model configuration"));
        host.Controls.Add(settings);

        _resetModelsButton.Width = 365;
        host.Controls.Add(_resetModelsButton);
        host.Controls.Add(UiTheme.SectionTitle("Model summary"));
        _modelSummary.Width = 365;
        host.Controls.Add(_modelSummary);
        host.Controls.Add(BuildPredictionCard());
        page.Controls.Add(host);
        return page;
    }

    private TabPage BuildMetricsTab()
    {
        var page = Tab("METRICS");
        var host = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(10) };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 176));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        host.Controls.Add(UiTheme.SectionTitle("Active model performance"), 0, 0);
        var cards = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = false };
        cards.Controls.AddRange([_trainMse, _trainRmse, _trainMae, _trainR2, _testMse, _testRmse, _testMae, _testR2]);
        host.Controls.Add(cards, 0, 1);

        var comparison = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        var cmpLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        cmpLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        cmpLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cmpLayout.Controls.Add(UiTheme.SectionTitle("Linear vs MLP — test set"), 0, 0);
        cmpLayout.Controls.Add(_comparisonGrid, 0, 1);
        comparison.Controls.Add(cmpLayout);
        host.Controls.Add(comparison, 0, 2);
        page.Controls.Add(host);
        return page;
    }

    private TabPage BuildLearningTab()
    {
        var page = Tab("LEARNING");
        var host = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        host.Controls.Add(new Label
        {
            Text = "İki modelin normalize eğitim MSE geçmişi aynı eksende gösterilir.",
            AutoSize = true,
            ForeColor = UiTheme.Muted,
            Font = new Font("Segoe UI", 8.5f)
        }, 0, 0);
        host.Controls.Add(_learningCurve, 0, 1);
        page.Controls.Add(host);
        return page;
    }

    private TabPage BuildExperimentsTab()
    {
        var page = Tab("EXPERIMENTS");
        var host = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(10) };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 48));

        var intro = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        _lrSweepButton.Width = 165;
        _epochSweepButton.Width = 165;
        intro.Controls.AddRange([_lrSweepButton, _epochSweepButton]);
        host.Controls.Add(intro, 0, 0);
        host.Controls.Add(_experimentChart, 0, 1);
        host.Controls.Add(UiTheme.SectionTitle("Experiment results"), 0, 2);
        host.Controls.Add(_experimentGrid, 0, 3);
        page.Controls.Add(host);
        return page;
    }

    private Control BuildPredictionCard()
    {
        var card = new CardPanel { Width = 365, Height = 150, Padding = new Padding(14) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        layout.Controls.Add(new Label { Text = "Single value prediction", AutoSize = true, ForeColor = UiTheme.Text, Font = new Font("Segoe UI Semibold", 9f) }, 0, 0);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 2);
        layout.Controls.Add(_predictX, 0, 1);
        _predictButton.Width = 120;
        layout.Controls.Add(_predictButton, 1, 1);
        var outputs = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        outputs.Controls.AddRange([_predictionLinear, _predictionMlp]);
        layout.Controls.Add(outputs, 0, 2);
        layout.SetColumnSpan(outputs, 2);
        card.Controls.Add(layout);
        return card;
    }

    private StatusStrip BuildStatusStrip()
    {
        var strip = new StatusStrip { BackColor = UiTheme.Surface, SizingGrip = false };
        strip.Items.Add(_statusLabel);
        strip.Items.Add(_progress);
        return strip;
    }

    private void WireEvents()
    {
        _noise.ValueChanged += (_, _) => _noiseValue.Text = $"{NoiseStd:0.00} σ";
        _generateButton.Click += (_, _) => GenerateDataset();
        _clearDataButton.Click += (_, _) => ClearDataset();
        _importButton.Click += (_, _) => ImportCsv();
        _exportButton.Click += (_, _) => ExportCsv();
        _testPercent.ValueChanged += (_, _) => { ApplyTrainTestSplit(); InvalidateModels("Train/test oranı değişti."); };
        _seed.ValueChanged += (_, _) => { ApplyTrainTestSplit(); InvalidateModels("Random seed değişti."); };
        _algorithm.SelectedIndexChanged += (_, _) => { UpdateAlgorithmState(); ActivateSelectedModel(); };
        _trainSelectedButton.Click += async (_, _) => await TrainSelectedAsync();
        _trainBothButton.Click += async (_, _) => await TrainBothAsync();
        _resetModelsButton.Click += (_, _) => InvalidateModels("Modeller sıfırlandı.");
        _predictButton.Click += (_, _) => UpdatePrediction();
        _showLinear.CheckedChanged += (_, _) => { _plot.ShowLinearCurve = _showLinear.Checked; _plot.Invalidate(); };
        _showMlp.CheckedChanged += (_, _) => { _plot.ShowMlpCurve = _showMlp.Checked; _plot.Invalidate(); };
        _showResiduals.CheckedChanged += (_, _) => { _plot.ShowResidualLines = _showResiduals.Checked; _plot.Invalidate(); };
        _lrSweepButton.Click += async (_, _) => await RunLearningRateSweepAsync();
        _epochSweepButton.Click += async (_, _) => await RunEpochSweepAsync();
        _plot.DataChanged += (_, _) => _dataGrid.Refresh();
    }

    private void ConfigureDataGrid()
    {
        _dataGrid.Dock = DockStyle.Fill;
        _dataGrid.BackgroundColor = UiTheme.Surface;
        _dataGrid.BorderStyle = BorderStyle.None;
        _dataGrid.AutoGenerateColumns = false;
        _dataGrid.AllowUserToAddRows = true;
        _dataGrid.AllowUserToDeleteRows = true;
        _dataGrid.RowHeadersVisible = false;
        _dataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _dataGrid.MultiSelect = false;
        _dataGrid.EnableHeadersVisualStyles = false;
        _dataGrid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.SurfaceAlt;
        _dataGrid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.Text;
        _dataGrid.DefaultCellStyle.SelectionBackColor = UiTheme.AccentSoft;
        _dataGrid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
        _dataGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "X", DataPropertyName = nameof(DataPoint.X), Width = 72, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.####" } });
        _dataGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Y", DataPropertyName = nameof(DataPoint.Y), Width = 72, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.####" } });
        _dataGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Set", DataPropertyName = nameof(DataPoint.Set), Width = 62, ReadOnly = true });
        _dataGrid.DataSource = _samples;
        _dataGrid.CellValidating += (sender, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex > 1) return;
            if (!TryParseDouble(Convert.ToString(e.FormattedValue), out _))
            {
                e.Cancel = true;
                _dataGrid.Rows[e.RowIndex].ErrorText = "Sayısal bir değer gir.";
            }
            else
            {
                _dataGrid.Rows[e.RowIndex].ErrorText = string.Empty;
            }
        };
        _dataGrid.DataError += (_, e) => { e.ThrowException = false; };
    }

    private void ConfigureComparisonGrid()
    {
        ConfigureReadOnlyGrid(_comparisonGrid);
        _comparisonGrid.Columns.Add("Model", "Model");
        _comparisonGrid.Columns.Add("MSE", "MSE");
        _comparisonGrid.Columns.Add("RMSE", "RMSE");
        _comparisonGrid.Columns.Add("MAE", "MAE");
        _comparisonGrid.Columns.Add("R2", "R²");
        _comparisonGrid.Columns.Add("Epoch", "Epoch");
    }

    private void ConfigureExperimentGrid()
    {
        ConfigureReadOnlyGrid(_experimentGrid);
        _experimentGrid.Columns.Add("Parameter", "Parameter");
        _experimentGrid.Columns.Add("MSE", "Test MSE");
        _experimentGrid.Columns.Add("R2", "Test R²");
        _experimentGrid.Columns.Add("Epoch", "Epoch");
    }

    private static void ConfigureReadOnlyGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.BackgroundColor = UiTheme.Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.SurfaceAlt;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.Text;
        grid.DefaultCellStyle.SelectionBackColor = UiTheme.AccentSoft;
        grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
    }

    private void Samples_ListChanged(object? sender, ListChangedEventArgs e)
    {
        if (_suppressDataEvents) return;
        UpdateDatasetStats();
        InvalidateModels("Veri değişti; modeller yeniden eğitilmeli.");
        _plot.Invalidate();
    }

    private void GenerateDataset()
    {
        _suppressDataEvents = true;
        try
        {
            _samples.Clear();
            foreach (var point in DatasetGenerator.Generate(_datasetType.Text, (int)_sampleCount.Value, NoiseStd, (int)_seed.Value))
                _samples.Add(point);
        }
        finally { _suppressDataEvents = false; }
        ApplyTrainTestSplit();
        InvalidateModels($"{_datasetType.Text} dataset oluşturuldu.");
        UpdateDatasetStats();
        _dataGrid.Refresh();
        _plot.Invalidate();
    }

    private void ClearDataset()
    {
        _suppressDataEvents = true;
        try { _samples.Clear(); }
        finally { _suppressDataEvents = false; }
        InvalidateModels("Dataset temizlendi.");
        UpdateDatasetStats();
        _plot.Invalidate();
    }

    private void ApplyTrainTestSplit()
    {
        if (_samples.Count == 0) { UpdateDatasetStats(); return; }
        _suppressDataEvents = true;
        try
        {
            foreach (var sample in _samples) sample.IsTest = false;
            if (_samples.Count >= 3)
            {
                int testCount = (int)Math.Round(_samples.Count * (double)_testPercent.Value / 100.0);
                testCount = Math.Clamp(testCount, 1, Math.Max(1, _samples.Count - 2));
                var random = new Random((int)_seed.Value + 7919);
                var indices = Enumerable.Range(0, _samples.Count).OrderBy(_ => random.Next()).Take(testCount);
                foreach (int index in indices) _samples[index].IsTest = true;
            }
        }
        finally { _suppressDataEvents = false; }
        UpdateDatasetStats();
        _dataGrid.Refresh();
        _plot.Invalidate();
    }

    private async Task TrainSelectedAsync()
    {
        if (!CanTrain()) return;
        ApplyTrainTestSplit();
        SetBusy(true, $"{SelectedModelType} eğitiliyor...");
        try
        {
            await TrainModelInternalAsync(SelectedModelType);
            ActivateSelectedModel();
            _statusLabel.Text = $"{SelectedModelType} eğitimi tamamlandı.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Training error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = "Eğitim başarısız.";
        }
        finally { SetBusy(false, null); }
    }

    private async Task TrainBothAsync()
    {
        if (!CanTrain()) return;
        ApplyTrainTestSplit();
        SetBusy(true, "Linear ve MLP aynı split üzerinde eğitiliyor...");
        try
        {
            await TrainModelInternalAsync("Linear");
            await TrainModelInternalAsync("MLP");
            ActivateSelectedModel();
            _statusLabel.Text = "Linear + MLP karşılaştırması hazır.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Training error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = "Karşılaştırmalı eğitim başarısız.";
        }
        finally { SetBusy(false, null); }
    }

    private async Task<RegressionRun> TrainModelInternalAsync(string modelType, double? overrideLr = null, int? overrideEpochs = null)
    {
        var train = _samples.Where(p => !p.IsTest).ToArray();
        var test = _samples.Where(p => p.IsTest).ToArray();
        if (train.Length < 2) throw new InvalidOperationException("Train set için en az 2 örnek gerekli.");

        double lr = overrideLr ?? (double)_learningRate.Value;
        int epochs = overrideEpochs ?? (int)_epochs.Value;
        int hidden = (int)_hidden.Value;
        double momentum = (double)_momentum.Value;
        int seed = (int)_seed.Value;

        IRegressor model = modelType == "Linear"
            ? new LinearRegression(lr, epochs)
            : new MlpRegressor(hidden, lr, epochs, momentum, seed: seed);

        double[] trainX = train.Select(p => p.X).ToArray();
        double[] trainY = train.Select(p => p.Y).ToArray();
        RegressionTrainingResult training = await Task.Run(() => model.Fit(trainX, trainY));
        RegressionMetrics trainMetrics = RegressionMetrics.Evaluate(model, trainX, trainY);
        RegressionMetrics testMetrics = test.Length > 0
            ? RegressionMetrics.Evaluate(model, test.Select(p => p.X).ToArray(), test.Select(p => p.Y).ToArray())
            : trainMetrics;

        var run = new RegressionRun
        {
            Name = $"{modelType} {DateTime.Now:HH:mm:ss}",
            ModelType = modelType,
            Model = model,
            TrainMetrics = trainMetrics,
            TestMetrics = testMetrics,
            LossHistory = training.ErrorHistory,
            EpochsRun = training.EpochsRun,
            LearningRate = lr,
            HiddenNeurons = modelType == "MLP" ? hidden : 0,
            Momentum = modelType == "MLP" ? momentum : 0
        };

        if (modelType == "Linear")
        {
            _linearRun = run;
            _plot.LinearModel = model;
            _learningCurve.LinearHistory = training.ErrorHistory;
        }
        else
        {
            _mlpRun = run;
            _plot.MlpModel = model;
            _learningCurve.MlpHistory = training.ErrorHistory;
        }

        UpdateComparisonGrid();
        _learningCurve.Invalidate();
        _plot.Invalidate();
        return run;
    }

    private void ActivateSelectedModel()
    {
        _activeRun = _algorithm.SelectedIndex == 0 ? _linearRun : _mlpRun;
        _plot.ActiveModel = _activeRun?.Model;
        _residualPlot.Model = _activeRun?.Model;
        _residualPlot.ModelLabel = _activeRun is null ? "Aktif model" : _activeRun.ModelType;
        UpdateMetricsCards();
        UpdateModelSummary();
        UpdatePrediction();
        _plot.Invalidate();
        _residualPlot.Invalidate();
    }

    private void UpdateModelSummary()
    {
        if (_activeRun is null)
        {
            _modelSummary.Reset();
            return;
        }
        if (_activeRun.Model is LinearRegression linear)
            _modelSummary.ShowLinear(linear.Slope, linear.Intercept, _activeRun.TestMetrics);
        else
            _modelSummary.ShowMlp(_activeRun.HiddenNeurons, _activeRun.Momentum, _activeRun.TestMetrics);
    }

    private void UpdateMetricsCards()
    {
        if (_activeRun is null)
        {
            foreach (var card in MetricCards) card.Reset();
            return;
        }
        _trainMse.Value = _activeRun.TrainMetrics.Mse.ToString("0.#####");
        _trainRmse.Value = _activeRun.TrainMetrics.Rmse.ToString("0.#####");
        _trainMae.Value = _activeRun.TrainMetrics.Mae.ToString("0.#####");
        _trainR2.Value = _activeRun.TrainMetrics.R2.ToString("0.####");
        _testMse.Value = _activeRun.TestMetrics.Mse.ToString("0.#####");
        _testRmse.Value = _activeRun.TestMetrics.Rmse.ToString("0.#####");
        _testMae.Value = _activeRun.TestMetrics.Mae.ToString("0.#####");
        _testR2.Value = _activeRun.TestMetrics.R2.ToString("0.####");
    }

    private void UpdateComparisonGrid()
    {
        _comparisonGrid.Rows.Clear();
        AddComparisonRow(_linearRun);
        AddComparisonRow(_mlpRun);
    }

    private void AddComparisonRow(RegressionRun? run)
    {
        if (run is null) return;
        _comparisonGrid.Rows.Add(
            run.ModelType,
            run.TestMetrics.Mse.ToString("0.#####"),
            run.TestMetrics.Rmse.ToString("0.#####"),
            run.TestMetrics.Mae.ToString("0.#####"),
            run.TestMetrics.R2.ToString("0.####"),
            run.EpochsRun);
    }

    private void UpdatePrediction()
    {
        double x = (double)_predictX.Value;
        _predictionLinear.Text = _linearRun is null ? "Linear: —" : $"Linear ŷ = {_linearRun.Model.Predict(x):0.####}";
        _predictionMlp.Text = _mlpRun is null ? "MLP: —" : $"MLP ŷ = {_mlpRun.Model.Predict(x):0.####}";
    }

    private async Task RunLearningRateSweepAsync()
    {
        if (!CanTrain()) return;
        ApplyTrainTestSplit();
        double current = (double)_learningRate.Value;
        var rates = new[] { current * 0.25, current * 0.5, current, current * 2.0, current * 4.0 }
            .Select(v => Math.Clamp(v, 0.0001, 1.0))
            .Distinct()
            .ToArray();

        SetBusy(true, $"{SelectedModelType} learning-rate sweep çalışıyor...");
        try
        {
            var results = new List<ExperimentResult>();
            foreach (double rate in rates)
            {
                _statusLabel.Text = $"LR sweep: {rate:0.#####}";
                var run = await TrainTemporaryAsync(SelectedModelType, rate, (int)_epochs.Value);
                results.Add(new ExperimentResult { Parameter = rate.ToString("0.#####"), TestMse = run.TestMetrics.Mse, TestR2 = run.TestMetrics.R2, EpochsRun = run.EpochsRun });
            }
            ShowExperimentResults(results, $"Learning-rate comparison — {SelectedModelType}");
            _statusLabel.Text = "Learning-rate sweep tamamlandı.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Experiment error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { SetBusy(false, null); }
    }

    private async Task RunEpochSweepAsync()
    {
        if (!CanTrain()) return;
        ApplyTrainTestSplit();
        int current = (int)_epochs.Value;
        var epochs = new[] { Math.Max(10, current / 10), Math.Max(10, current / 4), Math.Max(10, current / 2), current }
            .Distinct()
            .OrderBy(v => v)
            .ToArray();

        SetBusy(true, $"{SelectedModelType} epoch sweep çalışıyor...");
        try
        {
            var results = new List<ExperimentResult>();
            foreach (int epoch in epochs)
            {
                _statusLabel.Text = $"Epoch sweep: {epoch}";
                var run = await TrainTemporaryAsync(SelectedModelType, (double)_learningRate.Value, epoch);
                results.Add(new ExperimentResult { Parameter = epoch.ToString(), TestMse = run.TestMetrics.Mse, TestR2 = run.TestMetrics.R2, EpochsRun = run.EpochsRun });
            }
            ShowExperimentResults(results, $"Epoch comparison — {SelectedModelType}");
            _statusLabel.Text = "Epoch sweep tamamlandı.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Experiment error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { SetBusy(false, null); }
    }

    private async Task<RegressionRun> TrainTemporaryAsync(string modelType, double lr, int epochs)
    {
        var train = _samples.Where(p => !p.IsTest).ToArray();
        var test = _samples.Where(p => p.IsTest).ToArray();
        IRegressor model = modelType == "Linear"
            ? new LinearRegression(lr, epochs)
            : new MlpRegressor((int)_hidden.Value, lr, epochs, (double)_momentum.Value, seed: (int)_seed.Value);
        var trainX = train.Select(p => p.X).ToArray();
        var trainY = train.Select(p => p.Y).ToArray();
        var fit = await Task.Run(() => model.Fit(trainX, trainY));
        var testMetrics = test.Length > 0
            ? RegressionMetrics.Evaluate(model, test.Select(p => p.X).ToArray(), test.Select(p => p.Y).ToArray())
            : RegressionMetrics.Evaluate(model, trainX, trainY);
        return new RegressionRun
        {
            Name = "Experiment",
            ModelType = modelType,
            Model = model,
            TrainMetrics = RegressionMetrics.Evaluate(model, trainX, trainY),
            TestMetrics = testMetrics,
            LossHistory = fit.ErrorHistory,
            EpochsRun = fit.EpochsRun,
            LearningRate = lr,
            HiddenNeurons = modelType == "MLP" ? (int)_hidden.Value : 0,
            Momentum = modelType == "MLP" ? (double)_momentum.Value : 0
        };
    }

    private void ShowExperimentResults(IReadOnlyList<ExperimentResult> results, string title)
    {
        _experimentChart.Results = results;
        _experimentChart.Title = title;
        _experimentChart.Invalidate();
        _experimentGrid.Rows.Clear();
        foreach (var item in results)
            _experimentGrid.Rows.Add(item.Parameter, item.TestMse.ToString("0.#####"), item.TestR2.ToString("0.####"), item.EpochsRun);
    }

    private bool CanTrain()
    {
        _dataGrid.EndEdit();
        if (_samples.Count < 6)
        {
            MessageBox.Show("Eğitim ve test ayrımı için en az 6 veri noktası ekle.", "Insufficient data", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        return true;
    }

    private void InvalidateModels(string status)
    {
        _linearRun = null;
        _mlpRun = null;
        _activeRun = null;
        _plot.LinearModel = null;
        _plot.MlpModel = null;
        _plot.ActiveModel = null;
        _residualPlot.Model = null;
        _learningCurve.LinearHistory = Array.Empty<double>();
        _learningCurve.MlpHistory = Array.Empty<double>();
        _modelSummary.Reset();
        foreach (var card in MetricCards) card.Reset();
        _comparisonGrid.Rows.Clear();
        _predictionLinear.Text = "Linear: —";
        _predictionMlp.Text = "MLP: —";
        _statusLabel.Text = status;
        _plot.Invalidate();
        _residualPlot.Invalidate();
        _learningCurve.Invalidate();
    }

    private void UpdateAlgorithmState()
    {
        bool mlp = _algorithm.SelectedIndex == 1;
        _momentum.Enabled = mlp;
        _hidden.Enabled = mlp;
    }

    private void UpdateDatasetStats()
    {
        int test = _samples.Count(p => p.IsTest);
        int train = _samples.Count - test;
        _datasetStats.Text = $"{_samples.Count} points  •  train {train}  •  test {test}";
    }

    private void ImportCsv()
    {
        using var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*", Title = "Import X/Y data" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var imported = new List<DataPoint>();
            foreach (string raw in File.ReadLines(dialog.FileName))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                char separator = line.Contains(';') ? ';' : line.Contains('\t') ? '\t' : ',';
                string[] parts = line.Split(separator);
                if (parts.Length < 2) continue;
                if (!TryParseDouble(parts[0], out double x) || !TryParseDouble(parts[1], out double y))
                    continue; // header or invalid row
                imported.Add(new DataPoint(x, y));
            }
            if (imported.Count < 2) throw new InvalidDataException("CSV içinde en az iki geçerli X,Y satırı bulunamadı.");

            _suppressDataEvents = true;
            try
            {
                _samples.Clear();
                foreach (var point in imported) _samples.Add(point);
            }
            finally { _suppressDataEvents = false; }
            ApplyTrainTestSplit();
            InvalidateModels($"CSV içe aktarıldı: {Path.GetFileName(dialog.FileName)}");
            UpdateDatasetStats();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "CSV import error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportCsv()
    {
        if (_samples.Count == 0)
        {
            MessageBox.Show("Dışa aktarılacak veri yok.", "CSV export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "regression_dataset.csv", Title = "Export X/Y data" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        using var writer = new StreamWriter(dialog.FileName, false);
        writer.WriteLine("X,Y,Set");
        foreach (var point in _samples)
            writer.WriteLine($"{point.X.ToString("R", CultureInfo.InvariantCulture)},{point.Y.ToString("R", CultureInfo.InvariantCulture)},{point.Set}");
        _statusLabel.Text = $"CSV kaydedildi: {Path.GetFileName(dialog.FileName)}";
    }

    private void SetBusy(bool busy, string? message)
    {
        UseWaitCursor = busy;
        _progress.Visible = busy;
        _trainSelectedButton.Enabled = !busy;
        _trainBothButton.Enabled = !busy;
        _lrSweepButton.Enabled = !busy;
        _epochSweepButton.Enabled = !busy;
        _generateButton.Enabled = !busy;
        _importButton.Enabled = !busy;
        if (!string.IsNullOrWhiteSpace(message)) _statusLabel.Text = message;
    }

    private double NoiseStd => _noise.Value / 100.0;
    private string SelectedModelType => _algorithm.SelectedIndex == 0 ? "Linear" : "MLP";
    private IEnumerable<MetricCard> MetricCards => new[] { _trainMse, _trainRmse, _trainMae, _trainR2, _testMse, _testRmse, _testMae, _testR2 };

    private static TabPage Tab(string text) => new() { Text = text, BackColor = UiTheme.Background };

    private static FlowLayoutPanel ButtonRow(params Control[] controls)
    {
        var row = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 6, 0, 2) };
        foreach (var control in controls)
        {
            control.Width = 112;
            row.Controls.Add(control);
        }
        return row;
    }

    private static Control Field(string label, Control control)
    {
        var host = new Panel { Width = 238, Height = 58, Margin = new Padding(0, 2, 0, 2) };
        host.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = UiTheme.Text, Font = new Font("Segoe UI Semibold", 8.5f), Location = new Point(0, 0) });
        control.Location = new Point(0, 24);
        control.Width = 224;
        host.Controls.Add(control);
        return host;
    }

    private static void AddRow(TableLayoutPanel grid, int row, string label, Control control)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        grid.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            ForeColor = UiTheme.Text,
            Font = new Font("Segoe UI Semibold", 8.5f),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(2, 10, 4, 0)
        }, 0, row);
        control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        control.Margin = new Padding(4, 5, 2, 4);
        grid.Controls.Add(control, 1, row);
    }

    private static ComboBox Combo(string[] items)
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Height = 30 };
        combo.Items.AddRange(items);
        return combo;
    }

    private static NumericUpDown Num(decimal min, decimal max, decimal value, decimal increment, int decimals) => new()
    {
        Minimum = min,
        Maximum = max,
        Value = value,
        Increment = increment,
        DecimalPlaces = decimals,
        ThousandsSeparator = true,
        Height = 30
    };

    private static CheckBox Check(string text, bool value) => new()
    {
        Text = text,
        Checked = value,
        AutoSize = true,
        ForeColor = UiTheme.Text,
        Font = new Font("Segoe UI", 8.5f),
        Margin = new Padding(5, 4, 10, 0)
    };

    private static Label SmallValue(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = UiTheme.PrimaryDark,
        Font = new Font("Segoe UI Semibold", 8f)
    };

    private static bool TryParseDouble(string? text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
