using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using LyoConsole.Models;

namespace LyoConsole.Services;

public sealed class PortUsageService
{
    private const uint NO_ERROR = 0;
    private const uint ERROR_INSUFFICIENT_BUFFER = 122;

    private const int AF_INET = 2;
    private const int AF_INET6 = 23;

    public IReadOnlyList<PortUsageEntry> GetAllPortUsages()
    {
        var entries = new List<(int Port, string Protocol, int Pid)>();

        entries.AddRange(GetTcpListeners(AF_INET).Select(p => (p.Port, "TCP", p.Pid)));
        entries.AddRange(GetTcpListeners(AF_INET6).Select(p => (p.Port, "TCP", p.Pid)));
        entries.AddRange(GetUdpListeners(AF_INET).Select(p => (p.Port, "UDP", p.Pid)));
        entries.AddRange(GetUdpListeners(AF_INET6).Select(p => (p.Port, "UDP", p.Pid)));

        var unique = new HashSet<(int Port, string Protocol, int Pid)>();
        var processNameCache = new Dictionary<int, string>();
        var result = new List<PortUsageEntry>();

        foreach (var item in entries)
        {
            if (item.Port <= 0)
                continue;

            if (!unique.Add(item))
                continue;

            var name = GetProcessNameSafe(item.Pid, processNameCache);
            result.Add(new PortUsageEntry(item.Port, item.Protocol, item.Pid, name));
        }

        result.Sort(static (a, b) =>
        {
            var portCompare = a.Port.CompareTo(b.Port);
            if (portCompare != 0)
                return portCompare;

            var protoCompare = string.CompareOrdinal(a.Protocol, b.Protocol);
            if (protoCompare != 0)
                return protoCompare;

            return a.Pid.CompareTo(b.Pid);
        });

        return result;
    }

    private static string GetProcessNameSafe(int pid, Dictionary<int, string> cache)
    {
        if (pid <= 0)
            return "System";

        if (cache.TryGetValue(pid, out var name))
            return name;

        try
        {
            name = Process.GetProcessById(pid).ProcessName;
        }
        catch
        {
            name = "<未知>";
        }

        cache[pid] = name;
        return name;
    }

    private static IReadOnlyList<(int Port, int Pid)> GetTcpListeners(int addressFamily)
    {
        var tableClass = TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_LISTENER;
        var rows = addressFamily == AF_INET6 ? ReadTcp6Table(tableClass) : ReadTcp4Table(tableClass);

        var result = new List<(int Port, int Pid)>(rows.Count);
        foreach (var row in rows)
        {
            var port = ConvertPort(row.LocalPort);
            if (port == 0)
                continue;

            result.Add((port, row.OwningPid));
        }

        return result;
    }

    private static IReadOnlyList<(int Port, int Pid)> GetUdpListeners(int addressFamily)
    {
        var tableClass = UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID;
        var rows = addressFamily == AF_INET6 ? ReadUdp6Table(tableClass) : ReadUdp4Table(tableClass);

        var result = new List<(int Port, int Pid)>(rows.Count);
        foreach (var row in rows)
        {
            var port = ConvertPort(row.LocalPort);
            if (port == 0)
                continue;

            result.Add((port, row.OwningPid));
        }

        return result;
    }

    private static int ConvertPort(uint port)
        => (ushort)IPAddress.NetworkToHostOrder(unchecked((short)port));

    private static List<TcpRow> ReadTcp4Table(TCP_TABLE_CLASS tableClass)
    {
        try
        {
        var bufferSize = 0;
        var result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET, tableClass, 0);
        if (result != ERROR_INSUFFICIENT_BUFFER)
            return [];

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = GetExtendedTcpTable(buffer, ref bufferSize, true, AF_INET, tableClass, 0);
            if (result != NO_ERROR)
                return [];

            var count = Marshal.ReadInt32(buffer);
            var rowPtr = IntPtr.Add(buffer, 4);
            var rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

            var rows = new List<TcpRow>(count);
            for (var i = 0; i < count; i++)
            {
                var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);
                rows.Add(new TcpRow(row.dwLocalPort, unchecked((int)row.dwOwningPid)));
                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }

            return rows;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
        }
        catch
        {
            return [];
        }
    }

    private static List<TcpRow> ReadTcp6Table(TCP_TABLE_CLASS tableClass)
    {
        try
        {
        var bufferSize = 0;
        var result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET6, tableClass, 0);
        if (result != ERROR_INSUFFICIENT_BUFFER)
            return [];

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = GetExtendedTcpTable(buffer, ref bufferSize, true, AF_INET6, tableClass, 0);
            if (result != NO_ERROR)
                return [];

            var count = Marshal.ReadInt32(buffer);
            var rowPtr = IntPtr.Add(buffer, 4);
            var rowSize = Marshal.SizeOf<MIB_TCP6ROW_OWNER_PID>();

            var rows = new List<TcpRow>(count);
            for (var i = 0; i < count; i++)
            {
                var row = Marshal.PtrToStructure<MIB_TCP6ROW_OWNER_PID>(rowPtr);
                rows.Add(new TcpRow(row.dwLocalPort, unchecked((int)row.dwOwningPid)));
                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }

            return rows;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
        }
        catch
        {
            return [];
        }
    }

    private static List<UdpRow> ReadUdp4Table(UDP_TABLE_CLASS tableClass)
    {
        try
        {
        var bufferSize = 0;
        var result = GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true, AF_INET, tableClass, 0);
        if (result != ERROR_INSUFFICIENT_BUFFER)
            return [];

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = GetExtendedUdpTable(buffer, ref bufferSize, true, AF_INET, tableClass, 0);
            if (result != NO_ERROR)
                return [];

            var count = Marshal.ReadInt32(buffer);
            var rowPtr = IntPtr.Add(buffer, 4);
            var rowSize = Marshal.SizeOf<MIB_UDPROW_OWNER_PID>();

            var rows = new List<UdpRow>(count);
            for (var i = 0; i < count; i++)
            {
                var row = Marshal.PtrToStructure<MIB_UDPROW_OWNER_PID>(rowPtr);
                rows.Add(new UdpRow(row.dwLocalPort, unchecked((int)row.dwOwningPid)));
                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }

            return rows;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
        }
        catch
        {
            return [];
        }
    }

    private static List<UdpRow> ReadUdp6Table(UDP_TABLE_CLASS tableClass)
    {
        try
        {
        var bufferSize = 0;
        var result = GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true, AF_INET6, tableClass, 0);
        if (result != ERROR_INSUFFICIENT_BUFFER)
            return [];

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = GetExtendedUdpTable(buffer, ref bufferSize, true, AF_INET6, tableClass, 0);
            if (result != NO_ERROR)
                return [];

            var count = Marshal.ReadInt32(buffer);
            var rowPtr = IntPtr.Add(buffer, 4);
            var rowSize = Marshal.SizeOf<MIB_UDP6ROW_OWNER_PID>();

            var rows = new List<UdpRow>(count);
            for (var i = 0; i < count; i++)
            {
                var row = Marshal.PtrToStructure<MIB_UDP6ROW_OWNER_PID>(rowPtr);
                rows.Add(new UdpRow(row.dwLocalPort, unchecked((int)row.dwOwningPid)));
                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }

            return rows;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
        }
        catch
        {
            return [];
        }
    }

    private readonly record struct TcpRow(uint LocalPort, int OwningPid);
    private readonly record struct UdpRow(uint LocalPort, int OwningPid);

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCPROW_OWNER_PID
    {
        public uint dwState;
        public uint dwLocalAddr;
        public uint dwLocalPort;
        public uint dwRemoteAddr;
        public uint dwRemotePort;
        public uint dwOwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCP6ROW_OWNER_PID
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ucLocalAddr;

        public uint dwLocalScopeId;
        public uint dwLocalPort;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ucRemoteAddr;

        public uint dwRemoteScopeId;
        public uint dwRemotePort;
        public uint dwState;
        public uint dwOwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_UDPROW_OWNER_PID
    {
        public uint dwLocalAddr;
        public uint dwLocalPort;
        public uint dwOwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_UDP6ROW_OWNER_PID
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ucLocalAddr;

        public uint dwLocalScopeId;
        public uint dwLocalPort;
        public uint dwOwningPid;
    }

    private enum TCP_TABLE_CLASS
    {
        TCP_TABLE_BASIC_LISTENER = 0,
        TCP_TABLE_BASIC_CONNECTIONS = 1,
        TCP_TABLE_BASIC_ALL = 2,
        TCP_TABLE_OWNER_PID_LISTENER = 3,
        TCP_TABLE_OWNER_PID_CONNECTIONS = 4,
        TCP_TABLE_OWNER_PID_ALL = 5,
        TCP_TABLE_OWNER_MODULE_LISTENER = 6,
        TCP_TABLE_OWNER_MODULE_CONNECTIONS = 7,
        TCP_TABLE_OWNER_MODULE_ALL = 8
    }

    private enum UDP_TABLE_CLASS
    {
        UDP_TABLE_BASIC = 0,
        UDP_TABLE_OWNER_PID = 1,
        UDP_TABLE_OWNER_MODULE = 2
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int dwOutBufLen,
        bool sort,
        int ipVersion,
        TCP_TABLE_CLASS tblClass,
        uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedUdpTable(
        IntPtr pUdpTable,
        ref int dwOutBufLen,
        bool sort,
        int ipVersion,
        UDP_TABLE_CLASS tblClass,
        uint reserved);
}
