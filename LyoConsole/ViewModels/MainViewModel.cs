using System.Collections.ObjectModel;
using System.Windows.Input;
using LyoConsole.Models;
using LyoConsole.Services;

namespace LyoConsole.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly SettingsStorage _settingsStorage = new();
    private CancellationTokenSource? _startAllCts;

    private string _statusText = "就绪";
    private string _nowText = "";
    private string _settingsPath = "";

    public ObservableCollection<ConsoleProgramViewModel> Programs { get; } = [];

    public ConsoleProgramViewModel? SelectedProgram { get; set; }

    public AppSettings Settings { get; private set; } = new();

    public UiSettings Ui => Settings.Ui;

    public string SettingsPath
    {
        get => _settingsPath;
        private set => SetProperty(ref _settingsPath, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string NowText
    {
        get => _nowText;
        set => SetProperty(ref _nowText, value);
    }

    public ICommand StartAllCommand { get; }
    public ICommand StopAllCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenPortManagerCommand { get; }
    public ICommand ExitCommand { get; }

    public event Action? OpenSettingsRequested;
    public event Action? OpenPortManagerRequested;
    public event Action? ExitRequested;

    public MainViewModel()
    {
        StartAllCommand = new AsyncRelayCommand(StartAllAsync, CanStartAll);
        StopAllCommand = new RelayCommand(StopAll, CanStopAll);
        OpenSettingsCommand = new RelayCommand(() => OpenSettingsRequested?.Invoke());
        OpenPortManagerCommand = new RelayCommand(() => OpenPortManagerRequested?.Invoke());
        ExitCommand = new RelayCommand(() => ExitRequested?.Invoke());
    }

    public async Task InitializeAsync()
    {
        var (settings, loadedFromPath) = await _settingsStorage.LoadAsync();
        Settings = settings;
        SettingsPath = loadedFromPath;

        RebuildPrograms();
        StatusText = Programs.Count == 0 ? "未配置程序，请先打开“设置”。" : "就绪";

        RaisePropertyChanged(nameof(Ui));
        RaiseGlobalCanExecute();
    }

    public async Task ApplyAndSaveSettingsAsync(AppSettings newSettings)
    {
        Settings = newSettings;
        RebuildPrograms();
        RaisePropertyChanged(nameof(Ui));
        await _settingsStorage.SaveAsync(Settings);
        StatusText = "设置已保存。";
        RaiseGlobalCanExecute();
    }

    private void RebuildPrograms()
    {
        foreach (var vm in Programs)
            vm.Dispose();

        Programs.Clear();

        foreach (var config in Settings.Programs.OrderBy(p => p.StartOrder))
            Programs.Add(new ConsoleProgramViewModel(config, RaiseGlobalCanExecute));
    }

    private bool CanStartAll() => Programs.Count > 0 && Programs.Any(p => !p.IsRunning) && _startAllCts is null;

    private bool CanStopAll() => Programs.Any(p => p.IsRunning) || _startAllCts is not null;

    private async Task StartAllAsync()
    {
        if (_startAllCts is not null)
            return;

        _startAllCts = new CancellationTokenSource();
        RaiseGlobalCanExecute();

        try
        {
            StatusText = "正在启动...";

            foreach (var program in Programs.OrderBy(p => p.Config.StartOrder))
            {
                _startAllCts.Token.ThrowIfCancellationRequested();

                if (!program.IsRunning)
                    program.Start();

                var delay = Math.Max(0, program.Config.StartDelayMs);
                if (delay > 0)
                    await Task.Delay(delay, _startAllCts.Token);
            }

            StatusText = "已启动。";
        }
        catch (OperationCanceledException)
        {
            StatusText = "启动已取消。";
        }
        finally
        {
            _startAllCts?.Dispose();
            _startAllCts = null;
            RaiseGlobalCanExecute();
        }
    }

    private void StopAll()
    {
        _startAllCts?.Cancel();

        foreach (var program in Programs)
            program.Stop();

        StatusText = "已停止。";
        RaiseGlobalCanExecute();
    }

    private void RaiseGlobalCanExecute()
    {
        (StartAllCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (StopAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    public void Dispose()
    {
        _startAllCts?.Cancel();
        _startAllCts?.Dispose();

        foreach (var vm in Programs)
            vm.Dispose();
    }
}
