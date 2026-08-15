param(
    [string]$Output = (Join-Path (Split-Path -Parent $PSScriptRoot) 'assets\StrataShell.ico'),
    [string]$Preview = (Join-Path (Split-Path -Parent $PSScriptRoot) 'assets\StrataShell-icon-preview.png')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$repoRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$assetRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'assets')).TrimEnd('\') + '\'
$outputFull = [System.IO.Path]::GetFullPath($Output)
$previewFull = [System.IO.Path]::GetFullPath($Preview)
foreach ($target in @($outputFull, $previewFull)) {
    if (-not $target.StartsWith($assetRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Icon output must stay inside the assets directory: $target"
    }
}

function New-RoundedPath([float]$x, [float]$y, [float]$width, [float]$height, [float]$radius) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $radius * 2
    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($x + $width - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($x + $width - $diameter, $y + $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $y + $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-LogoBitmap([int]$size) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $scale = $size / 256.0
        $shell = New-RoundedPath (10*$scale) (10*$scale) (236*$scale) (236*$scale) (54*$scale)
        $shellBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
            [System.Drawing.PointF]::new(10*$scale, 10*$scale),
            [System.Drawing.PointF]::new(246*$scale, 246*$scale),
            [System.Drawing.Color]::FromArgb(255, 29, 44, 73),
            [System.Drawing.Color]::FromArgb(255, 8, 13, 24))
        $graphics.FillPath($shellBrush, $shell)
        $graphics.DrawPath([System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 52, 72, 111), [Math]::Max(1, 3*$scale)), $shell)

        $layers = @(
            @{ X=48; Y=47; W=140; H=112; R=25; Fill=[System.Drawing.Color]::FromArgb(255,40,60,105); Stroke=[System.Drawing.Color]::FromArgb(255,108,136,217) },
            @{ X=64; Y=70; W=140; H=112; R=25; Fill=[System.Drawing.Color]::FromArgb(255,23,103,120); Stroke=[System.Drawing.Color]::FromArgb(255,75,194,192) }
        )
        foreach ($layer in $layers) {
            $path = New-RoundedPath ($layer.X*$scale) ($layer.Y*$scale) ($layer.W*$scale) ($layer.H*$scale) ($layer.R*$scale)
            $graphics.FillPath([System.Drawing.SolidBrush]::new($layer.Fill), $path)
            $graphics.DrawPath([System.Drawing.Pen]::new($layer.Stroke, [Math]::Max(1, 3*$scale)), $path)
            $path.Dispose()
        }

        $front = New-RoundedPath (80*$scale) (93*$scale) (140*$scale) (112*$scale) (25*$scale)
        $frontBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
            [System.Drawing.PointF]::new(80*$scale, 93*$scale),
            [System.Drawing.PointF]::new(220*$scale, 205*$scale),
            [System.Drawing.Color]::FromArgb(255,138,167,255),
            [System.Drawing.Color]::FromArgb(255,71,210,197))
        $graphics.FillPath($frontBrush, $front)
        $inset = New-RoundedPath (99*$scale) (112*$scale) (102*$scale) (74*$scale) (14*$scale)
        $graphics.FillPath([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(235,11,20,40)), $inset)

        $shell.Dispose(); $shellBrush.Dispose(); $front.Dispose(); $frontBrush.Dispose(); $inset.Dispose()
    }
    finally {
        $graphics.Dispose()
    }
    return $bitmap
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = foreach ($size in $sizes) {
    $bitmap = New-LogoBitmap $size
    try {
        $stream = [System.IO.MemoryStream]::new()
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        ,$stream.ToArray()
        $stream.Dispose()
    }
    finally {
        $bitmap.Dispose()
    }
}

$file = [System.IO.File]::Open($outputFull, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
$writer = [System.IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
    $offset = 6 + (16 * $sizes.Count)
    for ($index = 0; $index -lt $sizes.Count; $index++) {
        $size = $sizes[$index]
        $writer.Write([byte]($(if ($size -eq 256) { 0 } else { $size })))
        $writer.Write([byte]($(if ($size -eq 256) { 0 } else { $size })))
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$images[$index].Length); $writer.Write([uint32]$offset)
        $offset += $images[$index].Length
    }
    foreach ($image in $images) { $writer.Write($image) }
}
finally {
    $writer.Dispose(); $file.Dispose()
}

$previewBitmap = New-LogoBitmap 256
try { $previewBitmap.Save($previewFull, [System.Drawing.Imaging.ImageFormat]::Png) }
finally { $previewBitmap.Dispose() }

[pscustomobject]@{ Icon=$outputFull; Preview=$previewFull; Bytes=(Get-Item $outputFull).Length }
