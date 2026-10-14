using QuickTidy.Core;

namespace QuickTidy.App;

/// <summary>
/// 设置主界面：路径列表（短名称/目标路径/日期子文件夹）+ 冲突策略 + 菜单注册。
/// 界面文字跟随系统语言（中文系统 → 中文，其他系统 → 英文），可用配置 uiLanguage 覆盖。
/// </summary>
public class MainForm : Form
{
    private readonly ListView _list;
    private readonly ComboBox _cboPolicy;
    private readonly Button _btnAdd, _btnEdit, _btnDelete, _btnUp, _btnDown;
    private readonly Button _btnSave;
    private readonly Button _btnClassicOn, _btnClassicOff;
    private readonly Button _btnModernOn, _btnModernOff;
    private readonly Label _status;

    public MainForm()
    {
        Text = L.T("速理 QuickTidy — 设置", "QuickTidy — Settings");
        Font = new Font("Microsoft YaHei UI", 9F);
        Size = new Size(860, 560);
        MinimumSize = new Size(760, 480);
        StartPosition = FormStartPosition.CenterScreen;

        // ---- 路径列表 ----
        _list = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            Dock = DockStyle.Top,
            Height = 300,
        };
        _list.Columns.Add(L.T("短名称", "Short name"), 160);
        _list.Columns.Add(L.T("目标路径", "Target folder"), 430);
        _list.Columns.Add(L.T("按日期归档", "Dated"), 100);
        _list.DoubleClick += (s, e) => EditSelected();

        // ---- 编辑按钮 ----
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(4, 8, 4, 0),
            FlowDirection = FlowDirection.LeftToRight,
        };
        _btnAdd = MakeButton(L.T("添加…", "Add…"), 90);
        _btnEdit = MakeButton(L.T("修改…", "Edit…"), 90);
        _btnDelete = MakeButton(L.T("删除", "Delete"), 70);
        _btnUp = MakeButton(L.T("上移", "Up"), 70);
        _btnDown = MakeButton(L.T("下移", "Down"), 70);
        _btnAdd.Click += (s, e) => AddNew();
        _btnEdit.Click += (s, e) => EditSelected();
        _btnDelete.Click += (s, e) => DeleteSelected();
        _btnUp.Click += (s, e) => MoveSelected(-1);
        _btnDown.Click += (s, e) => MoveSelected(1);
        buttonPanel.Controls.AddRange(new Control[] { _btnAdd, _btnEdit, _btnDelete, _btnUp, _btnDown });

        // ---- 冲突策略 ----
        var policyPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(4, 8, 4, 0),
        };
        policyPanel.Controls.Add(new Label
        {
            Text = L.T("目标已存在同名文件/文件夹时：", "If an item with the same name already exists:"),
            AutoSize = true,
            Margin = new Padding(3, 10, 3, 3),
        });
        _cboPolicy = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 220,
        };
        _cboPolicy.Items.AddRange(new object[]
        {
            L.T("弹窗询问（推荐）", "Ask me (recommended)"),   // ask
            L.T("自动重命名", "Auto-rename"),                   // rename
            L.T("跳过", "Skip"),                                // skip
            L.T("覆盖", "Overwrite"),                           // overwrite
        });
        policyPanel.Controls.Add(_cboPolicy);

        // ---- 保存 ----
        _btnSave = MakeButton(L.T("保存并应用", "Save && Apply"), 120);
        _btnSave.BackColor = Color.FromArgb(14, 154, 167);
        _btnSave.ForeColor = Color.White;
        _btnSave.FlatStyle = FlatStyle.Flat;
        _btnSave.Click += (s, e) => SaveAll();

        var savePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(4, 8, 4, 0),
        };
        savePanel.Controls.Add(_btnSave);

        // ---- 菜单注册 ----
        var menuPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(4, 8, 4, 0),
        };
        _btnClassicOn = MakeButton(L.T("启用经典右键菜单 (Win10/Win11)", "Enable classic menu (Win10/Win11)"), 220);
        _btnClassicOff = MakeButton(L.T("移除经典右键菜单", "Remove classic menu"), 140);
        _btnClassicOn.Click += (s, e) =>
        {
            ClassicMenuRegistrar.Register(CurrentEntries());
            UpdateStatus();
        };
        _btnClassicOff.Click += (s, e) =>
        {
            ClassicMenuRegistrar.Unregister();
            UpdateStatus();
        };
        menuPanel.Controls.Add(_btnClassicOn);
        menuPanel.Controls.Add(_btnClassicOff);

        if (ModernMenuInstaller.IsWin11)
        {
            _btnModernOn = MakeButton(L.T("注册 Win11 新式菜单", "Register Win11 modern menu"), 170);
            _btnModernOff = MakeButton(L.T("移除 Win11 新式菜单", "Remove Win11 modern menu"), 170);
            _btnModernOn.Click += (s, e) =>
            {
                var (ok, output) = ModernMenuInstaller.Register();
                if (!ok)
                    MessageBox.Show(this,
                        L.T(
                            "注册 Win11 新式菜单失败（不影响经典菜单）：\n\n" + output +
                            "\n\n提示：未签名的 sparse package 通常需要先在「系统设置 → 开发者选项」中开启开发者模式，" +
                            "或使用可信证书签名后注册。详见 README。",
                            "Failed to register the Windows 11 modern menu (classic menu is not affected):\n\n" + output +
                            "\n\nTip: unsigned sparse packages usually require Developer Mode " +
                            "(Settings → System → For developers) or a trusted signing certificate. See README."),
                        Caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdateStatus();
            };
            _btnModernOff.Click += (s, e) =>
            {
                ModernMenuInstaller.Unregister();
                UpdateStatus();
            };
            menuPanel.Controls.Add(_btnModernOn);
            menuPanel.Controls.Add(_btnModernOff);
        }
        else
        {
            _btnModernOn = new Button();
            _btnModernOff = new Button();
        }

        // ---- 状态栏 ----
        _status = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
        };

        Controls.Add(menuPanel);
        Controls.Add(savePanel);
        Controls.Add(policyPanel);
        Controls.Add(buttonPanel);
        Controls.Add(_list);
        Controls.Add(_status);

        Load += (s, e) =>
        {
            LoadConfig();
            UpdateStatus();
        };
    }

    private static string Caption => L.T("速理 QuickTidy", "QuickTidy");

    private static Button MakeButton(string text, int width) => new()
    {
        Text = text,
        Width = width,
        Height = 30,
    };

    // ------------------------------------------------------------------
    private List<PathEntry> CurrentEntries()
    {
        var entries = new List<PathEntry>();
        foreach (ListViewItem item in _list.Items)
        {
            if (item.Tag is PathEntry entry) entries.Add(entry);
        }
        return entries;
    }

    private void LoadConfig()
    {
        var config = ConfigStore.Load();
        _list.Items.Clear();
        foreach (var entry in config.Paths) AddListItem(entry);
        _cboPolicy.SelectedIndex = config.ConflictPolicy switch
        {
            "rename" => 1,
            "skip" => 2,
            "overwrite" => 3,
            _ => 0,
        };
    }

    private void AddListItem(PathEntry entry)
    {
        var item = new ListViewItem(entry.Name) { Tag = entry };
        item.SubItems.Add(entry.Path);
        item.SubItems.Add(entry.UseDateFolder ? L.T("是", "Yes") : "");
        _list.Items.Add(item);
    }

    private void AddNew()
    {
        using var dialog = new AddEditDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.Entry is not null)
        {
            AddListItem(dialog.Entry);
            UpdateStatus();
        }
    }

    private void EditSelected()
    {
        if (_list.SelectedItems.Count == 0) return;
        if (_list.SelectedItems[0].Tag is not PathEntry entry) return;

        using var dialog = new AddEditDialog(entry);
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.Entry is not null)
        {
            int index = _list.SelectedItems[0].Index;
            _list.Items.RemoveAt(index);
            var item = new ListViewItem(dialog.Entry.Name) { Tag = dialog.Entry };
            item.SubItems.Add(dialog.Entry.Path);
            item.SubItems.Add(dialog.Entry.UseDateFolder ? L.T("是", "Yes") : "");
            _list.Items.Insert(index, item);
            UpdateStatus();
        }
    }

    private void DeleteSelected()
    {
        if (_list.SelectedItems.Count == 0) return;
        if (MessageBox.Show(this, L.T("确定删除选中的路径吗？", "Delete the selected target?"),
                Caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        foreach (ListViewItem item in _list.SelectedItems) _list.Items.Remove(item);
        UpdateStatus();
    }

    private void MoveSelected(int delta)
    {
        if (_list.SelectedItems.Count == 0) return;
        int index = _list.SelectedItems[0].Index;
        int target = index + delta;
        if (target < 0 || target >= _list.Items.Count) return;

        var item = _list.Items[index];
        _list.Items.RemoveAt(index);
        _list.Items.Insert(target, item);
        item.Selected = true;
    }

    private void SaveAll()
    {
        var entries = CurrentEntries();

        // 默认短名称 = 路径最后一级文件夹名
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                entry.Name = entry.Path.TrimEnd('\\', '/').Split('\\', '/').LastOrDefault() ?? entry.Path;
            }
            entry.Path = entry.Path.TrimEnd('\\');
        }

        var config = new QuickTidyConfig
        {
            Paths = entries,
            ConflictPolicy = _cboPolicy.SelectedIndex switch
            {
                1 => "rename",
                2 => "skip",
                3 => "overwrite",
                _ => "ask",
            },
        };
        try
        {
            ConfigStore.Save(config);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, L.T($"保存配置失败：{ex.Message}", $"Failed to save config: {ex.Message}"),
                Caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // 重新加载（补齐了默认短名称）
        _list.Items.Clear();
        foreach (var entry in entries) AddListItem(entry);

        // 若经典菜单已启用则刷新（路径变了菜单也要变）
        if (ClassicMenuRegistrar.IsRegistered())
            ClassicMenuRegistrar.Register(entries);

        UpdateStatus();
        MessageBox.Show(this, L.T("已保存。右键菜单将使用新配置。", "Saved. The context menu now uses the new config."),
            Caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateStatus()
    {
        bool classic = ClassicMenuRegistrar.IsRegistered();
        bool modern = ModernMenuInstaller.IsWin11 && ModernMenuInstaller.IsPackageRegistered();
        _status.Text = L.T(
            $"路径数：{_list.Items.Count}    经典菜单：{(classic ? "已启用" : "未启用")}    " +
            $"Win11 新式菜单：{(ModernMenuInstaller.IsWin11 ? (modern ? "已注册" : "未注册") : "本系统不支持")}",
            $"Targets: {_list.Items.Count}    Classic menu: {(classic ? "enabled" : "disabled")}    " +
            $"Win11 modern menu: {(ModernMenuInstaller.IsWin11 ? (modern ? "registered" : "not registered") : "not supported on this system")}");
    }
}

/// <summary>添加/修改一条路径。</summary>
public class AddEditDialog : Form
{
    private readonly TextBox _txtName = new() { Width = 320, Left = 110, Top = 20 };
    private readonly TextBox _txtPath = new() { Width = 320, Left = 110, Top = 60 };
    private readonly CheckBox _chkDate = new()
    {
        Text = L.T("移入该路径下今天日期的子文件夹（yyyy-MM-dd，不存在则自动创建）",
            "Move into a dated subfolder (yyyy-MM-dd) inside the target (auto-created)"),
        Left = 110,
        Top = 100,
        AutoSize = true,
    };
    private readonly Button _btnOk = new() { Text = L.T("确定", "OK"), Left = 210, Width = 90, Top = 140 };
    private readonly Button _btnCancel = new() { Text = L.T("取消", "Cancel"), Left = 310, Width = 90, Top = 140 };

    public PathEntry? Entry { get; private set; }

    public AddEditDialog() : this(null) { }

    public AddEditDialog(PathEntry? existing)
    {
        Text = L.T(existing is null ? "添加路径" : "修改路径",
                   existing is null ? "Add target folder" : "Edit target folder");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 190);
        Font = new Font("Microsoft YaHei UI", 9F);

        Controls.Add(new Label { Text = L.T("短名称：", "Short name:"), Left = 30, Top = 23, AutoSize = true });
        Controls.Add(new Label { Text = L.T("目标路径：", "Target folder:"), Left = 30, Top = 63, AutoSize = true });
        Controls.Add(_txtName);
        Controls.Add(_txtPath);
        Controls.Add(_chkDate);

        var btnBrowse = new Button { Text = L.T("浏览…", "Browse…"), Left = 440, Width = 60, Top = 58 };
        btnBrowse.Click += (s, e) =>
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _txtPath.Text = dialog.SelectedPath;
                if (string.IsNullOrWhiteSpace(_txtName.Text))
                {
                    // 默认短名称 = 最后一级文件夹名
                    _txtName.Text = dialog.SelectedPath.TrimEnd('\\', '/').Split('\\', '/').LastOrDefault();
                }
            }
        };
        Controls.Add(btnBrowse);

        _btnOk.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_txtPath.Text) || !System.IO.Directory.Exists(_txtPath.Text))
            {
                MessageBox.Show(this, L.T("请填写有效目标路径。", "Please choose a valid target folder."),
                    L.T("速理 QuickTidy", "QuickTidy"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Entry ??= new PathEntry();
            if (existing is not null) Entry.Id = existing.Id;
            Entry.Id = string.IsNullOrWhiteSpace(Entry.Id)
                ? "p" + Guid.NewGuid().ToString("N")[..8]
                : Entry.Id;
            Entry.Name = _txtName.Text.Trim();
            Entry.Path = _txtPath.Text.Trim();
            Entry.UseDateFolder = _chkDate.Checked;
            DialogResult = DialogResult.OK;
            Close();
        };
        _btnCancel.Click += (s, e) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        if (existing is not null)
        {
            _txtName.Text = existing.Name;
            _txtPath.Text = existing.Path;
            _chkDate.Checked = existing.UseDateFolder;
        }
    }
}
