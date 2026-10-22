param(
    [Parameter(Mandatory = $true)]
    [string]$SourceImage,
    [string]$OutputDirectory
)

Add-Type -AssemblyName System.Drawing

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $PSScriptRoot '..\logo'
}

$size = 1024
$radius = 190
$targetLetterHeight = 590
$source = [System.Drawing.Bitmap]::new($SourceImage)

try {
    $letterMask = [System.Drawing.Bitmap]::new($source.Width, $source.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $minX = $source.Width
    $minY = $source.Height
    $maxX = -1
    $maxY = -1

    # Extract the bright monogram from the selected ImageGen concept while
    # rejecting its unwanted dark lighting artefacts.
    for ($y = 0; $y -lt $source.Height; $y++) {
        for ($x = 0; $x -lt $source.Width; $x++) {
            $pixel = $source.GetPixel($x, $y)
            $isLetter = $pixel.A -gt 0 -and ($pixel.R + $pixel.G + $pixel.B) -ge 520 -and $pixel.B -gt $pixel.R
            if ($isLetter) {
                $letterMask.SetPixel($x, $y, [System.Drawing.Color]::White)
                if ($x -lt $minX) { $minX = $x }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }

    if ($maxX -lt $minX -or $maxY -lt $minY) {
        throw 'Monogram extraction failed: no letter pixels detected.'
    }

    $sourceBounds = [System.Drawing.Rectangle]::FromLTRB($minX, $minY, $maxX + 1, $maxY + 1)
    $scale = $targetLetterHeight / $sourceBounds.Height
    $targetWidth = [int][Math]::Round($sourceBounds.Width * $scale)
    $targetX = [int][Math]::Round(($size - $targetWidth) / 2)
    $targetY = [int][Math]::Round(($size - $targetLetterHeight) / 2)
    $targetBounds = [System.Drawing.Rectangle]::new($targetX, $targetY, $targetWidth, $targetLetterHeight)

    $variants = @(
        @{ Name = 'logo-a.png'; Background = '#1B2440'; Foreground = '#8FB4FF' },
        @{ Name = 'logo-b.png'; Background = '#2A1E4A'; Foreground = '#C9B6FF' },
        @{ Name = 'logo-c.png'; Background = '#0F2E2A'; Foreground = '#7FE0C4' },
        @{ Name = 'logo-d.png'; Background = '#3A2210'; Foreground = '#FFAA5C' }
    )

    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

    foreach ($variant in $variants) {
        $canvas = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($canvas)
        $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
        $iconRegion = $null
        $scaledMask = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $maskGraphics = [System.Drawing.Graphics]::FromImage($scaledMask)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None

            $diameter = $radius * 2
            $path.AddArc(0, 0, $diameter, $diameter, 180, 90)
            $path.AddArc($size - $diameter, 0, $diameter, $diameter, 270, 90)
            $path.AddArc($size - $diameter, $size - $diameter, $diameter, $diameter, 0, 90)
            $path.AddArc(0, $size - $diameter, $diameter, $diameter, 90, 90)
            $path.CloseFigure()
            $iconRegion = [System.Drawing.Region]::new($path)
            $graphics.Clip = $iconRegion

            $background = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml($variant.Background))
            try { $graphics.FillRectangle($background, 0, 0, $size, $size) } finally { $background.Dispose() }

            $maskGraphics.Clear([System.Drawing.Color]::Transparent)
            $maskGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $maskGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $maskGraphics.DrawImage($letterMask, $targetBounds, $sourceBounds, [System.Drawing.GraphicsUnit]::Pixel)

            $foregroundColor = [System.Drawing.ColorTranslator]::FromHtml($variant.Foreground)
            for ($y = 0; $y -lt $size; $y++) {
                for ($x = 0; $x -lt $size; $x++) {
                    if ($scaledMask.GetPixel($x, $y).A -ge 128) {
                        $canvas.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $foregroundColor.R, $foregroundColor.G, $foregroundColor.B))
                    }
                }
            }

            $outputPath = Join-Path $OutputDirectory $variant.Name
            $canvas.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $maskGraphics.Dispose()
            $scaledMask.Dispose()
            if ($null -ne $iconRegion) { $iconRegion.Dispose() }
            $path.Dispose()
            $graphics.Dispose()
            $canvas.Dispose()
        }
    }
}
finally {
    if ($null -ne $letterMask) { $letterMask.Dispose() }
    $source.Dispose()
}
