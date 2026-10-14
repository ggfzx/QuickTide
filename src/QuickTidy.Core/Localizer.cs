using System.Globalization;

namespace QuickTidy.Core;

/// <summary>
/// 轻量本地化：跟随系统 UI 语言（中文系统显示中文，其他语言一律英文）。
/// 可通过配置文件 "uiLanguage": "auto" | "zh" | "en" 强制指定。
/// </summary>
public static class L
{
    private static string _override = "auto";

    /// <summary>语言覆盖："auto"（跟随系统）/"zh"/"en"。由宿主启动时从配置写入。</summary>
    public static string Override
    {
        get => _override;
        set => _override = string.IsNullOrWhiteSpace(value) ? "auto" : value.Trim().ToLowerInvariant();
    }

    public static bool IsChinese
    {
        get
        {
            if (string.Equals(_override, "zh", StringComparison.Ordinal)) return true;
            if (string.Equals(_override, "en", StringComparison.Ordinal)) return false;
            // auto：跟随系统 UI 语言
            try
            {
                return string.Equals(
                    CultureInfo.CurrentUICulture?.TwoLetterISOLanguageName ?? "en",
                    "zh", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>取词：中文系统返回 zh，否则返回 en。</summary>
    public static string T(string zh, string en) => IsChinese ? zh : en;
}
