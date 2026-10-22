param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDirectory,
    [Parameter(Mandatory = $true)]
    [string]$DestinationDirectory
)

Add-Type -AssemblyName System.Drawing

$names = @(
    'mind.png', 'logic.png', 'org.png', 'tree.png', 'timeline.png',
    'vtimeline.png', 'fishbone.png', 'brainstorm.png', 'project.png',
    'meeting.png', 'swot.png', 'week.png', 'orgup.png'
)

New-Item -ItemType Directory -Force -Path $DestinationDirectory | Out-Null

foreach ($name in $names) {
    $sourcePath = Join-Path $SourceDirectory $name
    $destinationPath = Join-Path $DestinationDirectory $name
    $source = [System.Drawing.Image]::FromFile($sourcePath)
    try {
        $targetRatio = 476.0 / 236.0
        $sourceRatio = $source.Width / [double]$source.Height

        if ($sourceRatio -gt $targetRatio) {
            $cropHeight = $source.Height
            $cropWidth = [int][Math]::Round($cropHeight * $targetRatio)
            $cropX = [int](($source.Width - $cropWidth) / 2)
            $cropY = 0
        } else {
            $cropWidth = $source.Width
            $cropHeight = [int][Math]::Round($cropWidth / $targetRatio)
            $cropX = 0
            $cropY = [int](($source.Height - $cropHeight) / 2)
        }

        $bitmap = New-Object System.Drawing.Bitmap 476, 236, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.Clear([System.Drawing.Color]::FromArgb(246, 247, 249))
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $destination = New-Object System.Drawing.Rectangle 0, 0, 476, 236
                $crop = New-Object System.Drawing.Rectangle $cropX, $cropY, $cropWidth, $cropHeight
                $graphics.DrawImage($source, $destination, $crop, [System.Drawing.GraphicsUnit]::Pixel)
            } finally {
                $graphics.Dispose()
            }
            $bitmap.Save($destinationPath, [System.Drawing.Imaging.ImageFormat]::Png)
        } finally {
            $bitmap.Dispose()
        }
    } finally {
        $source.Dispose()
    }
}
