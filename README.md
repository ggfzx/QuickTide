<div align="center">

# QuickTidy · 速理

**右键一键整理文件。**

Windows 右键菜单里新增「速理」入口，把选中的文件/文件夹瞬间移动到你设定的常用目录。

[English | [简体中文](README.zh-CN.md)]

</div>

## 它能做什么

- 在任意文件或文件夹的右键菜单里看到 **速理**（Win11 新菜单 / Win11「显示更多选项」/ Win10 都支持）
- 菜单列出你事先设好的几个常用目录的短名称
- 点击其中一个，选中的项目就**移动**到对应目录
- 可以让程序自动把文件放到「今天的日期」子文件夹里（例如 `D:\工作\2026-10-14\报告.docx`）
- 中文系统显示中文，其他语言显示英文

```
右键 文件.docx
  └─ 速理
      ├─ 工作              ← 移动到 D:\工作\
      ├─ 学习              ← 移动到 D:\学习\
      ├─ 备份（今日）      ← 移动到 D:\备份\2026-10-14\
      └─ 设置 QuickTidy…   ← 打开设置窗口
```

## 怎么用

1. 打开 `QuickTidy.exe`
2. 点「添加…」选择一个文件夹（短名称会自动取最后一级文件夹名，可改）
3. （可选）勾选「按今天日期归档」，程序会自动在目标下创建 `yyyy-MM-dd` 子文件夹
4. 选一下重名时怎么办（推荐「弹窗询问」）
5. 点「保存并应用」
6. 点「启用经典右键菜单」—— 右键任意文件/文件夹就能看到「速理」了

> 想要 Win11 新式菜单直接显示？点「注册 Win11 新式菜单」（首次需要 Windows 开发者模式，详见下方说明）。

## 怎么删

- 用程序自带的「移除经典右键菜单」「移除 Win11 新式菜单」按钮
- 或者直接运行 `QuickTidy.exe /cleanup`

## 常见问题

**Q: Win11 右键菜单里没看到「速理」？**
A: Win11 默认分「新式菜单」和「显示更多选项」里的「经典菜单」两层。
- 注册了新式菜单但没看到：设置 → 系统 → 开发者选项 → 打开「开发人员模式」后重试；或者重启一下资源管理器（任务管理器 → 重启「Windows 资源管理器」）。
- 还没看到：用「显示更多选项」试试。

**Q: 安装时 Windows SmartScreen / 杀软弹出警告？**
A: 这是因为程序没有代码签名证书。点「仍要运行」/「信任」即可正常使用。
如果你想分发给其他人用，建议给程序申请一个代码签名证书，警告就不会再弹了。

**Q: 重名冲突怎么办？**
A: 默认弹窗询问，让你选「覆盖 / 重命名 / 跳过」。在设置里可以改成自动重命名、跳过或直接覆盖。

**Q: 可以撤销吗？**
A: 程序用的是「移动」不是「复制」，**撤销只能从回收站找**。覆盖/跳过之前会先弹窗提示你。

**Q: 选了好几个文件可以一次移动吗？**
A: 可以。多选右键 → 速理 → 选目标目录，全部移动过去。

**Q: 文件被锁住 / 移动失败？**
A: 关闭占用文件的程序后重试，常见的占用是文档被 Office 打开、视频在播放等。

**Q: 程序放哪合适？**
A: 任何**当前用户可写**的位置都行。**不要放 `C:\Program Files`**——那需要管理员权限。
推荐：`D:\Tools\QuickTidy\` 或 `%LocalAppData%\Programs\QuickTidy\`。

## Win11 新式菜单的开发者模式说明

未签名的包（这就是我们用的）注册时 Windows 会拒绝。两种办法：

- **个人使用最简单**：系统设置 → 开发者选项 → 打开「开发人员模式」，然后在 QuickTidy 设置里点「注册 Win11 新式菜单」就成功了。
- **不开也完全没问题**：经典菜单在 Win10/Win11 都能用，Win11 下右键选「显示更多选项」即可看到。

## 文件位置

- **程序文件**：你放哪里就在哪里
- **配置 / 日志**：`%APPDATA%\QuickTidy\config.json`（可用记事本编辑）

## 系统要求

- Windows 10 x64（1809 及以上）
- Windows 11 x64
- 不需要额外装任何运行时（程序自带）

## 反馈

遇到问题或想加新功能，在 GitHub Issues 里提。

---

<details>
<summary><b>需要自己编译 / 改源码？展开看</b></summary>

```bat
:: 需要 .NET 8 SDK
dotnet publish src\QuickTidy.MenuHost -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
dotnet publish src\QuickTidy.App    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

工程结构、技术选型、Win11 IExplorerCommand 实现细节等，参见 [`README.zh-CN.md`](README.zh-CN.md)。

</details>
