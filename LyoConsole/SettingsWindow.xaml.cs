using System.Text.Json;
using System.Windows;
using LyoConsole.Models;
using LyoConsole.ViewModels;
using Microsoft.Win32;

namespace LyoConsole;

public partial class SettingsWindow : Window
{
    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly SettingsViewModel _viewModel;

    public AppSettings ResultSettings { get; private set; } = new();

    public SettingsWindow(AppSettings currentSettings)
    {
        InitializeComponent();

        var settingsCopy = DeepClone(currentSettings);
        _viewModel = new SettingsViewModel(settingsCopy);
        DataContext = _viewModel;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedProgram is null)
        {
            if (_viewModel.Programs.Count == 0)
                _viewModel.AddProgramCommand.Execute(null);
            else
                _viewModel.SelectedProgram = _viewModel.Programs[0];

            if (_viewModel.SelectedProgram is null)
            {
                MessageBox.Show(this, "请先选择或添加一个程序行，然后再点击“浏览”。", "浏览", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        var dialog = new OpenFileDialog
        {
            Title = "选择要运行的控制台程序",
            Filter = "控制台程序 (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != true)
            return;

        _viewModel.SelectedProgram.FilePath = dialog.FileName;
        if (string.IsNullOrWhiteSpace(_viewModel.SelectedProgram.WorkingDirectory))
            _viewModel.SelectedProgram.WorkingDirectory = System.IO.Path.GetDirectoryName(dialog.FileName) ?? "";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ResultSettings = _viewModel.BuildResultSettings();
        DialogResult = true;
    }

    private static AppSettings DeepClone(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, CloneOptions);
            return JsonSerializer.Deserialize<AppSettings>(json, CloneOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }
}
