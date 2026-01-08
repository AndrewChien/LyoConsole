using System.Globalization;
using System.Text;

namespace LyoConsole.Utilities;

public static class EncodingHelper
{
    public static Encoding GetEncodingOrDefault(string? setting)
    {
        if (string.IsNullOrWhiteSpace(setting))
            return GetDefaultConsoleEncoding();

        var trimmed = setting.Trim();
        if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            return GetDefaultConsoleEncoding();
        }

        TryRegisterCodePages();

        try
        {
            if (trimmed.Equals("oem", StringComparison.OrdinalIgnoreCase))
                return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);

            if (trimmed.Equals("ansi", StringComparison.OrdinalIgnoreCase))
                return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage);

            if (trimmed.Equals("gbk", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("cp936", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("936", StringComparison.OrdinalIgnoreCase))
            {
                return Encoding.GetEncoding(936);
            }

            if (trimmed.Equals("utf8", StringComparison.OrdinalIgnoreCase))
                return Encoding.UTF8;
        }
        catch
        {
            // ignored
        }

        if (int.TryParse(trimmed, out var codePage))
        {
            try
            {
                return Encoding.GetEncoding(codePage);
            }
            catch
            {
                // ignored
            }
        }

        try
        {
            return Encoding.GetEncoding(trimmed);
        }
        catch
        {
            return GetDefaultConsoleEncoding();
        }
    }

    public static Encoding GetDefaultConsoleEncoding()
    {
        TryRegisterCodePages();

        try
        {
            return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        }
        catch
        {
            // ignored
        }

        try
        {
            return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
        }
        catch
        {
            // ignored
        }

        return Encoding.UTF8;
    }

    private static void TryRegisterCodePages()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        catch
        {
            // ignored
        }
    }
}
