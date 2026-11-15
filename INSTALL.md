# Installing Bruno on Windows

These instructions apply to **Bruno 0.1.2** on Windows 10/11 x64. The installer
and portable EXE are **not digitally signed**. Windows may show an unfamiliar-app
or unknown-publisher warning. The checks below help distinguish that warning
from an actual antivirus detection.

## 1. Download the installer

1. Open the [official Bruno release](https://github.com/BurtFablehoff/Bruno/releases/tag/v0.1.2).
2. Under **Assets**, download [Bruno-Setup-0.1.2.exe](https://github.com/BurtFablehoff/Bruno/releases/download/v0.1.2/Bruno-Setup-0.1.2.exe).
   This is the normal installer. **Source code (zip)** is for developers.
3. Open your Downloads folder in File Explorer. Leave the installer there while
   checking it below. If Bruno is already running, click him and choose **Close**.

The official repository is **github.com/BurtFablehoff/Bruno**. Download from its
release page rather than a repost or an unsolicited attachment.

## 2. Verify the download

Download [SHA256SUMS.txt](https://github.com/BurtFablehoff/Bruno/releases/download/v0.1.2/SHA256SUMS.txt)
from the same release. Open PowerShell and run this read-only command, adjusting
the path if your download is elsewhere:

```powershell
Get-FileHash -LiteralPath "$env:USERPROFILE\Downloads\Bruno-Setup-0.1.2.exe" -Algorithm SHA256
```

Compare the **Hash** value with this installer hash, or with the matching filename
in SHA256SUMS.txt. Uppercase and lowercase letters are equivalent.

| File | SHA-256 for 0.1.2 |
| --- | --- |
| Bruno-Setup-0.1.2.exe | `f9c976ba92667e11da49405dcd4bd98a308ccb78b712de4c964eef830501f76a` |
| Bruno.exe | `ee043e9649b573be74eda78155da2ee7eb13cd1e92bf3c3efbc65dc6d38088f6` |

**If the hash differs, stop and download again from the official release.** A
matching hash establishes that you have the published file; it does not prove
that a program is harmless or replace an antivirus scan. You can use File
Explorer's **Scan with Microsoft Defender** option if it is available; on Windows
11 it may be under **Show more options**.

## 3. If Windows says “Windows protected your PC”

Microsoft Defender SmartScreen checks download reputation. A new unsigned file
can trigger an unrecognized-app warning; this is different from a named malware
detection. Microsoft explains this in its [SmartScreen reputation guidance](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation).

If the warning says the app is unrecognized, you verified the official download,
and you choose to trust this release:

1. Check that you opened **Bruno-Setup-0.1.2.exe**.
2. Select **More info** on the warning.
3. Windows may show **Unknown publisher**, because this release is unsigned.
4. Select **Run anyway**, if Windows offers it, to allow this launch.

The same warning may appear when launching the installed **Bruno.exe** for the
first time. Apply the same file-specific checks. You do not need to turn off
Defender, real-time protection, or SmartScreen, or add a folder exclusion.

If your browser only warns that a download is uncommon, inspect its source and
the warning details before using any **Keep** option. An explicit malware or
potentially-unwanted-app warning belongs to the next section instead.

## 4. If Defender reports a threat or removes the file

A named threat, quarantine, or removed download is **not** the ordinary
SmartScreen unfamiliar-app prompt. Do not assume it is a false positive just
because Bruno is unsigned or because the checksum matches.

1. Open **Start → Windows Security → Virus & threat protection → Protection history**.
2. Expand the Bruno-related event and note its **threat name**, affected filename,
   and action. Windows may require administrator access to inspect details.
3. Leave an uncertain file quarantined. Update Defender's security intelligence
   and scan again when the file is available for analysis.
4. Report the version, detection name, release URL and file hash through
   [Bruno Issues](https://github.com/BurtFablehoff/Bruno/issues). Suspected false
   positives can also be submitted through [Microsoft's file-analysis portal](https://www.microsoft.com/en-us/wdsi/filesubmission).
5. Only after establishing that the particular detection is a false positive,
   consider the event's file-specific **Allow on device** action. If the event
   is quarantined, **Restore** may cause detection again; restoration alone is
   not an allow decision. A removed download may need downloading again after
   an allow decision. Do not allow unrelated events or exclude all Downloads.

See Microsoft's [Protection History instructions](https://support.microsoft.com/en-us/windows/security/windows-security/protection-history-in-the-windows-security-app)
for event types and actions. The project's build/integrity checks are not a
Microsoft antivirus approval, and this release does not claim one.

## 5. If “Run anyway” is missing or Smart App Control blocks it

Work/school device policies can prevent continuation. Ask the device administrator
for an approved installation route rather than changing organization controls.

Windows 11 **Smart App Control** is a separate feature. Microsoft currently
provides no per-app exception for it. Do not disable it to install Bruno; report
the block and wait for a compatible release or an administrator-approved option.
See the [Smart App Control FAQ](https://support.microsoft.com/en-us/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions).

## 6. Complete installation and launch

1. Double-click **Bruno-Setup-0.1.2.exe** and work through any applicable warning
   above. The installer installs for your account and does not require **Run as
   administrator**.
2. Keep the default installation folder unless you have a reason to change it:
   `%LOCALAPPDATA%\Programs\Bruno`.
3. Choose whether to create a desktop shortcut. This option is unchecked by default.
4. Select **Install**, then **Finish**. Leave **Launch Bruno** selected if you
   want him to appear immediately. Later, search for **Bruno** in Start.
5. Bruno should appear at the desktop/taskbar edge, with a tray icon available
   in the notification area (possibly under its hidden-icons arrow).

Click visible fur to open the menu. Choose **Size → Small** for the smallest
size; existing saved settings may retain a larger size. Left-drag to reposition.
Use **Bring to this screen** if he is on another display. Click elsewhere to
dismiss the menu, and use **Close** to quit.

**Start with Windows is off by default.** Enable it in Bruno's menu only if you
want automatic startup. Automatic updates and Check for updates are greyed out
in 0.1.2: that build has no update feed configured. Publishing this repository
does not change the settings compiled into an existing EXE. Use the release page
for manual downloads until a later build enables updating.

## Portable option

Download [Bruno.exe](https://github.com/BurtFablehoff/Bruno/releases/download/v0.1.2/Bruno.exe),
verify its hash above, and place it in a permanent folder you can write to, such
as `Documents\Bruno`. Double-click it; no installation or extraction is needed.
The same Windows warning guidance applies. Do not run the installed and portable
production copies together. Close the current one before switching versions.

If you enable startup for a portable copy, keep the EXE in that location. If you
move it, turn startup off first and enable it again from its new location.

## Updating or uninstalling

For a manual update, close Bruno, download and verify the newer official release,
then run its installer. For a portable update, replace the EXE in its permanent
folder after closing the old process. Existing size/position preferences are kept.

To uninstall the installed version, close Bruno, search Start for **Add or remove
programs**, find **Bruno**, and choose **Uninstall**. Its uninstaller removes the
matching Bruno startup entry. Preferences in `%LOCALAPPDATA%\BrunoCompanionV1`
are retained. For a portable copy, disable **Start with Windows**, close Bruno,
then remove the portable EXE. These instructions do not require changing Windows
security settings.
