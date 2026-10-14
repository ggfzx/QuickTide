# 速理 QuickTidy

右键一键整理：在文件/文件夹的右键菜单里显示「速理」→ 二级菜单列出你设定的多个常用路径短名称 → 点击即把选中项移动过去。附带设置界面管理路径、短名称、日期子文件夹归档与冲突策略。

- 中文名：**速理**
- 英文名：**QuickTidy**（简称 QT）
- 支持：Windows 10 x64（经典右键菜单）+ Windows 11 x64（经典菜单 + 新式右键菜单）

---

## 功能一览

| 功能 | 说明 |
|---|---|
| 多路径管理 | 设置界面增删改路径，支持上下移排序 |
| 短名称 | 默认自动取路径最后一级文件夹名，可自行修改 |
| 日期子文件夹 | 每条路径可勾选「移入该路径下今天日期的子文件夹（yyyy-MM-dd，无则自动创建）」 |
| 经典右键菜单 | 注册表静态级联菜单，Win10 / Win11 全支持（Win11 下在「显示更多选项」里） |
| Win11 新式菜单 | IExplorerCommand 进程外 COM 服务器 + sparse package，出现在 Win11 新菜单一级 |
| 冲突策略 | 弹窗询问（默认）/ 自动重命名 / 跳过 / 覆盖，全局设置 |
| 跨盘移动 | 同盘直接 Move，跨盘自动 复制+删除 |
| Win10/Win11 兼容 | 同一套程序，Win10 自动只注册经典菜单 |

菜单二级项末尾固定带一个 **「设置 QuickTidy…」** 入口，随时打开设置界面。

## 目录结构

```
QuickTidy/
├── QuickTidy.sln
├── assets/qt.ico                      # 应用与菜单图标（由 gen_assets.py 生成）
├── src/
│   ├── QuickTidy.Core/                # 共享核心：配置模型 + 移动引擎（无 UI 依赖）
│   │   ├── ConfigModel.cs             #   PathEntry / QuickTidyConfig
│   │   ├── ConfigStore.cs             #   配置路径 %APPDATA%\QuickTidy\config.json
│   │   └── MoveEngine.cs              #   移动/日期子文件夹/冲突处理/跨盘复制
│   ├── QuickTidy.App/                 # 主程序 QuickTidy.exe（WinForms）
│   │   ├── Program.cs                 #   模式分发：无参=设置 / /move /setup /cleanup
│   │   ├── MainForm.cs                #   设置界面（路径列表 + 策略 + 菜单注册）
│   │   ├── ClassicMenuRegistrar.cs    #   经典菜单注册表注册（HKCU，免管理员）
│   │   └── ModernMenuInstaller.cs     #   Win11 新式菜单注册（LocalServer32 + sparse package）
│   ├── QuickTidy.MenuHost/            # Win11 新式菜单宿主 QuickTidy.MenuHost.exe
│   │   ├── Program.cs                 #   进程外 COM 本地服务器（CoRegisterClassObject + 消息循环）
│   │   ├── ShellInterop.cs            #   IExplorerCommand / IEnumExplorerCommand / IShellItemArray 定义
│   │   └── Commands.cs                #   「速理」菜单树 + Invoke 移动
└── packaging/
    ├── AppxManifest.xml               # sparse package 清单（com:ExeServer + fileExplorerContextMenus）
    ├── Register-SparsePackage.ps1     # 手动注册 Win11 新式菜单
    ├── Unregister-SparsePackage.ps1
    ├── installer.iss                  # Inno Setup 安装包脚本（按用户安装，免管理员）
    ├── build.cmd                      # 一键构建：发布 + 组装 + 便携 zip + 安装包
    ├── gen_assets.py                  # 图标生成脚本
    └── payload/                       # 构建产物（便携版内容）
```

## 技术选型

| 方案 | 结论 |
|---|---|
| **C# / .NET 8（采用）** | 单一技术栈覆盖全部需求：WinForms 设置界面、经典注册表菜单、Win11 COM 菜单（进程外服务器，无需 NativeAOT/C++ 工具链）、自包含单文件发布 |
| C++/Win32 COM | 性能与兼容最佳，但开发门槛高、构建链重 |
| Python（PyInstaller） | 体积大（50MB+）、杀软误报率高、COM Shell 扩展支持弱 |
| Go（Fyne） | 桌面生态一般，COM 扩展需手写大量绑定 |

**Win11 新式菜单实现说明**：`QuickTidy.MenuHost.exe` 是进程外 COM 本地服务器（LocalServer32 / sparse package `com:ExeServer`），explorer 通过 CLSID 拉起它，用系统内置的 OneCoreUAPCommonProxyStub 跨进程封送 `IExplorerCommand`。已在本机（Win11 26200）端到端实测：激活、菜单枚举（标题/图标/状态/子命令）、跨进程 Invoke 真实移动文件，全部通过。

## 构建步骤（开发者）

前置：Windows 10/11 x64 + [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)；可选 Inno Setup 6（打安装包用）。

```bat
git clone / 拷贝本项目后：
cd QuickTidy
packaging\build.cmd
```

build.cmd 依次执行：

```bat
dotnet publish src\QuickTidy.MenuHost -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true

dotnet publish src\QuickTidy.App -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

rem 组装 packaging\payload\ 并压缩为 QuickTidy-1.0.0-portable-win-x64.zip
rem 检测到 ISCC 时额外生成 packaging\out\QuickTidy-1.0.0-setup.exe
```

图标资源如需重新生成：`python packaging\gen_assets.py`（纯标准库，无第三方依赖）。

## 交付方案 A：单文件便携版

`packaging\payload\` 即绿色便携版，也可直接用 `QuickTidy-1.0.0-portable-win-x64.zip`。放到任意目录（**不要放需要管理员写入的目录，如 C:\Program Files**，菜单注册需读取该路径下的 exe），运行 `QuickTidy.exe`：

1. 添加常用路径（默认短名称 = 最后一级文件夹名）
2. 点「启用经典右键菜单 (Win10/Win11)」
3. Win11 用户可再点「注册 Win11 新式菜单」（见下文开发者模式说明）

配置保存在 `%APPDATA%\QuickTidy\config.json`，改完路径「保存并应用」后经典菜单自动刷新。

## 交付方案 B：标准安装包（Inno Setup）

用 Inno Setup 6 编译 `packaging\installer.iss`（或直接运行 build.cmd）得到 `QuickTidy-1.0.0-setup.exe`：

- 按当前用户安装到 `%LocalAppData%\Programs\QuickTidy`，**无需管理员权限**
- 安装向导 + 可选桌面快捷方式 + 开始菜单项
- 安装完成自动执行 `/setup`：注册经典菜单、尝试注册 Win11 新式菜单（结果写入 `%APPDATA%\QuickTidy\QuickTidy.log`）
- 卸载时自动执行 `/cleanup`：移除全部右键菜单注册

## Win11 新式菜单的重要说明（必读）

未签名的 sparse package 注册时 Windows 会拒绝（错误 0x80073CFF，本机已实测），三选一：

1. **开启开发者模式（个人使用推荐）**：设置 → 系统 → 开发者选项 → 打开「开发人员模式」，然后重新点「注册 Win11 新式菜单」即可成功。
2. **签名**：用 `makeappx.exe` 把 manifest 打包为 .msix，用可信证书签名后 `Add-AppxPackage`（适合分发，需 Windows SDK）。
3. **不用新式菜单**：经典菜单在 Win11 同样可用（右键 →「显示更多选项」，或选中后按 Shift+F10）。

注册/移除新式菜单后需重启 explorer.exe（任务管理器重启「Windows 资源管理器」）才能看到变化。

## 测试与排错清单

已在本机（Windows 11 26200 x64）实测通过：

- [x] COM 进程外服务器激活与跨进程方法调用（CoCreateInstance / GetTitle / EnumSubCommands / Invoke）
- [x] 菜单树：主项「速理」+ 每条路径一个子项 +「设置 QuickTidy…」
- [x] 跨进程 Invoke 真实移动文件（同盘 Move）
- [x] 经典菜单注册表结构（`*\shell` 与 `Directory\shell`，HKCU 免管理员）
- [x] `/setup`、`/cleanup`、`/move "%V" <id>` 命令行全流程
- [x] 自包含单文件发布（QuickTidy.exe ~68MB / MenuHost ~33MB，运行时零依赖）
- [ ] Win11 新式菜单在 explorer 中的最终显示（需开发者模式或签名后在你目标系统实测）

常见问题：

| 现象 | 处理 |
|---|---|
| Win11 右键看不到「速理」 | 新菜单最多显示每个应用 16 项且需重启 explorer；或直接用「显示更多选项」里的经典菜单 |
| 新式菜单注册报 0x80073CFF | 未签名包需开发者模式，见上节 |
| 点菜单没反应 | 检查 `%APPDATA%\QuickTidy\config.json` 是否存在且路径有效；重开设置程序保存一次 |
| 移动失败提示被占用 | 目标文件被程序锁定，关闭占用程序后重试 |
| 目标路径失效 | 程序会弹窗提示路径不存在，请更新设置 |

## 安全与杀软提示（重要）

- 程序未做代码签名，**Windows SmartScreen / Defender / 第三方杀软可能误报**。规避方式：①购买代码签名证书对 QuickTidy.exe / QuickTidy.MenuHost.exe 签名；②在杀软中把安装目录加入白名单；③SmartScreen 弹窗选「仍要运行」。
- 菜单注册只写 `HKEY_CURRENT_USER\Software\Classes`（当前用户），不写 HKLM，不修改系统级配置。
- 程序对外仅访问：配置文件 `%APPDATA%\QuickTidy\`、你设置的目标路径、以及移动操作涉及的文件。

## 已知限制

1. Win11 新式菜单每个应用最多显示约 16 项（系统限制），建议常用路径不超过 15 条；经典菜单无此限制。
2. 经典菜单多选时按选中项逐个触发 `/move`（每个文件一次）；新式菜单一次调用处理全部选中项。
3. 跨盘移动大文件/大文件夹为「复制+删除」，无进度对话框，期间请勿中断。
4. 同名冲突「覆盖」时：文件覆盖文件、文件夹覆盖文件夹；类型不同自动转为重命名，避免误删。
5. 移动的是「文件系统路径」类项目；对「此电脑」「回收站」等虚拟位置右键时菜单项不生效（会自动忽略）。
