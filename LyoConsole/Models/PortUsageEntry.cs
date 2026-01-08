namespace LyoConsole.Models;

public sealed record PortUsageEntry(int Port, string Protocol, int Pid, string ProcessName);

