using System.Runtime.InteropServices;

namespace QuickTidy.MenuHost;

// ---------------------------------------------------------------------------
// Shell COM 接口（经典 ComImport 互操作，vtable slot 顺序来自 shobjidl_core.idl，勿改动）
// 系统已为这些接口注册了跨进程代理（HKCR\Interface\...\ProxyStubClsid32 = {95E15D0A-...}），
// 因此本进程外服务器与 explorer.exe 之间可以正常封送。
// ---------------------------------------------------------------------------

/// <summary>标准 COM 类厂。</summary>
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00000001-0000-0000-C000-000000000046")]
public interface IClassFactory
{
    void CreateInstance([MarshalAs(UnmanagedType.IUnknown)] object? pUnkOuter, ref Guid riid,
        out IntPtr ppvObject);

    void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
}

/// <summary>IExplorerCommand {A08CE4D0-FA25-44AB-B57C-C7B1C323E0B9}</summary>
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("A08CE4D0-FA25-44AB-B57C-C7B1C323E0B9")]
public interface IExplorerCommand
{
    void GetTitle([MarshalAs(UnmanagedType.Interface)] IShellItemArray? psiItemArray,
        [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

    void GetIcon([MarshalAs(UnmanagedType.Interface)] IShellItemArray? psiItemArray,
        [MarshalAs(UnmanagedType.LPWStr)] out string ppszIcon);

    void GetToolTip([MarshalAs(UnmanagedType.Interface)] IShellItemArray? psiItemArray,
        [MarshalAs(UnmanagedType.LPWStr)] out string ppszInfotip);

    void GetCanonicalName(out Guid pguidCommandName);

    void GetState([MarshalAs(UnmanagedType.Interface)] IShellItemArray? psiItemArray,
        [MarshalAs(UnmanagedType.Bool)] bool fOkToBeSlow, out uint pCmdState);

    void Invoke([MarshalAs(UnmanagedType.Interface)] IShellItemArray? psiItemArray, IntPtr pbc);

    void GetFlags(out uint pFlags);

    void EnumSubCommands([MarshalAs(UnmanagedType.Interface)] out IEnumExplorerCommand? ppEnum);
}

/// <summary>IEnumExplorerCommand {A88826F8-186F-4987-AADE-EA0CEF8FBFE8}</summary>
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("A88826F8-186F-4987-AADE-EA0CEF8FBFE8")]
public unsafe interface IEnumExplorerCommand
{
    [PreserveSig]
    int Next(uint celt, IntPtr* pUICommand, uint* pceltFetched);

    void Skip(uint celt);

    void Reset();

    void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumExplorerCommand? ppenum);
}

/// <summary>
/// IShellItemArray {B63EA76D-1F85-456F-A19C-48159EFA858B}
/// 只使用 GetCount / GetItemAt（slot 顺序必须与 SDK 一致，未用方法用 IntPtr 占位）。
/// </summary>
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
public interface IShellItemArray
{
    void BindToHandler(IntPtr pbc, IntPtr bhid, IntPtr riid, IntPtr ppvOut);

    void GetPropertyStore(IntPtr flags, IntPtr riid, IntPtr ppv);

    void GetPropertyDescriptionList(IntPtr keyType, IntPtr riid, IntPtr ppv);

    void GetAttributes(uint attribFlags, uint sfgaoMask, out uint psfgaoAttribs);

    void GetCount(out uint pdwNumItems);

    void GetItemAt(uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out IShellItem? ppsi);

    void EnumItems(out IntPtr ppenumShellItems);
}

/// <summary>IShellItem {43826D1E-E718-42EE-BC55-A1E261C37BFE}，只用 GetDisplayName。</summary>
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
public interface IShellItem
{
    void BindToHandler(IntPtr pbc, IntPtr bhid, IntPtr riid, IntPtr ppvOut);

    void GetParent(IntPtr ppsi);

    void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

    void GetAttributes(uint sfgaoMask, IntPtr psfgaoAttribs);

    void Compare(IntPtr psiOther, uint hint, IntPtr order);
}

// ---------------------------------------------------------------------------
// 常量
// ---------------------------------------------------------------------------

internal static class ShellConsts
{
    public const uint ECS_ENABLED = 0x000;
    public const uint ECS_DISABLED = 0x001;
    public const uint ECS_HIDDEN = 0x002;

    public const uint ECF_DEFAULT = 0x000;
    public const uint ECF_HASSUBCOMMANDS = 0x001;

    public const uint SIGDN_FILESYSPATH = 0x80058000;

    public const uint MB_YESNOCANCEL = 0x3;
    public const uint MB_ICONWARNING = 0x30;
    public const uint MB_ICONERROR = 0x10;

    public const int IDYES = 6;
    public const int IDNO = 7;
    public const int IDCANCEL = 2;
}

internal static class NativeMethods
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("ole32.dll")]
    public static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [DllImport("ole32.dll")]
    public static extern int CoRegisterClassObject(ref Guid rclsid,
        [MarshalAs(UnmanagedType.IUnknown)] object pUnk, uint dwClsContext, uint flags, out uint lpdwRegister);

    [DllImport("ole32.dll")]
    public static extern int CoRevokeClassObject(uint dwRegister);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    public static extern int GetMessageW(out Msg msg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    public static extern bool TranslateMessage(ref Msg msg);

    [DllImport("user32.dll")]
    public static extern IntPtr DispatchMessageW(ref Msg msg);

    [DllImport("user32.dll")]
    public static extern bool PostThreadMessageW(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct Msg
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    public const uint CLSCTX_LOCAL_SERVER = 4;
    public const uint REGCLS_MULTIPLEUSE = 1;
    public const uint COINIT_APARTMENTTHREADED = 2;
    public const uint WM_QUIT = 0x12;
}
