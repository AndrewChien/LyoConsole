namespace LyoConsole.Models;

public sealed class UiSettings : System.ComponentModel.INotifyPropertyChanged
{
    private string _outputForeground = "#00FF00";
    private string _outputBackground = "#000000";
    private string _outputFontFamily = "Consolas";
    private double _outputFontSize = 14;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public string OutputForeground
    {
        get => _outputForeground;
        set
        {
            if (value == _outputForeground)
                return;
            _outputForeground = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(OutputForeground)));
        }
    }

    public string OutputBackground
    {
        get => _outputBackground;
        set
        {
            if (value == _outputBackground)
                return;
            _outputBackground = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(OutputBackground)));
        }
    }

    public string OutputFontFamily
    {
        get => _outputFontFamily;
        set
        {
            if (value == _outputFontFamily)
                return;
            _outputFontFamily = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(OutputFontFamily)));
        }
    }

    public double OutputFontSize
    {
        get => _outputFontSize;
        set
        {
            if (value.Equals(_outputFontSize))
                return;
            _outputFontSize = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(OutputFontSize)));
        }
    }
}
