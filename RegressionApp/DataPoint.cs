using System.ComponentModel;

namespace RegressionApp;

public sealed class DataPoint : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private bool _isTest;

    public DataPoint() { }

    public DataPoint(double x, double y, bool isTest = false)
    {
        _x = x;
        _y = y;
        _isTest = isTest;
    }

    public double X
    {
        get => _x;
        set
        {
            if (Math.Abs(_x - value) < 1e-12) return;
            _x = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X)));
        }
    }

    public double Y
    {
        get => _y;
        set
        {
            if (Math.Abs(_y - value) < 1e-12) return;
            _y = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y)));
        }
    }

    [Browsable(false)]
    public bool IsTest
    {
        get => _isTest;
        set
        {
            if (_isTest == value) return;
            _isTest = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTest)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Set)));
        }
    }

    public string Set => IsTest ? "Test" : "Train";

    public event PropertyChangedEventHandler? PropertyChanged;
}
