param(
    [string]$Version = "1.0.0.2",
    [string]$PackageName = "57742TomokazuKizawa.WoodStreamPLAZA",
    [string]$Publisher = "CN=963B8572-7B10-48CC-9F90-46F0022D6A68",
    [string]$PublisherDisplayName = "Tomokazu Kizawa",
    [switch]$CreateBundle = $true
)

$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
if (-not $scriptDir) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
}
if (-not $scriptDir) {
    $scriptDir = "D:\Dev\PlazaApp\packaging\msix"
}

Write-Host "MSIX Packaging Directory: $scriptDir" -ForegroundColor Cyan
$rootDir = (Resolve-Path "$scriptDir\..\..").Path
$msixOutputDir = [System.IO.Path]::Combine($rootDir, "MSIX")
if (!(Test-Path $msixOutputDir)) {
    New-Item -ItemType Directory -Force -Path $msixOutputDir | Out-Null
}

# 1. アセット画像の自動生成
$generateAssetsScript = [System.IO.Path]::Combine($scriptDir, "generate_assets.ps1")
if ($generateAssetsScript -and (Test-Path $generateAssetsScript)) {
    Write-Host "--- Generating MSIX Store Assets ---" -ForegroundColor Cyan
    & $generateAssetsScript
}

$architectures = @("x64", "arm64")

# 2. makeappx.exe の探索
$makeAppxPath = $null
$possibleSdkPaths = @(
    "$env:USERPROFILE\.nuget\packages\microsoft.windows.sdk.buildtools\*\bin\*\x64\makeappx.exe",
    "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\makeappx.exe",
    "C:\Program Files\Windows Kits\10\bin\*\x64\makeappx.exe",
    "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\makeappx.exe"
)
foreach ($pattern in $possibleSdkPaths) {
    $found = Get-Item $pattern -ErrorAction SilentlyContinue | Select-Object -Last 1
    if ($found) {
        $makeAppxPath = $found.FullName
        break
    }
}

if ($makeAppxPath) {
    Write-Host "Found makeappx.exe: $makeAppxPath" -ForegroundColor Green
} else {
    Write-Warning "makeappx.exe was not found. Please install Microsoft.Windows.SDK.BuildTools or Windows SDK."
}

$generatedMsixFiles = @()

# 3. 各アーキテクチャの MSIX 生成
foreach ($arch in $architectures) {
    Write-Host "`n--- Packaging MSIX for $arch ---" -ForegroundColor Cyan
    $publishDir = [System.IO.Path]::Combine($rootDir, "publish\win-$arch")
    
    dotnet publish "$rootDir\WoodStreamPlaza.csproj" -c Release -r "win-$arch" --self-contained false -p:Version=$Version -p:FileVersion=$Version -p:AssemblyVersion=$Version -p:UseSharedCompilation=false -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $arch."
    }

    $stageDir = [System.IO.Path]::Combine($rootDir, "packaging\msix\stage_$arch")
    if (Test-Path $stageDir) {
        Remove-Item -Recurse -Force $stageDir
    }
    New-Item -ItemType Directory -Force -Path $stageDir | Out-Null

    # 発行バイナリをコピー
    Copy-Item "$publishDir\*" -Destination $stageDir -Recurse -Force

    # Assets フォルダをコピー
    $assetsTarget = [System.IO.Path]::Combine($stageDir, "Assets")
    New-Item -ItemType Directory -Force -Path $assetsTarget | Out-Null
    Copy-Item "$scriptDir\Assets\*" -Destination $assetsTarget -Force

    # AppxManifest.xml の生成・プレースホルダー置換
    $templatePath = [System.IO.Path]::Combine($scriptDir, "AppxManifest_Template.xml")
    if (!(Test-Path $templatePath)) {
        throw "Template not found: $templatePath"
    }
    $manifestTemplate = [System.IO.File]::ReadAllText($templatePath, [System.Text.Encoding]::UTF8)
    $manifestContent = $manifestTemplate.Replace("__PACKAGE_NAME__", $PackageName)
    $manifestContent = $manifestContent.Replace("__PUBLISHER__", $Publisher)
    $manifestContent = $manifestContent.Replace("__PUBLISHER_DISPLAY_NAME__", $PublisherDisplayName)
    $manifestContent = $manifestContent.Replace("__VERSION__", $Version)
    $manifestContent = $manifestContent.Replace("__ARCH__", $arch)

    if ([string]::IsNullOrWhiteSpace($manifestContent)) {
        throw "Generated AppxManifest.xml content is empty!"
    }

    $manifestDest = [System.IO.Path]::Combine($stageDir, "AppxManifest.xml")
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($manifestDest, $manifestContent, $utf8NoBom)

    $outputMsix = [System.IO.Path]::Combine($msixOutputDir, "WoodStreamPlaza_${Version}_${arch}.msix")
    if (Test-Path $outputMsix) {
        Remove-Item -Force $outputMsix
    }

    if ($makeAppxPath) {
        Write-Host "Packaging with makeappx.exe -> $outputMsix"
        & $makeAppxPath pack /d $stageDir /p $outputMsix /o
        if ($LASTEXITCODE -ne 0) {
            throw "makeappx pack failed for $arch with exit code $LASTEXITCODE"
        }
    } else {
        Write-Host "Packaging with ZipFile fallback -> $outputMsix"
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory($stageDir, $outputMsix)
    }

    $generatedMsixFiles += $outputMsix
    Write-Host "Created: $outputMsix" -ForegroundColor Green
    Remove-Item -Recurse -Force $stageDir -ErrorAction SilentlyContinue
}

# 4. MSIX Bundle の生成 (x64 + arm64 統合パッケージ)
if ($CreateBundle -and $makeAppxPath -and ($generatedMsixFiles.Count -eq 2)) {
    Write-Host "`n--- Generating MSIX Bundle (x64 + arm64) ---" -ForegroundColor Cyan
    $bundleStage = [System.IO.Path]::Combine($rootDir, "packaging\msix\bundle_stage")
    if (Test-Path $bundleStage) {
        Remove-Item -Recurse -Force $bundleStage
    }
    New-Item -ItemType Directory -Force -Path $bundleStage | Out-Null

    foreach ($file in $generatedMsixFiles) {
        Copy-Item $file -Destination $bundleStage -Force
    }

    $outputBundle = [System.IO.Path]::Combine($msixOutputDir, "WoodStreamPlaza_${Version}.msixbundle")
    if (Test-Path $outputBundle) {
        Remove-Item -Force $outputBundle
    }

    Write-Host "Creating bundle: $outputBundle"
    & $makeAppxPath bundle /bv $Version /d $bundleStage /p $outputBundle /o
    if ($LASTEXITCODE -ne 0) {
        throw "makeappx bundle failed with exit code $LASTEXITCODE"
    }
    Remove-Item -Recurse -Force $bundleStage -ErrorAction SilentlyContinue

    Write-Host "Created: $outputBundle" -ForegroundColor Green
}

Write-Host "`n==================================================" -ForegroundColor Green
Write-Host "MSIX Packaging Complete!" -ForegroundColor Green
Write-Host "Output Directory: $msixOutputDir" -ForegroundColor Green
Get-ChildItem $msixOutputDir -Filter "WoodStreamPlaza_${Version}*" | ForEach-Object {
    $sizeMB = [Math]::Round($_.Length / 1MB, 2)
    Write-Host " - $($_.Name) ($sizeMB MB)" -ForegroundColor Yellow
}
Write-Host "==================================================" -ForegroundColor Green
