Add-Type -AssemblyName System.Drawing
$assetDirectory = Join-Path (Split-Path $PSScriptRoot -Parent) 'src\AppVolumeController\Assets'
$frames = @()
foreach ($size in @(16,24,32,48,64,128,256)) {
    $bitmap = New-Object System.Drawing.Bitmap($size,$size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = 'AntiAlias'
    $graphics.ScaleTransform($size/256.0,$size/256.0)
    $background = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#192335'))
    $accent = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#71DAC8'))
    $round = New-Object System.Drawing.Drawing2D.GraphicsPath
    $round.AddArc(0,0,108,108,180,90); $round.AddArc(148,0,108,108,270,90)
    $round.AddArc(148,148,108,108,0,90); $round.AddArc(0,148,108,108,90,90); $round.CloseFigure()
    $graphics.FillPath($background,$round)
    $points = [System.Drawing.PointF[]]@([System.Drawing.PointF]::new(48,100),[System.Drawing.PointF]::new(80,100),[System.Drawing.PointF]::new(124,64),[System.Drawing.PointF]::new(124,192),[System.Drawing.PointF]::new(80,156),[System.Drawing.PointF]::new(48,156))
    $graphics.FillPolygon($accent,$points)
    $pen = New-Object System.Drawing.Pen($accent,14)
    $pen.StartCap = 'Round'; $pen.EndCap = 'Round'
    $graphics.DrawBezier($pen,150,96,171,117,171,139,150,160)
    $graphics.DrawBezier($pen,178,70,215,109,215,147,178,186)
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
    $frames += ,@($size,$stream.ToArray())
    if ($size -eq 256) { $bitmap.Save((Join-Path $assetDirectory 'speaker.png'),[System.Drawing.Imaging.ImageFormat]::Png) }
    $stream.Dispose(); $pen.Dispose(); $round.Dispose(); $accent.Dispose(); $background.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
}
$file = [System.IO.File]::Create((Join-Path $assetDirectory 'speaker.ico'))
$writer = New-Object System.IO.BinaryWriter($file)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
    $offset = 6 + 16*$frames.Count
    foreach ($frame in $frames) {
        $dimension = if ($frame[0] -eq 256) {0} else {$frame[0]}
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$frame[1].Length); $writer.Write([uint32]$offset)
        $offset += $frame[1].Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame[1]) }
} finally { $writer.Dispose(); $file.Dispose() }
# Keep the reusable favicon identical to the Windows application icon.
Copy-Item -LiteralPath (Join-Path $assetDirectory 'speaker.ico') -Destination (Join-Path $assetDirectory 'favicon.ico') -Force
