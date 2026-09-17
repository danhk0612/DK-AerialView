param(
    [string]$OutputPath = "src/DKAerialView/Assets/AppIcon.ico"
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$fullPath = [System.IO.Path]::GetFullPath($OutputPath)
$directory = [System.IO.Path]::GetDirectoryName($fullPath)
[System.IO.Directory]::CreateDirectory($directory) | Out-Null

$size = 256
$bitmap = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

try {
    $background = New-Object System.Drawing.Drawing2D.GraphicsPath
    $radius = 48
    $diameter = $radius * 2
    $background.AddArc(8, 8, $diameter, $diameter, 180, 90)
    $background.AddArc(248 - $diameter, 8, $diameter, $diameter, 270, 90)
    $background.AddArc(248 - $diameter, 248 - $diameter, $diameter, $diameter, 0, 90)
    $background.AddArc(8, 248 - $diameter, $diameter, $diameter, 90, 90)
    $background.CloseFigure()

    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point 24, 24),
        (New-Object System.Drawing.Point 232, 232),
        ([System.Drawing.Color]::FromArgb(21, 34, 56)),
        ([System.Drawing.Color]::FromArgb(36, 85, 122)))
    $graphics.FillPath($bgBrush, $background)

    $roof = [System.Drawing.Point[]]@(
        (New-Object System.Drawing.Point 128, 38),
        (New-Object System.Drawing.Point 215, 82),
        (New-Object System.Drawing.Point 128, 126),
        (New-Object System.Drawing.Point 41, 82))
    $roofBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point 55, 45),
        (New-Object System.Drawing.Point 205, 120),
        ([System.Drawing.Color]::FromArgb(126, 215, 247)),
        ([System.Drawing.Color]::FromArgb(66, 165, 213)))
    $graphics.FillPolygon($roofBrush, $roof)

    $leftFace = [System.Drawing.Point[]]@(
        (New-Object System.Drawing.Point 41, 82),
        (New-Object System.Drawing.Point 128, 126),
        (New-Object System.Drawing.Point 128, 197),
        (New-Object System.Drawing.Point 41, 152))
    $rightFace = [System.Drawing.Point[]]@(
        (New-Object System.Drawing.Point 215, 82),
        (New-Object System.Drawing.Point 128, 126),
        (New-Object System.Drawing.Point 128, 197),
        (New-Object System.Drawing.Point 215, 152))
    $graphics.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(232, 245, 252))), $leftFace)
    $graphics.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(169, 221, 240))), $rightFace)

    $base = [System.Drawing.Point[]]@(
        (New-Object System.Drawing.Point 81, 151),
        (New-Object System.Drawing.Point 128, 176),
        (New-Object System.Drawing.Point 175, 151),
        (New-Object System.Drawing.Point 175, 177),
        (New-Object System.Drawing.Point 128, 202),
        (New-Object System.Drawing.Point 81, 177))
    $graphics.FillPolygon([System.Drawing.Brushes]::White, $base)

    $graphics.FillEllipse([System.Drawing.Brushes]::White, 115, 69, 26, 26)
    $markPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(36, 85, 122)), 7
    $markPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $markPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $graphics.DrawLine($markPen, 128, 61, 128, 103)
    $graphics.DrawLine($markPen, 107, 82, 149, 82)

    $pngStream = New-Object System.IO.MemoryStream
    $bitmap.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes = $pngStream.ToArray()

    $fileStream = [System.IO.File]::Create($fullPath)
    $writer = New-Object System.IO.BinaryWriter($fileStream)
    try {
        $writer.Write([UInt16]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]1)
        $writer.Write([Byte]0)
        $writer.Write([Byte]0)
        $writer.Write([Byte]0)
        $writer.Write([Byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$pngBytes.Length)
        $writer.Write([UInt32]22)
        $writer.Write($pngBytes)
    }
    finally {
        $writer.Dispose()
        $fileStream.Dispose()
    }

    Write-Host "Generated application icon: $fullPath"
}
finally {
    $graphics.Dispose()
    $bitmap.Dispose()
}
