# 手动注册 Win11 新式右键菜单（sparse package，按当前用户注册，无需管理员）
# 用法：powershell -ExecutionPolicy Bypass -File Register-SparsePackage.ps1
$ErrorActionPreference = 'Stop'
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path

# 1) COM LocalServer32 兜底注册（当前用户）
$clsid = '{7E42F3C9-2A64-4B7D-9E81-C50D3A8F6B24}'
New-Item -ItemType Directory -Force -Path "HKCU:\Software\Classes\CLSID\$clsid\LocalServer32" | Out-Null
Set-ItemProperty -Path "HKCU:\Software\Classes\CLSID\$clsid\LocalServer32" -Name '(default)' -Value "`"$dir\QuickTidy.MenuHost.exe`""

# 2) sparse package 注册（提供包标识）
Add-AppxPackage -Register -Path (Join-Path $dir 'AppxManifest.xml') -ExternalLocation $dir
Write-Host 'QuickTidy modern context menu registered. Restart explorer.exe to take effect.'
