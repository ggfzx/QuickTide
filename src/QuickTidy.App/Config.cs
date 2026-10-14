using System.Text.Json;
using QuickTidy.Core;

namespace QuickTidy.App;

/// <summary>配置读写（%APPDATA%\QuickTidy\config.json）。</summary>
internal static class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true, // 读取时兼容大小写
    };

    public static QuickTidyConfig Load()
    {
        try
        {
            string path = QuickTidy.Core.ConfigStore.ConfigPath;
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<QuickTidyConfig>(File.ReadAllText(path), JsonOptions)
                       ?? new QuickTidyConfig();
            }
        }
        catch
        {
            // 配置损坏时按空配置处理
        }
        return new QuickTidyConfig();
    }

    public static void Save(QuickTidyConfig config)
    {
        QuickTidy.Core.ConfigStore.EnsureDir();
        File.WriteAllText(QuickTidy.Core.ConfigStore.ConfigPath,
            JsonSerializer.Serialize(config, JsonOptions));
    }
}
