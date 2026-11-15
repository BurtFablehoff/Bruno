$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$manifest = Get-Content -LiteralPath (Join-Path $root 'planning/action-sources-v1.json') -Raw | ConvertFrom-Json
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$exe = Join-Path $root 'test-exes/Bruno-ActionReview-v1.exe'
$output = Join-Path $root 'build/actions-review-v1'
New-Item -ItemType Directory -Force -Path (Split-Path $exe),$output | Out-Null
$compilerArgs = @('/nologo','/target:winexe','/optimize+','/r:System.Drawing.dll','/r:System.Windows.Forms.dll',('/out:'+$exe))
$inputs = @(@{id='sit';file='draft-assets/approved-rest-v1/sit.png'},@{id='lay';file='draft-assets/approved-rest-v1/lay.png'}) + @($manifest.assets)
foreach ($inputFile in $inputs) {
    $file = Join-Path $root $inputFile.file
    if ($inputFile.sha256 -and (Get-FileHash -LiteralPath $file).Hash.ToLowerInvariant() -ne $inputFile.sha256) {throw "Source hash mismatch: $file"}
    $compilerArgs += ('/resource:'+$file+',BrunoReview.'+$inputFile.id)
}
$compilerArgs += (Join-Path $PSScriptRoot 'ActionReview.cs')
& $compiler @compilerArgs
if ($LASTEXITCODE -ne 0) {throw 'Action review compilation failed.'}
$process = Start-Process -FilePath $exe -ArgumentList @('--export',('"'+$output+'"')) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) {throw "Action review export failed; inspect $output/error.txt"}
$delays = @((Get-Content (Join-Path $output 'delays.txt') -Raw).Split(',') | ForEach-Object {[int]$_})
foreach ($mode in @('body','detail')) {
    & (Join-Path $PSScriptRoot 'SharedWrite-AnimatedGif.ps1') -FramesDirectory (Join-Path $output ($mode+'-frames')) -OutputPath (Join-Path $output ($mode+'.gif')) -Delays $delays
}
Get-Content (Join-Path $output 'checks.json')
Write-Output "Review ready: $exe"
