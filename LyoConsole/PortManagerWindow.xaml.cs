using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LyoConsole.ViewModels;

namespace LyoConsole;

public partial class PortManagerWindow : Window
{
    private readonly PortManagerViewModel _viewModel = new();

    public PortManagerWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
        => await _viewModel.RefreshAsync();

    private async void KillSelected_Click(object sender, RoutedEventArgs e)
    {
        var selected = _viewModel.SelectedPort;
        if (selected is null)
            return;

        var msg = $"确定要结束进程？\n\n端口：{selected.Port}\n协议：{selected.Protocol}\nPID：{selected.Pid}\n名称：{selected.ProcessName}";
        if (MessageBox.Show(this, msg, "结束进程", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            var process = Process.GetProcessById(selected.Pid);
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"结束进程失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await _viewModel.RefreshAsync();
    }

    private void PortsGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row is not null)
            PortsGrid.SelectedItem = row.Item;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T t)
                return t;
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}

