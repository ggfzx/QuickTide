using System.Diagnostics;
using System.Runtime.InteropServices;
using QuickTidy.Core;

namespace QuickTidy.MenuHost;

// ---------------------------------------------------------------------------
// 菜单命令实现：
//   RootCommand      「速理 / QuickTidy」一级菜单（含子命令）
//   PathCommand      每条常用路径一个子菜单项，点击即移动
//   SettingsCommand  「设置 QuickTidy…」子菜单项
//   CommandEnumerator 子命令枚举器
// 菜单文字在每次右键构建时实时求值，自动跟随系统语言（中文系统中文，其他英文），
// 也可通过配置 "uiLanguage": "zh"/"en" 强制指定。
// ---------------------------------------------------------------------------

public abstract class CommandBase : IExplorerCommand
{
    public abstract void GetTitle(IShellItemArray? psiItemArray, out string ppszName);

    public virtual void GetIcon(IShellItemArray? psiItemArray, out string ppszIcon)
    {
        // 指向安装目录里的 qt.ico；不存在则返回空串（菜单仍显示，无图标）
        string icon = Path.Combine(AppContext.BaseDirectory, "qt.ico");
        ppszIcon = File.Exists(icon) ? icon : "";
    }

    public virtual void GetToolTip(IShellItemArray? psiItemArray, out string ppszInfotip) => ppszInfotip = "";

    public void GetCanonicalName(out Guid pguidCommandName) => pguidCommandName = Guid.Empty;

    public virtual void GetState(IShellItemArray? psiItemArray, bool fOkToBeSlow, out uint pCmdState)
        => pCmdState = ShellConsts.ECS_ENABLED;

    public abstract void Invoke(IShellItemArray? psiItemArray, IntPtr pbc);

    public virtual void GetFlags(out uint pFlags) => pFlags = ShellConsts.ECF_DEFAULT;

    public virtual void EnumSubCommands(out IEnumExplorerCommand? ppEnum) => ppEnum = null;
}

/// <summary>「速理」一级菜单。</summary>
public sealed class RootCommand : CommandBase
{
    public override void GetTitle(IShellItemArray? psiItemArray, out string ppszName)
        => ppszName = L.T("速理", "QuickTidy");

    public override void GetToolTip(IShellItemArray? psiItemArray, out string ppszInfotip)
        => ppszInfotip = L.T("把选中的文件/文件夹快速移动到常用目录",
            "Quickly move the selected items to your target folders");

    public override void GetFlags(out uint pFlags) => pFlags = ShellConsts.ECF_HASSUBCOMMANDS;

    public override void EnumSubCommands(out IEnumExplorerCommand? ppEnum)
    {
        var config = ConfigStoreMenu.Load();
        var items = new List<IExplorerCommand>();
        foreach (var entry in config.Paths)
            items.Add(new PathCommand(entry));
        items.Add(new SettingsCommand());
        ppEnum = new CommandEnumerator(items);
    }

    public override void Invoke(IShellItemArray? psiItemArray, IntPtr pbc) => MenuUi.OpenSettings();
}

/// <summary>一条常用路径对应的子菜单项。</summary>
public sealed class PathCommand : CommandBase
{
    private readonly PathEntry _entry;

    public PathCommand(PathEntry entry) => _entry = entry;

    public override void GetTitle(IShellItemArray? psiItemArray, out string ppszName)
        => ppszName = string.IsNullOrWhiteSpace(_entry.Name) ? _entry.Path : _entry.Name;

    public override void GetToolTip(IShellItemArray? psiItemArray, out string ppszInfotip)
        => ppszInfotip = L.T($"移动到 {_entry.Path}", $"Move to {_entry.Path}")
            + (_entry.UseDateFolder ? L.T("（今天日期子文件夹）", " (today's dated subfolder)") : "");

    public override void Invoke(IShellItemArray? psiItemArray, IntPtr pbc)
    {
        if (psiItemArray is null) return;

        var config = ConfigStoreMenu.Load();
        string policy = config.ConflictPolicy;

        psiItemArray.GetCount(out uint count);
        for (uint i = 0; i < count; i++)
        {
            try
            {
                psiItemArray.GetItemAt(i, out IShellItem? item);
                if (item is null) continue;
                item.GetDisplayName(ShellConsts.SIGDN_FILESYSPATH, out string? path);
                if (string.IsNullOrEmpty(path)) continue;
                MoveEngine.MoveIntoTarget(path, _entry, policy, MenuUi.AskConflict, MenuUi.ShowError);
            }
            catch
            {
                // 单个项目失败不影响其余项目
            }
        }
    }
}

/// <summary>「设置 QuickTidy…」子菜单项。</summary>
public sealed class SettingsCommand : CommandBase
{
    public override void GetTitle(IShellItemArray? psiItemArray, out string ppszName)
        => ppszName = L.T("设置 QuickTidy…", "QuickTidy Settings…");

    public override void GetToolTip(IShellItemArray? psiItemArray, out string ppszInfotip)
        => ppszInfotip = L.T("管理常用路径与短名称", "Manage target folders and short names");

    public override void Invoke(IShellItemArray? psiItemArray, IntPtr pbc) => MenuUi.OpenSettings();
}

/// <summary>子命令枚举器。</summary>
public sealed unsafe class CommandEnumerator : IEnumExplorerCommand
{
    private static readonly Guid IidIExplorerCommand = new("A08CE4D0-FA25-44AB-B57C-C7B1C323E0B9");

    private readonly List<IExplorerCommand> _items;
    private uint _pos;

    public CommandEnumerator(List<IExplorerCommand> items) => _items = items;

    public int Next(uint celt, IntPtr* pUICommand, uint* pceltFetched)
    {
        uint fetched = 0;
        if (pUICommand is not null)
        {
            while (fetched < celt && _pos < (uint)_items.Count)
            {
                pUICommand[fetched] = GetInterfacePointer(_items[(int)_pos]);
                fetched++;
                _pos++;
            }
        }
        if (pceltFetched is not null) *pceltFetched = fetched;
        return fetched < celt ? 1 /* S_FALSE */ : 0 /* S_OK */;
    }

    /// <summary>把托管命令对象变成 IExplorerCommand* 接口指针。</summary>
    private static IntPtr GetInterfacePointer(IExplorerCommand item)
    {
        IntPtr unk = Marshal.GetIUnknownForObject(item);
        try
        {
            Guid iid = IidIExplorerCommand;
            int hr = Marshal.QueryInterface(unk, ref iid, out IntPtr pp);
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);
            return pp;
        }
        finally
        {
            Marshal.Release(unk);
        }
    }

    public void Skip(uint celt) => _pos = Math.Min(_pos + celt, (uint)_items.Count);

    public void Reset() => _pos = 0;

    public void Clone(out IEnumExplorerCommand? ppenum)
    {
        ppenum = new CommandEnumerator(_items.ToList()) { _pos = _pos };
    }
}

/// <summary>宿主侧 UI（MessageBoxW）。</summary>
internal static class MenuUi
{
    private static string Caption => L.T("速理 QuickTidy", "QuickTidy");

    public static int AskConflict(string message)
        => NativeMethods.MessageBoxW(IntPtr.Zero, message, Caption,
            ShellConsts.MB_YESNOCANCEL | ShellConsts.MB_ICONWARNING);

    public static void ShowError(string message)
        => NativeMethods.MessageBoxW(IntPtr.Zero, message, Caption, ShellConsts.MB_ICONERROR);

    public static void OpenSettings()
    {
        string exe = Path.Combine(AppContext.BaseDirectory, "QuickTidy.exe");
        if (File.Exists(exe))
        {
            Process.Start(new ProcessStartInfo { FileName = exe, UseShellExecute = true });
        }
        else
        {
            ShowError(L.T($"未找到设置程序：\n{exe}", $"Settings app not found:\n{exe}"));
        }
    }
}
