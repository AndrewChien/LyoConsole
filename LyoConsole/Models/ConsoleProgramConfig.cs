using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LyoConsole.Models;

public sealed class ConsoleProgramConfig : INotifyPropertyChanged
{
    private string _name = "新程序";
    private string _filePath = "";
    private string _arguments = "";
    private string _workingDirectory = "";
    private string _outputEncoding = "auto";
    private int _startOrder;
    private int _startDelayMs;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set
        {
            if (value == _name)
                return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (value == _filePath)
                return;
            _filePath = value;
            OnPropertyChanged();
        }
    }

    public string Arguments
    {
        get => _arguments;
        set
        {
            if (value == _arguments)
                return;
            _arguments = value;
            OnPropertyChanged();
        }
    }

    public string WorkingDirectory
    {
        get => _workingDirectory;
        set
        {
            if (value == _workingDirectory)
                return;
            _workingDirectory = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Output encoding for redirected stdout/stderr (e.g. auto/gbk/utf-8/65001/936).
    /// </summary>
    public string OutputEncoding
    {
        get => _outputEncoding;
        set
        {
            if (value == _outputEncoding)
                return;
            _outputEncoding = value;
            OnPropertyChanged();
        }
    }

    public int StartOrder
    {
        get => _startOrder;
        set
        {
            if (value == _startOrder)
                return;
            _startOrder = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Delay (ms) after starting this program, before starting the next one.
    /// </summary>
    public int StartDelayMs
    {
        get => _startDelayMs;
        set
        {
            if (value == _startDelayMs)
                return;
            _startDelayMs = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
