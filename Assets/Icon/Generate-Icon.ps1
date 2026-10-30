$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$folder = Split-Path -Parent $MyInvocation.MyCommand.Path
$size = 256
$bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics.Clear([System.Drawing.Color]::Transparent)

function RoundedPath([float]$x, [float]$y, [float]$width, [float]$height, [float]$radius) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $radius * 2
    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($x + $width - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($x + $width - $diameter, $y + $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $y + $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

try {
    $outer = RoundedPath 10 10 236 236 48
    $background = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.RectangleF]::new(10, 10, 236, 236),
        [System.Drawing.Color]::FromArgb(255, 37, 68, 105),
        [System.Drawing.Color]::FromArgb(255, 13, 22, 35), 135.0)
    $graphics.FillPath($background, $outer)
    $outline = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 133, 188, 255), 7)
    $graphics.DrawPath($outline, $outer)

    $screen = RoundedPath 40 53 176 150 23
    $screenBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 8, 18, 31))
    $screenLine = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 72, 120, 170), 5)
    $graphics.FillPath($screenBrush, $screen)
    $graphics.DrawPath($screenLine, $screen)

    $glyph = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $glyph.StartFigure()
    $glyph.AddLine(164, 77, 164, 146)
    $glyph.AddBezier(164, 146, 164, 183, 128, 188, 107, 178)
    $glyph.AddBezier(107, 178, 94, 172, 88, 161, 88, 150)
    $glyphPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 235, 245, 255), 21)
    $glyphPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $glyphPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $glyphPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $graphics.DrawPath($glyphPen, $glyph)
    $accent = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 101, 224, 203))
    $graphics.FillEllipse($accent, 80, 77, 24, 24)

    $pngPath = Join-Path $folder 'JonsboStudio.png'
    $bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $sizes = @(16, 24, 32, 48, 64, 128, 256)
    $images = [System.Collections.Generic.List[byte[]]]::new()
    foreach ($edge in $sizes) {
        $scaled = [System.Drawing.Bitmap]::new($edge, $edge, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $target = [System.Drawing.Graphics]::FromImage($scaled)
        try {
            $target.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $target.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $target.DrawImage($bitmap, 0, 0, $edge, $edge)
            $memory = [System.IO.MemoryStream]::new()
            try { $scaled.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png); $images.Add($memory.ToArray()) }
            finally { $memory.Dispose() }
        } finally { $target.Dispose(); $scaled.Dispose() }
    }
    $icoPath = Join-Path $folder 'JonsboStudio.ico'
    $stream = [System.IO.File]::Create($icoPath)
    $writer = [System.IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
        $offset = 6 + 16 * $sizes.Count
        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $writer.Write([byte]($sizes[$i] % 256)); $writer.Write([byte]($sizes[$i] % 256))
            $writer.Write([byte]0); $writer.Write([byte]0)
            $writer.Write([uint16]1); $writer.Write([uint16]32)
            $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
            $offset += $images[$i].Length
        }
        foreach ($image in $images) { $writer.Write($image) }
    } finally { $writer.Dispose() }
    Write-Host "Generated $icoPath"
} finally {
    foreach ($item in @($accent, $glyphPen, $glyph, $screenLine, $screenBrush, $screen, $outline, $background, $outer, $graphics, $bitmap)) {
        if ($null -ne $item) { $item.Dispose() }
    }
}
