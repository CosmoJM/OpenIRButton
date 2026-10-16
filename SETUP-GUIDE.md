# Incident Response Button — Setup Guide

A small system tray app for Windows. Employees click a shield icon near the clock and immediately see who to call during a cyber incident, in escalation order, with click-to-call and click-to-email links.

No installer frameworks, no SDK, no licensing. It compiles with the C# compiler already built into Windows and deploys through Intune (or any tool that can run a PowerShell script).

---

## What's in this folder

| File | What it is |
|---|---|
| `contacts.json` | **The only file you must edit.** Your organization name and escalation contacts. |
| `IncidentButton.cs` | The app's source code. You don't normally need to touch it. |
| `app.ico` | The tray icon. Replace with your own if you want. |
| `Build.ps1` | Compiles the app. One command. |
| `install.ps1` | Deploys the app. Intune runs this. |
| `uninstall.ps1` | Removes the app. Intune runs this. |
| `SETUP-GUIDE.md` | This file. |

---

## Step 1 — Edit your contacts (5 minutes)

Open `contacts.json` in Notepad and replace the example values.

```json
{
  "organization": "Your Company Name",

  "contacts": [
    {
      "order": "1",
      "role": "IT Manager",
      "name": "Your Name",
      "phone": "+1 (555) 555-0101",
      "email": "you@yourcompany.com",
      "note": "Call first. If no answer within 10 minutes, go to contact 2."
    }
  ]
}
```

Rules:
- Keep every quote mark and comma exactly where they are. A missing comma makes the file unreadable and the app falls back to the example data.
- **No comma after the last contact**, before the closing `]`.
- You can have as many contacts as you like. The window grows to fit them.
- Keep `note` text reasonably short — it wraps, but a paragraph will make a tall window.
- Leave the `_README` line alone or delete it; either is fine.

Optional: open `IncidentButton.cs` and change `AssemblyCompany("Example Corporation")` near the top to your company name. This only affects what shows in file properties.

---

## Step 2 — Build it (1 minute)

Open PowerShell in this folder and run:

```powershell
.\Build.ps1 -Run
```

That compiles `IncidentButton.exe` and launches it so you can see it. A shield icon appears near the clock — click it and check that your contacts look right.

If PowerShell blocks the script, run it this way instead:

```powershell
powershell -ExecutionPolicy Bypass -File .\Build.ps1 -Run
```

To close the app while testing: `Stop-Process -Name IncidentButton -Force`

**Want a different icon?** Replace `app.ico` with your own `.ico` file (keep the name `app.ico`) and build again. Use an icon that contains a 16×16 image, or it will look soft in the tray.

---

## Step 3 — Package it for Intune (5 minutes)

1. Download the **Microsoft Win32 Content Prep Tool**:
   https://github.com/microsoft/Microsoft-Win32-Content-Prep-Tool

2. Run it:

```powershell
.\IntuneWinAppUtil.exe -c "C:\path\to\this\folder" -s install.ps1 -o "C:\path\to\output"
```

   Or run it with no arguments and answer the three prompts:
   - Source folder: the folder containing these files
   - Setup file: `install.ps1`
   - Output folder: any existing folder

   It packages **everything in the source folder**, so delete anything you don't want on your endpoints first. The output is `install.intunewin`.

---

## Step 4 — Create the Intune app (10 minutes)

**Intune → Apps → Windows → Add → Windows app (Win32)** and upload `install.intunewin`.

**App information**
- Name: `Cyber Incident Reporting`
- Description: `Adds a shield icon to your system tray. Click it if you think a cyber incident has happened — it shows exactly who to call, in order.`
- Publisher: your company

**Program**

| Field | Value |
|---|---|
| Install command | `%windir%\sysnative\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass -NoProfile -File install.ps1` |
| Uninstall command | `%windir%\sysnative\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass -NoProfile -File uninstall.ps1` |
| Install behavior | **System** |
| Device restart behavior | No specific action |

> Use the `sysnative` path, not plain `powershell.exe`. Intune's agent is a 32-bit process, and plain PowerShell would install the app into `Program Files (x86)` where the detection rule won't find it.

**Requirements**
- OS architecture: 64-bit
- Minimum OS: Windows 10 1607 (or your fleet's baseline)

**Detection rules** → *Manually configure detection rules* → Add → **File**

| Field | Value |
|---|---|
| Path | `C:\Program Files\IncidentButton` |
| File | `version.txt` |
| Detection method | **String comparison** → **Equals** → `1.0.0` |
| Associated with a 32-bit app | **No** |

> Match this string to `$Version` at the top of `install.ps1`. If the two ever disagree, every install will "succeed" and then report failure, because detection can never pass.

**Assignments** → Required → a small pilot group first.

---

## Step 5 — Test, then roll out

Pick one or two pilot machines. To make Intune act immediately instead of waiting for its check-in cycle, run this on the target machine in an **elevated** PowerShell:

```powershell
Restart-Service IntuneManagementExtension
```

Wait a few minutes, then verify:

```powershell
Get-Content "C:\Program Files\IncidentButton\version.txt"
Get-Content "$env:LOCALAPPDATA\IncidentButton\activity.log" -Tail 5
```

You want the version number you set, and an `App started` line. Then sign out and back in — that's the real end-user experience. The shield should be sitting on the taskbar when the desktop finishes loading.

Checklist:
- [ ] Icon appears in the tray without a reboot
- [ ] It's pinned on the taskbar (not hidden behind the `^` arrow) after a sign-in
- [ ] Panel shows the right contacts, nothing cut off
- [ ] Phone and email links work
- [ ] Icon returns automatically after a sign-out/sign-in

When the pilot looks good, change the assignment to your full device group and send a short email telling people what the shield is and when to click it.

---

## Changing contacts later — no rebuild needed

The app reads `C:\ProgramData\IncidentButton\contacts.json` every time the panel opens. To change a phone number across the fleet, push the new file with an Intune **Platform script** (run as System):

```powershell
$dir = "$env:ProgramData\IncidentButton"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
Set-Content -Path "$dir\contacts.json" -Encoding UTF8 -Value @'
<paste the full contents of your updated contacts.json here>
'@
```

It takes effect the next time anyone opens the panel. No reinstall, no version bump, no restart.

---

## Shipping a new version of the app itself

Only needed when you change the code or the icon.

1. Edit, then `.\Build.ps1`
2. Change `$Version` in `install.ps1` (e.g. `1.0.1`)
3. Repackage with `IntuneWinAppUtil`
4. In Intune: upload the new package **and** change the detection rule string to the new version

Machines on the old version then fail detection and update themselves.

---

## Troubleshooting

| What you see | Why | Fix |
|---|---|---|
| Panel shows the example contacts | `contacts.json` is missing from `C:\ProgramData\IncidentButton\`, or has a JSON syntax error | Check the file exists and that every comma and quote is correct |
| You edited `contacts.json` but nothing changed | You edited the copy in your build folder. The app only reads the one in `C:\ProgramData\IncidentButton\` | Copy it there (elevated), or reinstall |
| Intune: "not detected after installation completed successfully" | Detection rule version doesn't match `$Version` in `install.ps1`, or the install command isn't using `sysnative` | Make the two versions match; use the sysnative command |
| Icon is hidden behind the `^` arrow | Windows creates the tray entry lazily on first run | It pins itself at the next sign-in. Users can also drag it onto the taskbar once |
| Icon doesn't appear right after install | The app couldn't launch into the user's session | It starts at the next sign-in |
| Packaging tool says the folder can't be accessed | A PowerShell window or Explorer window is sitting inside that folder, or the path was typed wrong | `cd C:\` in any open shells, close Explorer windows on that folder |
| `Access denied` copying into `Program Files` or `ProgramData` | Not elevated | Use an Administrator PowerShell window |

**Activity log** (useful when diagnosing a single machine):
`%LOCALAPPDATA%\IncidentButton\activity.log` — records app starts, panel opens, and which contact was clicked.

---

## Notes

- Windows 10 and 11, 64-bit. The taskbar pinning behavior requires Windows 11 22H2 or later; on older builds the app works fine, the icon just stays in the overflow area.
- The app runs in the signed-in user's context and only ever writes to that user's own registry and log file.
- Employees can't close it permanently — there's no Exit option — and it restarts at every sign-in.
- Nothing is sent anywhere. The app has no network code; it only opens the user's phone and mail handlers.
