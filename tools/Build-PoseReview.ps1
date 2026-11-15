# Pet-specific rest registration review; does not modify any source artwork.
param([string]$OutputDirectory = (Join-Path $PSScriptRoot '../build'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$registration = Get-Content -LiteralPath (Join-Path $root 'planning/rest-review-v1.json') -Raw | ConvertFrom-Json
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
foreach ($dark in @($false, $true)) {
    $canvas = [Drawing.Bitmap]::new(1800, 620)
    $g = [Drawing.Graphics]::FromImage($canvas)
    $font = [Drawing.Font]::new('Segoe UI', 18)
    $ink = [Drawing.SolidBrush]::new($(if ($dark) {[Drawing.Color]::WhiteSmoke} else {[Drawing.Color]::FromArgb(30,35,40)}))
    $line = [Drawing.Pen]::new([Drawing.Color]::FromArgb(130,140,150), 1)
    try {
        $g.Clear($(if ($dark) {[Drawing.Color]::FromArgb(30,34,40)} else {[Drawing.Color]::FromArgb(242,244,246)}))
        $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.DrawString('Bruno - rest pose review / common physical scale', $font, $ink, 24, 12)
        $floor = 548
        $g.DrawLine($line, 20, $floor, 1780, $floor)
        for ($i=0; $i -lt $registration.poses.Count; $i++) {
            $pose = $registration.poses[$i]
            $im = [Drawing.Image]::FromFile((Join-Path $root $pose.path))
            try {
                $scale = [double]$pose.scale
                $center = 300+600*$i
                $x = $center-$scale*$pose.support[0]
                $y = $floor-$scale*$pose.support[1]
                $rect = [Drawing.RectangleF]::new([single]$x,[single]$y,[single]($im.Width*$scale),[single]($im.Height*$scale))
                $g.DrawImage($im, $rect)
                $g.DrawString($pose.name,$font,$ink,[single](40+600*$i),[single]565)
            } finally {$im.Dispose()}
        }
        $suffix = if ($dark) {'dark'} else {'light'}
        $output = Join-Path $OutputDirectory ('rest-review-v1-'+$suffix+'.png')
        $canvas.Save($output,[Drawing.Imaging.ImageFormat]::Png)
        Write-Output ([IO.Path]::GetFullPath($output))
    } finally {$line.Dispose();$ink.Dispose();$font.Dispose();$g.Dispose();$canvas.Dispose()}
}
