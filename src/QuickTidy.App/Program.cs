using QuickTidy.Core;

namespace QuickTidy.App;

internal static class Program
{
    private static readonly string LogPath =
        Path.Combine(QuickTidy.Core.ConfigStore.ConfigDir, "QuickTidy.log");

    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        ApplyLanguageOverride();

        string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "";
        switch (mode)
        {
            case "/move" when args.Length >= 3:
                return ShellMove(args[1], args[2]);

            case "/setup": // 安装器静默调用：注册经典菜单，尝试 Win11 新式菜单
                return RunSetup();

            case "/cleanup": // 卸载器静默调用：反注册全部菜单
                return RunCleanup();

            default:
                Application.Run(new MainForm());
                return 0;
        }
    }

    /// <summary>读取配置中的 uiLanguage 设置（auto/zh/en），默认跟随系统。</summary>
    private static void ApplyLanguageOverride()
    {
        try
        {
            L.Override = ConfigStore.Load().UiLanguage;
        }
        catch
        {
            L.Override = "auto";
        }
    }

    // ------------------------------------------------------------------
    // /move "%V" <id> —— 经典菜单点击入口（每个选中项触发一次）
    // ------------------------------------------------------------------
    private static int ShellMove(string source, string id)
    {
        var config = ConfigStore.Load();
        L.Override = config.UiLanguage;
        var entry = config.Paths.FirstOrDefault(p => p.Id == id);
        if (entry is null)
        {
            MessageBox.Show(
                L.T($"配置中找不到路径标识 {id}，请打开 QuickTidy 重新保存配置。",
                    $"Path id \"{id}\" not found in config. Please re-save the config in QuickTidy."),
                Caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return 1;
        }

        MoveEngine.MoveIntoTarget(source, entry, config.ConflictPolicy, AskConflict, ShowError);
        return 0;
    }

    private static string Caption => L.T("速理 QuickTidy", "QuickTidy");

    private static int AskConflict(string message)
        => MessageBox.Show(message, Caption, MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning) switch
        {
            DialogResult.Yes => MoveEngine.IdYes,
            DialogResult.No => MoveEngine.IdNo,
            _ => MoveEngine.IdCancel,
        };

    private static void ShowError(string message)
        => MessageBox.Show(message, Caption, MessageBoxButtons.OK, MessageBoxIcon.Error);

    // ------------------------------------------------------------------
    // /setup 与 /cleanup（安装器/卸载器静默调用，只写日志不弹窗）
    // ------------------------------------------------------------------
    private static int RunSetup()
    {
        var lines = new List<string> { $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] /setup" };
        try
        {
            var config = ConfigStore.Load();
            ClassicMenuRegistrar.Register(config.Paths);
            lines.Add("classic menu registered");
        }
        catch (Exception ex)
        {
            lines.Add($"classic menu FAILED: {ex.Message}");
        }

        if (ModernMenuInstaller.IsWin11 && ModernMenuInstaller.IsManifestPresent())
        {
            try
            {
                var (ok, output) = ModernMenuInstaller.Register();
                lines.Add($"modern menu register ok={ok}");
                if (!ok) lines.Add(output);
            }
            catch (Exception ex)
            {
                lines.Add($"modern menu FAILED: {ex.Message}");
            }
        }
        else
        {
            lines.Add("modern menu skipped (not Win11 or files missing)");
        }

        WriteLog(lines);
        return 0;
    }

    private static int RunCleanup()
    {
        var lines = new List<string> { $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] /cleanup" };
        try
        {
            ClassicMenuRegistrar.Unregister();
            lines.Add("classic menu removed");
        }
        catch (Exception ex)
        {
            lines.Add($"classic menu remove FAILED: {ex.Message}");
        }

        // 无条件清理：LocalServer32 兜底注册 + sparse package（不存在时静默跳过）
        var (ok, output) = ModernMenuInstaller.Unregister();
        lines.Add($"modern menu remove ok={ok}");
        if (!ok && output.Length > 0) lines.Add(output);

        WriteLog(lines);
        return 0;
    }

    private static void WriteLog(List<string> lines)
    {
        try
        {
            QuickTidy.Core.ConfigStore.EnsureDir();
            File.AppendAllLines(LogPath, lines);
        }
        catch
        {
            // 日志失败不影响功能
        }
    }
}
