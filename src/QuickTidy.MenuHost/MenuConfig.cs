using System.Text.Json;
using QuickTidy.Core;

namespace QuickTidy.MenuHost;

/// <summary>读取 %APPDATA%\QuickTidy\config.json（与主程序共用同一份配置）。</summary>
internal static class ConfigStoreMenu
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static QuickTidyConfig Load()
    {
        try
        {
            string path = ConfigStore.ConfigPath;
            if (File.Exists(path))
            {
                var config = JsonSerializer.Deserialize<QuickTidyConfig>(File.ReadAllText(path), JsonOptions)
                             ?? new QuickTidyConfig();
                L.Override = config.UiLanguage;
                return config;
            }
        }
        catch
        {
            // 配置损坏时返回空配置，菜单仍可显示「设置」入口
        }
        L.Override = "auto";
        return new QuickTidyConfig();
    }
}
