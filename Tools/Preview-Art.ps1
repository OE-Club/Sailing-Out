$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot = Split-Path -Parent $PSScriptRoot
$output = Join-Path $taskRoot 'Delivery'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$canvas = [System.Drawing.Bitmap]::new(1120, 840)
$graphics = [System.Drawing.Graphics]::FromImage($canvas)
$graphics.Clear([System.Drawing.ColorTranslator]::FromHtml('#122F43'))
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$font = [System.Drawing.Font]::new('Segoe UI',38,[System.Drawing.FontStyle]::Bold,[System.Drawing.GraphicsUnit]::Pixel)
$small = [System.Drawing.Font]::new('Segoe UI',16,[System.Drawing.FontStyle]::Regular,[System.Drawing.GraphicsUnit]::Pixel)
$ink = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#E8F0D8'))
$secondary = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#9AD6CB'))
$graphics.DrawString('SAILING OUT / PIXEL ATLAS',$font,$ink,32,22)
$graphics.DrawString('16 PPU  /  ORIGINAL ART  /  3 VARIANTS PER CATEGORY',$small,$secondary,34,70)
$categories = @('buoy','reef','seabird','supply','decoration')
for ($row=0;$row -lt 5;$row++) {
    $y=112+$row*140
    $graphics.DrawString($categories[$row].ToUpper(),$small,$secondary,36,$y+40)
    for ($variant=1;$variant -le 3;$variant++) {
        $path=Join-Path $taskRoot ('Assets\SailingOut\Art\Sprites\{0}_{1:00}_01.png' -f $categories[$row],$variant)
        $sprite=[System.Drawing.Image]::FromFile($path)
        $graphics.DrawImage($sprite,[System.Drawing.Rectangle]::new(230+($variant-1)*260,$y,128,128),0,0,32,32,[System.Drawing.GraphicsUnit]::Pixel)
        $sprite.Dispose()
    }
}
$canvas.Save((Join-Path $output 'art-atlas.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose(); $canvas.Dispose(); $font.Dispose(); $small.Dispose(); $ink.Dispose(); $secondary.Dispose()
