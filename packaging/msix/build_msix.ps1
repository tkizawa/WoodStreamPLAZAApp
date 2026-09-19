param(
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = "Stop"
$rootDir = Resolve-Path "$PSScriptRoot\..\.."
$msixOutputDir = Join-Path $rootDir "MSIX"
if (!(Test-Path $msixOutputDir)) {
    New-Item -ItemType Directory -Force -Path $msixOutputDir | Out-Null
}

$architectures = @("x64", "arm64")

# makeappx.exe の探索
$makeAppxPath = $null
$possibleSdkPaths = @(
    "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\makeappx.exe",
    "C:\Program Files\Windows Kits\10\bin\*\x64\makeappx.exe"
)
foreach ($pattern in $possibleSdkPaths) {
    $found = Get-Item $pattern -ErrorAction SilentlyContinue | Select-Object -Last 1
    if ($found) {
        $makeAppxPath = $found.FullName
        break
    }
}

foreach ($arch in $architectures) {
    Write-Host "--- Packaging MSIX for $arch ---" -ForegroundColor Cyan
    $publishDir = Join-Path $rootDir "publish\win-$arch"
    if (!(Test-Path $publishDir)) {
        Write-Host "Publishing win-$arch first..."
        dotnet publish "$rootDir\src\WoodStreamPlaza\WoodStreamPlaza.csproj" -c Release -r "win-$arch" --self-contained false -o $publishDir
    }

    $stageDir = Join-Path $rootDir "packaging\msix\stage_$arch"
    if (Test-Path $stageDir) {
        Remove-Item -Recurse -Force $stageDir
    }
    New-Item -ItemType Directory -Force -Path $stageDir | Out-Null

    # ファイルコピー
    Copy-Item "$publishDir\*" -Destination $stageDir -Recurse -Force
    $assetsTarget = Join-Path $stageDir "Assets"
    New-Item -ItemType Directory -Force -Path $assetsTarget | Out-Null
    Copy-Item "$PSScriptRoot\Assets\*" -Destination $assetsTarget -Force

    # AppxManifest.xml 生成
    $manifestContent = Get-Content "$PSScriptRoot\AppxManifest_Template.xml" -Raw -Encoding utf8
    $manifestContent = $manifestContent.Replace("__ARCH__", $arch)
    Set-Content -Path (Join-Path $stageDir "AppxManifest.xml") -Value $manifestContent -Encoding utf8

    $outputMsix = Join-Path $msixOutputDir "WoodStreamPlaza_${Version}_${arch}.msix"
    if (Test-Path $outputMsix) {
        Remove-Item -Force $outputMsix
    }

    if ($makeAppxPath) {
        Write-Host "Using makeappx.exe: $makeAppxPath"
        & $makeAppxPath pack /d $stageDir /p $outputMsix /o
    } else {
        Write-Host "Using Zip packaging for MSIX container: $outputMsix"
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory($stageDir, $outputMsix)
    }

    Write-Host "Created: $outputMsix" -ForegroundColor Green
    Remove-Item -Recurse -Force $stageDir -ErrorAction SilentlyContinue
}

Write-Host "MSIX generation completed successfully." -ForegroundColor Green
