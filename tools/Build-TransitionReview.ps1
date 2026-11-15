$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$manifestPath = Join-Path $root 'planning/transition-sources-v1.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$exe = Join-Path $root 'test-exes/Bruno-TransitionReview-v1.exe'
$output = Join-Path $root 'build/transitions-review-v1'
New-Item -ItemType Directory -Force -Path (Split-Path $exe),$output | Out-Null
$compilerArgs = @('/nologo','/target:winexe','/optimize+','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Web.Extensions.dll',('/out:'+$exe),('/resource:'+$manifestPath+',BrunoTransitions.manifest'))
foreach ($asset in $manifest.assets) {
    $file = Join-Path $root $asset.file
    if ((Get-FileHash -LiteralPath $file).Hash.ToLowerInvariant() -ne $asset.sha256) {throw "Source hash mismatch: $file"}
    $compilerArgs += ('/resource:'+$file+',BrunoTransitions.'+$asset.id)
}
$compilerArgs += (Join-Path $PSScriptRoot 'TransitionReview.cs')
& $compiler @compilerArgs
if ($LASTEXITCODE -ne 0) {throw 'Transition review compilation failed.'}
$process = Start-Process -FilePath $exe -ArgumentList @('--export',('"'+$output+'"')) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) {throw "Transition export failed; inspect $output/error.txt"}
foreach ($mode in @('light','dark')) {
    & (Join-Path $PSScriptRoot 'SharedWrite-AnimatedGif.ps1') -FramesDirectory (Join-Path $output ($mode+'-frames')) -OutputPath (Join-Path $output ($mode+'.gif')) -Delays $manifest.delays
}
Get-Content (Join-Path $output 'checks.json')
Write-Output "Review ready: $exe"
