using System.Reflection;
using System.Runtime.InteropServices;

namespace Timekeeping.Agent.Services;

/// <summary>
/// Late-bound wrapper around zkemkeeper COM so the agent builds with
/// <c>dotnet build</c> (no ResolveComReference / MSBuild Framework required).
/// Requires zkemkeeper.dll registered (see SDK/Register_SDK.bat).
/// </summary>
internal sealed class ZkComDevice : IDisposable
{
    private object? _zk;
    private bool _disposed;

    public static bool TryCreate(out ZkComDevice? device, out string? error)
    {
        device = null;
        error = null;

        var type = Type.GetTypeFromProgID("zkemkeeper.ZKEM")
                   ?? Type.GetTypeFromProgID("zkemkeeper.ZKEM.1");

        if (type is null)
        {
            error = "zkemkeeper COM is not registered. Run SDK/Register_SDK.bat as Administrator.";
            return false;
        }

        try
        {
            var instance = Activator.CreateInstance(type);
            if (instance is null)
            {
                error = "Failed to create zkemkeeper.ZKEM instance.";
                return false;
            }

            device = new ZkComDevice(instance);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private ZkComDevice(object zk) => _zk = zk;

    public bool SetCommPassword(int password)
        => InvokeBool(nameof(SetCommPassword), password);

    public bool ConnectNet(string ip, int port)
        => InvokeBool("Connect_Net", ip, port);

    public void Disconnect()
        => Invoke("Disconnect");

    public bool EnableDevice(int machineNumber, bool enable)
        => InvokeBool(nameof(EnableDevice), machineNumber, enable);

    public bool ReadGeneralLogData(int machineNumber)
        => InvokeBool(nameof(ReadGeneralLogData), machineNumber);

    public bool GetSerialNumber(int machineNumber, out string serial)
    {
        serial = "";
        if (_zk is null) return false;

        object[] args = [machineNumber, serial];
        var paramMods = new ParameterModifier(2);
        paramMods[1] = true;
        var mods = new[] { paramMods };

        try
        {
            var result = _zk.GetType().InvokeMember(
                "GetSerialNumber",
                BindingFlags.InvokeMethod,
                null,
                _zk,
                args,
                mods,
                null,
                null);

            serial = args[1] as string ?? "";
            return result is true || result is int i && i != 0;
        }
        catch
        {
            return false;
        }
    }

    public bool SetDeviceTime2(int machineNumber, int y, int m, int d, int h, int min, int s)
        => InvokeBool(nameof(SetDeviceTime2), machineNumber, y, m, d, h, min, s);

    public void GetLastError(ref int errorCode)
    {
        if (_zk is null) return;
        object[] args = [errorCode];
        var paramMods = new ParameterModifier(1);
        paramMods[0] = true;
        try
        {
            _zk.GetType().InvokeMember(
                "GetLastError",
                BindingFlags.InvokeMethod,
                null,
                _zk,
                args,
                [paramMods],
                null,
                null);
            errorCode = args[0] is int i ? i : 0;
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// SSR_GetGeneralLogData(machine, out enroll, out verify, out inout, out y,m,d,h,min,s, ref workCode)
    /// </summary>
    public bool TryReadNextLog(
        int machineNumber,
        out string enrollNumber,
        out int verifyMode,
        out int inOutMode,
        out int year,
        out int month,
        out int day,
        out int hour,
        out int minute,
        out int second,
        ref int workCode)
    {
        enrollNumber = "";
        verifyMode = inOutMode = year = month = day = hour = minute = second = 0;
        if (_zk is null) return false;

        object[] args =
        [
            machineNumber,
            enrollNumber,
            verifyMode,
            inOutMode,
            year,
            month,
            day,
            hour,
            minute,
            second,
            workCode
        ];

        var paramMods = new ParameterModifier(11);
        for (var i = 1; i <= 10; i++)
            paramMods[i] = true;

        try
        {
            var result = _zk.GetType().InvokeMember(
                "SSR_GetGeneralLogData",
                BindingFlags.InvokeMethod,
                null,
                _zk,
                args,
                [paramMods],
                null,
                null);

            enrollNumber = Convert.ToString(args[1]) ?? "";
            verifyMode = Convert.ToInt32(args[2]);
            inOutMode = Convert.ToInt32(args[3]);
            year = Convert.ToInt32(args[4]);
            month = Convert.ToInt32(args[5]);
            day = Convert.ToInt32(args[6]);
            hour = Convert.ToInt32(args[7]);
            minute = Convert.ToInt32(args[8]);
            second = Convert.ToInt32(args[9]);
            workCode = Convert.ToInt32(args[10]);

            return result is true || (result is int ri && ri != 0);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is COMException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool InvokeBool(string name, params object[] args)
    {
        var result = Invoke(name, args);
        return result is true || result is int i && i != 0;
    }

    private object? Invoke(string name, params object[] args)
    {
        if (_zk is null) return null;
        return _zk.GetType().InvokeMember(
            name,
            BindingFlags.InvokeMethod,
            null,
            _zk,
            args);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { Disconnect(); } catch { /* ignore */ }
        if (_zk is not null)
        {
            try { Marshal.FinalReleaseComObject(_zk); } catch { /* ignore */ }
            _zk = null;
        }
    }
}
