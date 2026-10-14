using System.Diagnostics;
using Microsoft.Win32;

namespace QuickTidy.App;

/// <summary>
/// Win11 新式右键菜单注册/移除：
///   1. HKCU CLSID LocalServer32（COM 进程外服务器兜底注册）
///   2. Sparse package（AppxManifest，提供包标识；需 Win11，未签名包需开发者模式或签名证书）
/// </summary>
internal static class ModernMenuInstaller
{
    public const string Clsid = "7E42F3C9-2A64-4B7D-9E81-C50D3A8F6B24";
    public const string PackageName = "QuickTidy";

    public static bool IsWin11 => Environment.OSVersion.Version.Build >= 22000;

    public static string InstallDir => AppContext.BaseDirectory;
    public static string ManifestPath => Path.Combine(InstallDir, "AppxManifest.xml");
    public static string HostExePath => Path.Combine(InstallDir, "QuickTidy.MenuHost.exe");

    private static string ClsidKeyName => $@"Software\Classes\CLSID\{{{Clsid}}}";

    public static bool IsManifestPresent() => File.Exists(ManifestPath) && File.Exists(HostExePath);

    public static bool IsPackageRegistered()
    {
        var (ok, output) = RunPowerShell(
            $"if (Get-AppxPackage -Name {PackageName}) {{ 'YES' }} else {{ 'NO' }}");
        return ok && output.Contains("YES", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>注册 LocalServer32 + sparse package。返回 (是否成功, 输出信息)。</summary>
    public static (bool Ok, string Output) Register()
    {
        RegisterLocalServer();
        if (!IsManifestPresent())
            return (false, $"缺少 {ManifestPath} 或 QuickTidy.MenuHost.exe（便携版需与主程序同目录放置这些文件）");
        return RunPowerShell(
            $"Add-AppxPackage -Register -Path '{ManifestPath}' -ExternalLocation '{InstallDir}'");
    }

    public static (bool Ok, string Output) Unregister()
    {
        var result = RunPowerShell(
            $"Get-AppxPackage -Name {PackageName} | Remove-AppxPackage");
        UnregisterLocalServer();
        return result;
    }

    private static void RegisterLocalServer()
    {
        using var key = Registry.CurrentUser.CreateSubKey(ClsidKeyName + @"\LocalServer32");
        key.SetValue(null, $"\"{HostExePath}\"");
    }

    private static void UnregisterLocalServer()
    {
        Registry.CurrentUser.DeleteSubKeyTree(ClsidKeyName, throwOnMissingSubKey: false);
    }

    private static (bool Ok, string Output) RunPowerShell(string script)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi);
            if (process is null) return (false, "无法启动 PowerShell。");
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(60_000);
            return (process.ExitCode == 0, (stdout + "\n" + stderr).Trim());
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
