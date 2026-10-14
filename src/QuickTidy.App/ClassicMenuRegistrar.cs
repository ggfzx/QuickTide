using Microsoft.Win32;
using QuickTidy.Core;

namespace QuickTidy.App;

/// <summary>
/// 经典右键菜单（Win10 全部 / Win11「显示更多选项」）注册表注册：
///   HKCU\Software\Classes\*\shell\QuickTidy\shell\<id>\command
///   HKCU\Software\Classes\Directory\shell\QuickTidy\shell\<id>\command
/// 每次保存配置后整体重建，保证菜单与配置一致。
/// </summary>
internal static class ClassicMenuRegistrar
{
    private static readonly string[] Roots = { "*", "Directory" };
    private const string MenuKey = "QuickTidy";

    public static string ExePath => Application.ExecutablePath;

    private static string BaseKeyName(string root) => $@"Software\Classes\{root}\shell\{MenuKey}";

    public static bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(BaseKeyName("*"));
        return key is not null;
    }

    /// <summary>整体重建经典菜单（先删后建）。菜单文字跟随系统语言。</summary>
    public static void Register(IReadOnlyList<PathEntry> entries)
    {
        Unregister();
        foreach (var root in Roots)
        {
            using var menu = Registry.CurrentUser.CreateSubKey(BaseKeyName(root));
            menu.SetValue("MUIVerb", L.T("速理 QuickTidy", "QuickTidy"));
            menu.SetValue("SubCommands", ""); // 空串 = 由 shell 子键生成级联菜单
            menu.SetValue("Icon", $"\"{ExePath}\"");

            using var children = menu.CreateSubKey("shell");
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.Path))
                    continue;
                using var verb = children.CreateSubKey(entry.Id);
                verb.SetValue(null, string.IsNullOrWhiteSpace(entry.Name) ? entry.Path : entry.Name);
                using var command = verb.CreateSubKey("command");
                command.SetValue(null, $"\"{ExePath}\" /move \"%V\" {entry.Id}");
            }

            // 固定「设置」入口（键名 zz- 开头保证排在最后；无路径时菜单也只剩这一项，可直接打开设置）
            using var settingsVerb = children.CreateSubKey("zz-quicktidy-settings");
            settingsVerb.SetValue(null, L.T("设置 QuickTidy…", "QuickTidy Settings…"));
            using var settingsCommand = settingsVerb.CreateSubKey("command");
            settingsCommand.SetValue(null, $"\"{ExePath}\"");
        }
    }

    public static void Unregister()
    {
        foreach (var root in Roots)
        {
            Registry.CurrentUser.DeleteSubKeyTree(BaseKeyName(root), throwOnMissingSubKey: false);
        }
    }
}
