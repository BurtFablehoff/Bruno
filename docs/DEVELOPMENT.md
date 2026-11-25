# Development notes

On Windows 10/11 x64 with the Windows .NET Framework C# compiler:

```powershell
.\build.ps1
.\build.ps1 -Dev
.\tools\Verify.ps1
```

Ordinary builds compile only this repository's `app/` and verified `art/` inputs.
No Python, source photos, parent workspace, other pet checkout, external DLL or
implicit sync is required. Runtime artwork is embedded in each EXE. Shared
framework source is explicitly synced into tracked snapshots before builds.

Installer packaging requires an existing external Inno Setup compiler:

```powershell
.\tools\Build-Installer.ps1 -CompilerPath 'C:\path\to\ISCC.exe'
```

The packager checks product/version identity and rejects developer builds. It
does not install or launch the package. No tool is downloaded automatically.

The checks cover all 50 embedded frames, approved action/posture timing,
2000 placements across monitor geometries, 96 simulated hours, menu/state/input
gates, alpha hit testing, hidden native uploads and identity/startup isolation.
They show no visible companion/tray, write no owner startup/settings and use no
network. Real dragging, menu feel and mixed-DPI changes need owner desktop review.

See [release notes](RELEASE-NOTES.md) and the [changelog](CHANGELOG.md). Developer
builds use separate settings/instance identities and offer direct state/action
controls. Original references, archives, internal planning and draft review
snapshots stay local-only. Small approval records are retained in `art/approvals/`.
Historical art-authoring tools may require ignored local inputs; they are not app
build steps. The installer was integrity-tested and its extracted payload verified;
automated verification did not run the installer or change owner startup/settings.

Mouse-follow selects each approved full PNG through the existing Assets/Placement
renderer. Do not replace these drawings with a cropped head, head layer or pasted
head composite. Their own neck/chest/body pixels and reviewed transforms are part
of the approval in art/approvals/approved-clock-looks-v1.json. The local native
--render-mouse-look export matched all 54 approved light/dark preview frames
pixel for pixel. Mouse tests cover 480 direction cases, stable neutral head anchors,
clock wrapping/hysteresis, sleep/transition/expression exclusion and menu/drag/pause
freezing. Hidden window checks upload all 24 complete gaze sprites and verify
that their hanging paws remain clickable.
