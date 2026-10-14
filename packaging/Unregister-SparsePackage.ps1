# 移除 Win11 新式右键菜单（sparse package + COM 注册，按当前用户）
$ErrorActionPreference = 'Continue'
Get-AppxPackage -Name QuickTidy | Remove-AppxPackage
Remove-Item -Path 'HKCU:\Software\Classes\CLSID\{7E42F3C9-2A64-4B7D-9E81-C50D3A8F6B24}' -Recurse -ErrorAction SilentlyContinue
Write-Host 'QuickTidy modern context menu removed. Restart explorer.exe to take effect.'
