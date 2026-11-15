# Bruno desktop companion

<p align="center"><img src="art/frames/sit.png" alt="Bruno the fawn pug sitting" width="260"></p>

Bruno is a quiet Windows desktop companion with approved sitting, lying and
head-down sleeping poses, posture transitions, blinks, head tilts and tongue peeks.
Version **0.1.2** includes owner-approved artwork, animations and body contact at
the taskbar edge. It supports Windows 10/11 x64.

Size choices are Small (new, two-thirds the former Small height), Normal (former
Small), Large (former Normal), and Extra Large (former Large). New settings default
to Small. Existing saved physical sizes are retained; choose Size > Small to shrink.

## Run

**[Detailed Windows installation guide, including Defender and SmartScreen warnings](INSTALL.md)**

Download [**Bruno-Setup-0.1.2.exe**](https://github.com/BurtFablehoff/Bruno/releases/download/v0.1.2/Bruno-Setup-0.1.2.exe)
for per-user installation, shortcuts and an uninstaller. An optional desktop
shortcut is unchecked by default. No administrator rights are required.

Prefer a portable copy? Download [**Bruno.exe**](https://github.com/BurtFablehoff/Bruno/releases/download/v0.1.2/Bruno.exe),
keep it in a writable folder and run it. Quit an older Bruno before opening a
new version. See the [release notes](RELEASE-NOTES.md) and
[SHA-256 checksums](https://github.com/BurtFablehoff/Bruno/releases/download/v0.1.2/SHA256SUMS.txt).

Click visible fur with either mouse button to open the menu. Drag with the left
button to reposition. Clicking outside the menu dismisses it. Production offers
Sit/Lay Down, size, screen controls, opt-in Start with Windows and Close; sleeping
is chosen automatically. Actions stay in the current awake pose. The transparent
window avoids taking focus. The tray icon provides access if Bruno is obscured.

The taskbar/floor edge supports his butt while seated and his chest/elbows while
lying awake or sleeping. His front paws hang in front of the edge and remain
clickable. Pose/action artwork and timing are unchanged; this is body placement.

![Bruno sitting, lying and sleeping at the taskbar edge](docs/images/bruno-taskbar.png)

Automatic updating is disabled in this first release; download newer versions
from [Releases](https://github.com/BurtFablehoff/Bruno/releases). Starting with
Windows is off by default; enable it through the production menu. Settings live under
`%LOCALAPPDATA%/BrunoCompanionV1` (development: `BrunoCompanionDevV1`).

## Build and verify

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

The checks cover all 21 embedded frames, approved action/posture timing,
840 placements across monitor geometries, 96 simulated hours, menu/state/input
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
