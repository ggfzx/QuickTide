namespace QuickTidy.Core;

/// <summary>
/// 配置文件固定位置： %APPDATA%\QuickTidy\config.json
/// 序列化/反序列化由各宿主工程自行实现（App 用反射版 JSON，菜单 DLL 用 AOT 源生成版 JSON）。
/// </summary>
public static class ConfigStore
{
    public static string ConfigDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickTidy");

    public static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    /// <summary>确保配置目录存在。</summary>
    public static void EnsureDir() => Directory.CreateDirectory(ConfigDir);
}
