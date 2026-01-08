using System.Collections.ObjectModel;
using System.Windows.Input;
using LyoConsole.Models;
using LyoConsole.Services;

namespace LyoConsole.ViewModels;

public sealed class PortManagerViewModel : ObservableObject
{
    private readonly PortUsageService _service = new();
    private PortUsageEntry? _selectedPort;
    private string _statusText = "准备就绪";
    private string _lastUpdatedText = "";

    public ObservableCollection<PortUsageEntry> Ports { get; } = [];

    public PortUsageEntry? SelectedPort
    {
        get => _selectedPort;
        set => SetProperty(ref _selectedPort, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string LastUpdatedText
    {
        get => _lastUpdatedText;
        set => SetProperty(ref _lastUpdatedText, value);
    }

    public ICommand RefreshCommand { get; }

    public PortManagerViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public async Task RefreshAsync()
    {
        StatusText = "正在刷新...";
        IReadOnlyList<PortUsageEntry> list;
        try
        {
            list = await Task.Run(() => _service.GetAllPortUsages());
        }
        catch (Exception ex)
        {
            StatusText = $"刷新失败：{ex.Message}";
            return;
        }

        Ports.Clear();
        foreach (var item in list)
            Ports.Add(item);

        StatusText = $"共 {Ports.Count} 条";
        LastUpdatedText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
