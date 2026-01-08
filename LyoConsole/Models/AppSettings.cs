namespace LyoConsole.Models;

public sealed class AppSettings
{
    public int Version { get; set; } = 1;

    public UiSettings Ui { get; set; } = new();

    public List<ConsoleProgramConfig> Programs { get; set; } = [];
}
