using System.Text;
using System.Windows;
using System.Windows.Input;
using LyoConsole.Models;
using LyoConsole.Services;

namespace LyoConsole.ViewModels;

public sealed class ConsoleProgramViewModel : ObservableObject, IDisposable
{
    private readonly StringBuilder _output = new();
    private readonly object _outputLock = new();
    private readonly ConsoleProcessHost _host;
    private readonly Action _raiseStartStopCanExecute;

    private string _outputText = "";
    private string _inputText = "";
    private bool _isRunning;

    public ConsoleProgramConfig Config { get; }

    public string Name => Config.Name;

    public string OutputText
    {
        get => _outputText;
        private set => SetProperty(ref _outputText, value);
    }

    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
                RaisePropertyChanged(nameof(TabHeader));
        }
    }

    public string TabHeader => IsRunning ? $"{Name}（运行中）" : Name;

    public ICommand SendInputCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ClearOutputCommand { get; }

    public ConsoleProgramViewModel(ConsoleProgramConfig config, Action raiseStartStopCanExecute)
    {
        Config = config;
        _raiseStartStopCanExecute = raiseStartStopCanExecute;
        _host = new ConsoleProcessHost(config);

        _host.OutputReceived += OnOutputReceived;
        _host.Exited += OnExited;

        SendInputCommand = new RelayCommand(SendInput, () => IsRunning);
        StartCommand = new RelayCommand(Start, () => !IsRunning);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        ClearOutputCommand = new RelayCommand(ClearOutput);
    }

    public void Start()
    {
        _host.Start();
        IsRunning = _host.IsRunning;
        RaiseCommandState();
    }

    public void Stop()
    {
        _host.Stop();
        // IsRunning will be updated on exit; but also update quickly.
        IsRunning = _host.IsRunning;
        RaiseCommandState();
    }

    public void SendInput()
    {
        var text = InputText;
        if (string.IsNullOrWhiteSpace(text))
            return;

        _host.SendLine(text);
        AppendLocal($"[输入] {text}");
        InputText = "";
    }

    public void ClearOutput()
    {
        lock (_outputLock)
            _output.Clear();

        OutputText = "";
    }

    private void OnOutputReceived(string line, bool isError)
    {
        var prefix = isError ? "[错误] " : "";
        AppendLocal(prefix + line);
    }

    private void OnExited(int? exitCode)
    {
        AppendLocal(exitCode is null ? "[退出]" : $"[退出] 退出码={exitCode}");
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsRunning = false;
            RaiseCommandState();
        });
    }

    private void AppendLocal(string line)
    {
        lock (_outputLock)
        {
            _output.AppendLine(line);
            if (_output.Length > 200_000)
                _output.Remove(0, _output.Length - 150_000);
        }

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            string text;
            lock (_outputLock)
                text = _output.ToString();

            OutputText = text;
        });
    }

    private void RaiseCommandState()
    {
        (SendInputCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
        _raiseStartStopCanExecute();
    }

    public void Dispose()
    {
        try
        {
            _host.Stop();
        }
        catch
        {
            // ignored
        }
        _host.Dispose();
    }
}
