using System.Diagnostics;
using System.IO;
using System.Text;
using LyoConsole.Models;
using LyoConsole.Utilities;

namespace LyoConsole.Services;

public sealed class ConsoleProcessHost : IDisposable
{
    private Process? _process;

    public ConsoleProgramConfig Config { get; }

    public bool IsRunning => _process is { HasExited: false };

    public event Action<string, bool>? OutputReceived;
    public event Action<int?>? Exited;

    public ConsoleProcessHost(ConsoleProgramConfig config)
    {
        Config = config;
    }

    public void Start()
    {
        if (IsRunning)
            return;

        if (string.IsNullOrWhiteSpace(Config.FilePath))
        {
            OutputReceived?.Invoke("启动失败：程序路径为空。", true);
            return;
        }

        var encoding = EncodingHelper.GetEncodingOrDefault(Config.OutputEncoding);

        var fileName = Config.FilePath;
        var arguments = Config.Arguments;
        if (IsCmdScript(fileName))
        {
            var comspec = Environment.GetEnvironmentVariable("ComSpec");
            fileName = string.IsNullOrWhiteSpace(comspec) ? "cmd.exe" : comspec;
            var argTail = string.IsNullOrWhiteSpace(arguments) ? "" : " " + arguments;
            arguments = $"/c \"\"{Config.FilePath}\"{argTail}\"";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = ResolveWorkingDirectory(),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = encoding,
            StandardErrorEncoding = encoding,
            StandardInputEncoding = encoding,
        };

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;
            OutputReceived?.Invoke(e.Data, false);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;
            OutputReceived?.Invoke(e.Data, true);
        };

        process.Exited += (_, _) =>
        {
            int? exitCode = null;
            try
            {
                exitCode = process.ExitCode;
            }
            catch
            {
                // ignored
            }

            Exited?.Invoke(exitCode);

            try
            {
                process.Dispose();
            }
            catch
            {
                // ignored
            }

            if (ReferenceEquals(_process, process))
                _process = null;
        };

        try
        {
            if (!process.Start())
            {
                OutputReceived?.Invoke("启动进程失败。", true);
                process.Dispose();
                return;
            }
        }
        catch (Exception ex)
        {
            OutputReceived?.Invoke($"启动失败：{ex.Message}", true);
            process.Dispose();
            return;
        }

        _process = process;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        OutputReceived?.Invoke($"[启动] {Config.FilePath} {Config.Arguments}".TrimEnd(), false);
    }

    public void Stop()
    {
        var process = _process;
        if (process is null)
            return;

        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // ignored
        }
    }

    public void SendLine(string line)
    {
        var process = _process;
        if (process is null || process.HasExited)
            return;

        try
        {
            process.StandardInput.WriteLine(line);
            process.StandardInput.Flush();
        }
        catch
        {
            // ignored
        }
    }

    private string ResolveWorkingDirectory()
    {
        if (!string.IsNullOrWhiteSpace(Config.WorkingDirectory))
            return Config.WorkingDirectory;

        try
        {
            var dir = Path.GetDirectoryName(Config.FilePath);
            if (!string.IsNullOrWhiteSpace(dir))
                return dir;
        }
        catch
        {
            // ignored
        }

        return Environment.CurrentDirectory;
    }

    private static bool IsCmdScript(string filePath)
    {
        try
        {
            var ext = Path.GetExtension(filePath);
            return ext.Equals(".bat", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".cmd", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        try
        {
            _process?.Dispose();
        }
        catch
        {
            // ignored
        }
    }
}
