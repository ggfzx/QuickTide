namespace QuickTidy.Core;

/// <summary>
/// 「把文件/文件夹移动到常用路径」的核心逻辑。
/// 同时被两个宿主使用：
///   - QuickTidy.App（WinForms，弹窗用 MessageBox）
///   - QuickTidy.MenuHost（Win11 新式菜单，弹窗用 MessageBoxW P/Invoke）
/// 因此这里不引用任何 UI 框架，冲突/错误提示通过委托回调给宿主。
/// </summary>
public static class MoveEngine
{
    /// <summary>冲突询问回调：返回 IDYES(6) / IDNO(7) / IDCANCEL(2)。null = 视为取消。</summary>
    public delegate int AskConflictHandler(string message);

    /// <summary>错误提示回调。</summary>
    public delegate void ErrorHandler(string message);

    public const int IdYes = 6;
    public const int IdNo = 7;
    public const int IdCancel = 2;

    /// <summary>把 source 移动到 entry 所指的目标目录（含日期子文件夹、冲突处理）。</summary>
    public static void MoveIntoTarget(string source, PathEntry entry, string policy,
        AskConflictHandler? ask, ErrorHandler? error)
    {
        try
        {
            MoveCore(source, entry, policy, ask, error);
        }
        catch (Exception ex)
        {
            error?.Invoke(L.T($"移动失败：{source}\n\n{ex.Message}",
                $"Move failed: {source}\n\n{ex.Message}"));
        }
    }

    private static void MoveCore(string source, PathEntry entry, string policy,
        AskConflictHandler? ask, ErrorHandler? error)
    {
        bool srcIsFile = File.Exists(source);
        bool srcIsDir = Directory.Exists(source);
        if (!srcIsFile && !srcIsDir)
        {
            error?.Invoke(L.T($"找不到要移动的项目：\n{source}", $"Item not found:\n{source}"));
            return;
        }

        string baseDir = entry.Path?.Trim() ?? "";
        if (baseDir.Length == 0 || !Directory.Exists(baseDir))
        {
            error?.Invoke(L.T($"目标路径不存在或不可访问：\n{baseDir}",
                $"Target folder does not exist or is not accessible:\n{baseDir}"));
            return;
        }

        // 规范化源路径，去掉结尾分隔符
        string srcFull;
        try { srcFull = Path.GetFullPath(source).TrimEnd('\\', '/'); }
        catch { srcFull = source; }
        string name = Path.GetFileName(srcFull);
        if (string.IsNullOrEmpty(name))
        {
            error?.Invoke(L.T($"无法确定项目名称：\n{source}", $"Cannot determine item name:\n{source}"));
            return;
        }

        // 不允许把文件夹移动到其自身内部
        string baseFull;
        try { baseFull = Path.GetFullPath(baseDir).TrimEnd('\\', '/'); }
        catch { baseFull = baseDir; }
        if (string.Equals(srcFull, baseFull, StringComparison.OrdinalIgnoreCase) ||
            baseFull.StartsWith(srcFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            error?.Invoke(L.T($"不能把文件夹移动到它自己的内部：\n{srcFull}\n→ {baseFull}",
                $"Cannot move a folder into itself:\n{srcFull}\n→ {baseFull}"));
            return;
        }

        // 计算目标目录（可选：今天日期子文件夹）
        string targetDir = baseFull;
        if (entry.UseDateFolder)
        {
            targetDir = Path.Combine(baseFull, DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(targetDir);
        }

        // 冲突处理
        string dest = Path.Combine(targetDir, name);
        bool destIsFile = File.Exists(dest);
        bool destIsDir = Directory.Exists(dest);

        if (destIsFile || destIsDir)
        {
            string kind = destIsDir ? L.T("文件夹", "folder") : L.T("文件", "file");
            switch (policy)
            {
                case "skip":
                    return;

                case "rename":
                    dest = UniquePath(dest);
                    break;

                case "overwrite":
                    if (!TryOverwrite(dest, destIsFile, srcIsFile, srcIsDir, srcFull, destIsDir, error))
                        dest = UniquePath(dest);
                    break;

                default: // "ask" 弹窗询问
                    int answer = ask?.Invoke(L.T(
                        $"目标位置已存在同名{kind}：\n\n{dest}\n\n" +
                        "[是] 覆盖它\n[否] 自动重命名保存\n[取消] 跳过本项目",
                        $"An item named \"{Path.GetFileName(dest)}\" already exists in the target ({kind}):\n\n{dest}\n\n" +
                        "[Yes] Overwrite it\n[No] Save with a new name\n[Cancel] Skip this item")) ?? IdCancel;
                    if (answer == IdCancel) return;
                    if (answer == IdNo)
                    {
                        dest = UniquePath(dest);
                    }
                    else
                    {
                        if (!TryOverwrite(dest, destIsFile, srcIsFile, srcIsDir, srcFull, destIsDir, error))
                            dest = UniquePath(dest);
                    }
                    break;
            }
        }

        // 执行移动（跨盘 = 复制 + 删除）
        bool sameVolume = string.Equals(Path.GetPathRoot(srcFull), Path.GetPathRoot(targetDir),
            StringComparison.OrdinalIgnoreCase);

        if (srcIsDir)
        {
            if (sameVolume)
            {
                Directory.Move(srcFull, dest);
            }
            else
            {
                CopyDirectory(srcFull, dest);
                Directory.Delete(srcFull, true);
            }
        }
        else
        {
            if (sameVolume)
            {
                File.Move(srcFull, dest);
            }
            else
            {
                File.Copy(srcFull, dest, true);
                File.Delete(srcFull);
            }
        }
    }

    /// <summary>尝试覆盖目标；类型不匹配或删除失败时返回 false（调用方转自动重命名）。</summary>
    private static bool TryOverwrite(string dest, bool destIsFile, bool srcIsFile, bool srcIsDir,
        string srcFull, bool destIsDir, ErrorHandler? error)
    {
        try
        {
            if (destIsFile && srcIsFile)
            {
                File.Delete(dest);
                return true;
            }
            if (destIsDir && srcIsDir)
            {
                // 防止删除自身子树（前面已有祖先校验，这里双保险）
                Directory.Delete(dest, true);
                return true;
            }
            // 源与目标类型不同（文件 vs 文件夹）→ 保守起见改为重命名
            return false;
        }
        catch (Exception ex)
        {
            error?.Invoke(L.T(
                $"无法覆盖目标（可能正被占用）：\n{dest}\n\n{ex.Message}\n\n已改为自动重命名。",
                $"Cannot overwrite target (possibly in use):\n{dest}\n\n{ex.Message}\n\nFalling back to auto-rename."));
            return false;
        }
    }

    /// <summary>生成不冲突的路径：name.ext → name (2).ext → name (3).ext …</summary>
    public static string UniquePath(string path)
    {
        string dir = Path.GetDirectoryName(path) ?? "";
        string name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(name)) return path;

        string stem = name;
        string ext = "";
        if (File.Exists(path))
        {
            ext = Path.GetExtension(name);
            stem = name[..^ext.Length];
        }

        for (int i = 2; ; i++)
        {
            string candidate = Path.Combine(dir, $"{stem} ({i}){ext}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
                return candidate;
        }
    }

    /// <summary>递归复制目录。</summary>
    private static void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var file in Directory.EnumerateFiles(src))
        {
            string target = Path.Combine(dst, Path.GetFileName(file));
            File.Copy(file, target, true);
        }
        foreach (var dir in Directory.EnumerateDirectories(src))
        {
            CopyDirectory(dir, Path.Combine(dst, Path.GetFileName(dir)));
        }
    }
}
