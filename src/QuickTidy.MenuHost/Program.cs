using System.Runtime.InteropServices;

namespace QuickTidy.MenuHost;

/// <summary>
/// 进程外 COM 本地服务器入口：
///   1. 防止多实例（SCM 会复用已注册的类厂）
///   2. STA 初始化 + CoRegisterClassObject 注册类厂
///   3. 消息循环等待 explorer 的调用，空闲 60 秒自动退出
/// </summary>
internal static class Program
{
    public static readonly Guid ClsidQuickTidy = new("7E42F3C9-2A64-4B7D-9E81-C50D3A8F6B24");
    private static Guid ClsidQuickTidyRef = ClsidQuickTidy;
    private const string InstanceMutexName = @"Local\QuickTidy.MenuHost.Instance";

    [STAThread]
    private static int Main()
    {
        Log($"Main enter pid={Environment.ProcessId} user={Environment.UserName}");
        try
        {
            using var mutex = new Mutex(true, InstanceMutexName, out bool createdNew);
            if (!createdNew)
            {
                Log("mutex held by another instance, exit");
                return 0;
            }

            int hrInit = NativeMethods.CoInitializeEx(IntPtr.Zero, NativeMethods.COINIT_APARTMENTTHREADED);
            Log($"CoInitializeEx hr=0x{hrInit:X8}");
            if (hrInit != 0 && hrInit != 1 /* S_FALSE：已初始化 */)
                return hrInit;

            var factory = new QuickTidyClassFactory();

            // 自检：本地 QI 应当成功，否则 CCW 配置有问题
            LogSelfQI(factory, new Guid("00000001-0000-0000-C000-000000000046"), "IClassFactory");
            var probe = new RootCommand();
            LogSelfQI(probe, new Guid("A08CE4D0-FA25-44AB-B57C-C7B1C323E0B9"), "IExplorerCommand");

            int hr = NativeMethods.CoRegisterClassObject(
                ref ClsidQuickTidyRef, factory,
                NativeMethods.CLSCTX_LOCAL_SERVER, NativeMethods.REGCLS_MULTIPLEUSE, out uint cookie);
            Log($"CoRegisterClassObject hr=0x{hr:X8} cookie={cookie}");
            if (hr != 0)
                return hr;

            uint threadId = NativeMethods.GetCurrentThreadId();

            // 空闲退出：60 秒没有新调用就结束进程（每次 CreateInstance 会重置计时器）
            var idleTimer = new System.Threading.Timer(
                _ => NativeMethods.PostThreadMessageW(threadId, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero),
                null, 60_000, 60_000);
            QuickTidyClassFactory.IdleTimer = idleTimer;

            while (NativeMethods.GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                // STA 的 COM 跨进程调用以窗口消息形式到达，必须分发才会被执行
                NativeMethods.TranslateMessage(ref msg);
                NativeMethods.DispatchMessageW(ref msg);
            }

            Log("message loop done, revoking");
            NativeMethods.CoRevokeClassObject(cookie);
            return 0;
        }
        catch (Exception ex)
        {
            Log($"FATAL {ex}");
            return unchecked((int)0x80004005);
        }
    }

    private static void LogSelfQI(object obj, Guid iid, string name)
    {
        IntPtr unk = Marshal.GetIUnknownForObject(obj);
        try
        {
            Guid g = iid;
            int hr = Marshal.QueryInterface(unk, ref g, out _);
            Log($"self QI {name} hr=0x{hr:X8}");
        }
        finally
        {
            Marshal.Release(unk);
        }
    }

    internal static void Log(string message)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(Path.GetTempPath(), "QuickTidy.MenuHost.log"),
                $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch { /* 日志失败不影响功能 */ }
    }
}

/// <summary>IClassFactory 实现：创建菜单命令对象并按请求的 IID 做 QI 后返回指针。</summary>
[ComVisible(true)]
public sealed class QuickTidyClassFactory : IClassFactory
{
    internal static System.Threading.Timer? IdleTimer;

    public unsafe void CreateInstance([MarshalAs(UnmanagedType.IUnknown)] object? pUnkOuter, ref Guid riid,
        out IntPtr ppvObject)
    {
        Program.Log($"CreateInstance riid={riid}");
        // 通知宿主：有新调用，重置空闲计时
        IdleTimer?.Change(60_000, 60_000);

        if (pUnkOuter is not null)
            throw new COMException("聚合不受支持。", unchecked((int)0x80040110));

        object command = new RootCommand();
        IntPtr unk = Marshal.GetIUnknownForObject(command);
        try
        {
            Guid iid = riid;
            int hr = Marshal.QueryInterface(unk, ref iid, out IntPtr pp);
            if (hr != 0)
                Marshal.ThrowExceptionForHR(hr); // E_NOINTERFACE 等
            ppvObject = pp;
        }
        finally
        {
            Marshal.Release(unk);
        }
    }

    public void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock)
    {
        // 无需实现：进程以空闲计时器管理生命周期
    }
}
