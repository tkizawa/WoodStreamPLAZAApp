param(
    [string]$SourcePath = "$PSScriptRoot\..\..\favicon.ico",
    [string]$OutputDir = "$PSScriptRoot\Assets"
)

Add-Type -AssemblyName System.Drawing

if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
}

$sourceImg = [System.Drawing.Image]::FromFile((Resolve-Path $SourcePath))

function Resize-Image {
    param(
        [System.Drawing.Image]$Image,
        [int]$Width,
        [int]$Height,
        [string]$DestinationPath,
        [System.Drawing.Color]$BgColor = [System.Drawing.Color]::Transparent,
        [double]$PaddingRatio = 0.0
    )

    $bmp = New-Object System.Drawing.Bitmap($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bmp)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

    $graphics.Clear($BgColor)

    $availW = $Width * (1.0 - $PaddingRatio * 2)
    $availH = $Height * (1.0 - $PaddingRatio * 2)
    
    $scale = [Math]::Min($availW / $Image.Width, $availH / $Image.Height)
    $drawW = [int]($Image.Width * $scale)
    $drawH = [int]($Image.Height * $scale)
    $drawX = [int](($Width - $drawW) / 2)
    $drawY = [int](($Height - $drawH) / 2)

    $graphics.DrawImage($Image, $drawX, $drawY, $drawW, $drawH)
    $graphics.Dispose()

    $bmp.Save($DestinationPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Generated: $(Split-Path $DestinationPath -Leaf) ($Width x $Height)" -ForegroundColor Green
}

# 1. Square44x44Logo (標準 + スケール)
Resize-Image -Image $sourceImg -Width 44 -Height 44 -DestinationPath (Join-Path $OutputDir "Square44x44Logo.png")
Resize-Image -Image $sourceImg -Width 44 -Height 44 -DestinationPath (Join-Path $OutputDir "Square44x44Logo.targetsize-44.png")
Resize-Image -Image $sourceImg -Width 24 -Height 24 -DestinationPath (Join-Path $OutputDir "Square44x44Logo.targetsize-24_altform-unplated.png")
Resize-Image -Image $sourceImg -Width 88 -Height 88 -DestinationPath (Join-Path $OutputDir "Square44x44Logo.scale-200.png")

# 2. Square150x150Logo (標準 + スケール)
Resize-Image -Image $sourceImg -Width 150 -Height 150 -DestinationPath (Join-Path $OutputDir "Square150x150Logo.png")
Resize-Image -Image $sourceImg -Width 300 -Height 300 -DestinationPath (Join-Path $OutputDir "Square150x150Logo.scale-200.png")

# 3. StoreLogo (50x50 + スケール)
Resize-Image -Image $sourceImg -Width 50 -Height 50 -DestinationPath (Join-Path $OutputDir "StoreLogo.png")
Resize-Image -Image $sourceImg -Width 100 -Height 100 -DestinationPath (Join-Path $OutputDir "StoreLogo.scale-200.png")

# 4. Wide310x150Logo (ワイドタイル)
Resize-Image -Image $sourceImg -Width 310 -Height 150 -DestinationPath (Join-Path $OutputDir "Wide310x150Logo.png") -PaddingRatio 0.1
Resize-Image -Image $sourceImg -Width 620 -Height 300 -DestinationPath (Join-Path $OutputDir "Wide310x150Logo.scale-200.png") -PaddingRatio 0.1

# 5. Square310x310Logo (大型タイル)
Resize-Image -Image $sourceImg -Width 310 -Height 310 -DestinationPath (Join-Path $OutputDir "Square310x310Logo.png")
Resize-Image -Image $sourceImg -Width 620 -Height 620 -DestinationPath (Join-Path $OutputDir "Square310x310Logo.scale-200.png")

# 6. SplashScreen (起動スプラッシュ 620x300)
Resize-Image -Image $sourceImg -Width 620 -Height 300 -DestinationPath (Join-Path $OutputDir "SplashScreen.png") -PaddingRatio 0.15
Resize-Image -Image $sourceImg -Width 1240 -Height 600 -DestinationPath (Join-Path $OutputDir "SplashScreen.scale-200.png") -PaddingRatio 0.15

$sourceImg.Dispose()
Write-Host "All MSIX Assets generated successfully." -ForegroundColor Cyan
