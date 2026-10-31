$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$sourcePath = Join-Path $PSScriptRoot 'JonsboStudio.png'
$outputPath = Join-Path $PSScriptRoot 'JonsboStudio.ico'
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = New-Object System.Collections.Generic.List[byte[]]
$source = [System.Drawing.Image]::FromFile($sourcePath)
try {
    foreach ($size in $sizes) {
        $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.DrawImage($source, 0, 0, $size, $size)
            } finally {
                $graphics.Dispose()
            }
            $memory = [System.IO.MemoryStream]::new()
            try {
                $bitmap.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
                $images.Add($memory.ToArray())
            } finally {
                $memory.Dispose()
            }
        } finally {
            $bitmap.Dispose()
        }
    }
} finally {
    $source.Dispose()
}

$file = [System.IO.File]::Create($outputPath)
try {
    $writer = [System.IO.BinaryWriter]::new($file)
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $size = $sizes[$i]
        $writer.Write([byte]($size -band 255))
        $writer.Write([byte]($size -band 255))
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$images[$i].Length)
        $writer.Write([uint32]$offset)
        $offset += $images[$i].Length
    }
    foreach ($bytes in $images) {
        $writer.Write($bytes)
    }
    $writer.Flush()
} finally {
    $file.Dispose()
}
Write-Host "Built $outputPath from the official JONSBO logo PNG."
