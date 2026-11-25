$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
& (Join-Path $root 'build.ps1')
& (Join-Path $root 'build.ps1') -Dev
New-Item -ItemType Directory -Force -Path (Join-Path $root 'build') | Out-Null
foreach ($variant in @('production','dev')) {
    $name = if ($variant -eq 'dev') {'Bruno-Dev-0.1.5.exe'} else {'Bruno-0.1.5.exe'}
    foreach ($mode in @('self-test','test-desktop','test-product')) {
        $report = Join-Path $root ('build/'+$mode+'-'+$variant+'-0.1.5.json')
        $process = Start-Process -FilePath (Join-Path $root ('test-exes/'+$name)) -ArgumentList @(('--'+$mode),('"'+$report+'"')) -WindowStyle Hidden -Wait -PassThru
        if ($process.ExitCode -ne 0) {throw "Failed $variant $mode; inspect $report.error.txt"}
        Write-Output "Passed: $variant $mode"
    }
}
$production = Join-Path $root 'test-exes/Bruno-0.1.5.exe'
$cycle = Join-Path $root 'build/runtime-cycle-0.1.5'
$process = Start-Process -FilePath $production -ArgumentList @('--render-pose-cycle',('"'+$cycle+'"')) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) {throw 'Runtime cycle render failed.'}
$delays = @(Get-Content -LiteralPath (Join-Path $cycle 'delays.json') -Raw | ConvertFrom-Json)
& (Join-Path $PSScriptRoot 'SharedWrite-AnimatedGif.ps1') -FramesDirectory $cycle -OutputPath (Join-Path $root 'build/runtime-cycle-0.1.5.gif') -Delays $delays
$menu = Join-Path $root 'build/production-menu-0.1.5.png'
$process = Start-Process -FilePath $production -ArgumentList @('--render-menu',('"'+$menu+'"')) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) {throw 'Menu render failed.'}
$contact = Join-Path $root 'build/body-contact-0.1.5.png'
$process = Start-Process -FilePath $production -ArgumentList @('--render-contact',('"'+$contact+'"')) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) {throw 'Body contact render failed.'}
Write-Output 'Verified both variants without showing UI, writing user settings/startup, or using the network.'
