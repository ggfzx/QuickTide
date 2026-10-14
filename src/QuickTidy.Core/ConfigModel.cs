namespace QuickTidy.Core;

/// <summary>
/// 用户配置中的一条「常用路径」。
/// </summary>
public sealed class PathEntry
{
    /// <summary>唯一标识（用于经典菜单注册表子键与命令行参数）。</summary>
    public string Id { get; set; } = "";

    /// <summary>右键菜单里显示的短名称，默认取路径最后一个文件夹名。</summary>
    public string Name { get; set; } = "";

    /// <summary>目标根路径。</summary>
    public string Path { get; set; } = "";

    /// <summary>true = 移入该路径下「今天日期(yyyy-MM-dd)」的子文件夹（不存在则自动创建）。</summary>
    public bool UseDateFolder { get; set; }
}

/// <summary>
/// QuickTidy 全局配置（保存于 %APPDATA%\QuickTidy\config.json）。
/// </summary>
public sealed class QuickTidyConfig
{
    public List<PathEntry> Paths { get; set; } = new();

    /// <summary>
    /// 目标处已存在同名项时的策略：
    /// ask = 弹窗询问（默认）；rename = 自动重命名；skip = 跳过；overwrite = 覆盖。
    /// </summary>
    public string ConflictPolicy { get; set; } = "ask";

    /// <summary>界面语言：auto（跟随系统，默认）/ zh / en。</summary>
    public string UiLanguage { get; set; } = "auto";
}
