# WoodStream PLAZA スタンドアロンインストーラ自動ビルドスクリプト (x64 / arm64)
# 使い方: powershell -ExecutionPolicy Bypass -File packaging/installer/build_installers.ps1

$ErrorActionPreference = "Stop"

$RootDir = Resolve-Path "$PSScriptRoot\..\.."
$ProjectFile = "$RootDir\WoodStreamPlaza.csproj"
$IssFile = "$RootDir\packaging\installer\setup.iss"

# ISCC (Inno Setup Compiler) の探索
$isccPath = "C:\Users\$env:USERNAME\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (!(Test-Path $isccPath)) {
    $found = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($found) {
        $isccPath = $found.Source
    } else {
        $foundInProgramFiles = Get-ChildItem "C:\Program Files*\Inno Setup*" -Recurse -Filter "ISCC.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($foundInProgramFiles) {
            $isccPath = $foundInProgramFiles.FullName
        } else {
            Write-Error "Inno Setup Compiler (ISCC.exe) が見つかりませんでした。"
            exit 1
        }
    }
}

Write-Host "=== 1. .NET 10 リリース発行 (win-x64 & win-arm64) ===" -ForegroundColor Cyan

Write-Host "[1/2] win-x64 発行中..."
dotnet publish $ProjectFile -c Release -r win-x64 --self-contained true -o "$RootDir\publish\win-x64"

Write-Host "[2/2] win-arm64 発行中..."
dotnet publish $ProjectFile -c Release -r win-arm64 --self-contained true -o "$RootDir\publish\win-arm64"

Write-Host "`n=== 2. Inno Setup インストーラビルド ===" -ForegroundColor Cyan

Write-Host "[1/2] x64 インストーラ作成中..."
& $isccPath /DTargetArch=x64 $IssFile

Write-Host "[2/2] arm64 インストーラ作成中..."
& $isccPath /DTargetArch=arm64 $IssFile

Write-Host "`n=== ビルド完了 ===" -ForegroundColor Green
Get-ChildItem "$RootDir\Installer" -Filter "*.exe" | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
