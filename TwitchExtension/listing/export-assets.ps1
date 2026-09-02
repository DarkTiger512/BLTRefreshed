# Deterministic export sizes required by Twitch's Version Details form.
# No source images are overwritten.
Add-Type -AssemblyName System.Drawing
function Export-Png([string]$Source, [string]$Target, [int]$Width, [int]$Height) {
    $sourceImage = [System.Drawing.Image]::FromFile($Source)
    $bitmap = [System.Drawing.Bitmap]::new($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($sourceImage, 0, 0, $Width, $Height)
        $bitmap.Save($Target, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $graphics.Dispose(); $bitmap.Dispose(); $sourceImage.Dispose()
    }
}
$crest = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../frontend/src/assets/blt-logo-v2.png'))
Export-Png $crest (Join-Path $PSScriptRoot 'logo-100.png') 100 100
Export-Png $crest (Join-Path $PSScriptRoot 'icon-24.png') 24 24
Export-Png (Join-Path $PSScriptRoot 'discovery-source.png') (Join-Path $PSScriptRoot 'discovery-300x200.png') 300 200
