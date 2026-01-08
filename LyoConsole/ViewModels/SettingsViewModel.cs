using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using LyoConsole.Models;

namespace LyoConsole.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private ConsoleProgramConfig? _selectedProgram;

    public UiSettings Ui { get; }

    public ObservableCollection<ConsoleProgramConfig> Programs { get; }

    public IReadOnlyList<string> FontFamilies { get; }

    public IReadOnlyList<EncodingOption> EncodingOptions { get; }

    public ConsoleProgramConfig? SelectedProgram
    {
        get => _selectedProgram;
        set
        {
            if (SetProperty(ref _selectedProgram, value))
                (RemoveProgramCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public ICommand AddProgramCommand { get; }
    public ICommand RemoveProgramCommand { get; }

    public SettingsViewModel(AppSettings settings)
    {
        Ui = settings.Ui;

        Programs = new ObservableCollection<ConsoleProgramConfig>(
            settings.Programs.Select(p => new ConsoleProgramConfig
            {
                Name = p.Name,
                FilePath = p.FilePath,
                Arguments = p.Arguments,
                WorkingDirectory = p.WorkingDirectory,
                OutputEncoding = p.OutputEncoding,
                StartOrder = p.StartOrder,
                StartDelayMs = p.StartDelayMs,
            }));

        FontFamilies = Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        EncodingOptions = new List<EncodingOption>
        {
            new("auto", "自动（系统默认）"),
            new("gbk", "GBK（936）"),
            new("utf-8", "UTF-8（65001）"),
            new("utf-16", "UTF-16（1200）"),
        };

        AddProgramCommand = new RelayCommand(AddProgram);
        RemoveProgramCommand = new RelayCommand(RemoveSelected, () => SelectedProgram is not null);

        SelectedProgram = Programs.FirstOrDefault();
    }

    private void AddProgram()
    {
        var nextOrder = Programs.Count == 0 ? 1 : Programs.Max(p => p.StartOrder) + 1;
        var item = new ConsoleProgramConfig
        {
            Name = $"程序 {nextOrder}",
            StartOrder = nextOrder,
            StartDelayMs = 0,
            OutputEncoding = "auto",
        };

        Programs.Add(item);
        SelectedProgram = item;
    }

    private void RemoveSelected()
    {
        if (SelectedProgram is null)
            return;

        Programs.Remove(SelectedProgram);
        SelectedProgram = null;
    }

    public AppSettings BuildResultSettings()
    {
        var programs = Programs
            .OrderBy(p => p.StartOrder)
            .Select((p, index) => new ConsoleProgramConfig
            {
                Name = p.Name,
                FilePath = p.FilePath,
                Arguments = p.Arguments,
                WorkingDirectory = p.WorkingDirectory,
                OutputEncoding = string.IsNullOrWhiteSpace(p.OutputEncoding) ? "auto" : p.OutputEncoding.Trim(),
                StartOrder = index + 1,
                StartDelayMs = Math.Max(0, p.StartDelayMs),
            })
            .ToList();

        return new AppSettings
        {
            Version = 1,
            Ui = new UiSettings
            {
                OutputForeground = Ui.OutputForeground,
                OutputBackground = Ui.OutputBackground,
                OutputFontFamily = Ui.OutputFontFamily,
                OutputFontSize = Ui.OutputFontSize,
            },
            Programs = programs,
        };
    }
}
