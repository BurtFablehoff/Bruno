$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$output=Join-Path $root 'build/release-preview'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$manifestPath=Join-Path $root 'art/manifest.json'
$manifest=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$exe=Join-Path $output 'Render-ReleasePreview.exe'
$arguments=@('/nologo','/target:winexe','/platform:x64','/main:Bruno.ReleasePreview','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Web.Extensions.dll',('/out:'+$exe),('/resource:'+$manifestPath+',Bruno.Manifest'))
foreach($entry in $manifest.frames.PSObject.Properties) {
    $path=Join-Path $root ('art/'+$entry.Value.file)
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.Value.sha256) { throw "Artwork digest differs: $($entry.Name)" }
    $arguments+='/resource:'+$path+',Bruno.Art.'+$entry.Name
}
$sources=@(Get-ChildItem -LiteralPath (Join-Path $root 'app') -Filter '*.cs' | ForEach-Object {$_.FullName})
& $compiler @arguments @sources (Join-Path $PSScriptRoot 'Render-ReleasePreview.cs')
if ($LASTEXITCODE -ne 0) { throw 'Preview renderer compilation failed' }
$frames=Join-Path $output 'frames'
$process=Start-Process -FilePath $exe -ArgumentList ('"'+$frames+'"') -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw 'Preview rendering failed' }
$delays=@(Get-Content -LiteralPath (Join-Path $frames 'delays.json') -Raw | ConvertFrom-Json)
& (Join-Path $PSScriptRoot 'SharedWrite-AnimatedGif.ps1') -FramesDirectory $frames -OutputPath (Join-Path $root 'docs/images/bruno-motion.gif') -Delays $delays
