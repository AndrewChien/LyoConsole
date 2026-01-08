using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using LyoConsole.ViewModels;

namespace LyoConsole;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private PortManagerWindow? _portManagerWindow;

    public MainWindow()
    {
        InitializeComponent();

        DataContext = _viewModel;

        Loaded += OnLoaded;
        Closing += OnClosing;

        _viewModel.OpenSettingsRequested += OnOpenSettingsRequested;
        _viewModel.OpenPortManagerRequested += OnOpenPortManagerRequested;
        _viewModel.ExitRequested += () => Close();

        _clockTimer.Tick += (_, _) =>
        {
            _viewModel.NowText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        };
        _clockTimer.Start();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        _viewModel.NowText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _clockTimer.Stop();
        _viewModel.Dispose();
    }

    private async void OnOpenSettingsRequested()
    {
        var dialog = new SettingsWindow(_viewModel.Settings)
        {
            Owner = this,
        };

        if (dialog.ShowDialog() == true)
            await _viewModel.ApplyAndSaveSettingsAsync(dialog.ResultSettings);
    }

    private void OnOpenPortManagerRequested()
    {
        if (_portManagerWindow is { IsVisible: true })
        {
            _portManagerWindow.Activate();
            return;
        }

        var window = new PortManagerWindow
        {
            Owner = this,
        };
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_portManagerWindow, window))
                _portManagerWindow = null;
        };
        _portManagerWindow = window;
        window.Show();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            "LyoConsole - 控制台控制器\n批量运行并交互多个控制台程序。",
            "关于",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
