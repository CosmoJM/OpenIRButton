# Incident Response Button: Setup Guide

This guide takes you from download to a fleet-wide rollout. Follow the steps in order and tick the boxes as you go.

For what the app does and what employees see, read the [README](README.md) first. For optional changes (wording, colors, icon), see [docs/CUSTOMIZATION.md](docs/CUSTOMIZATION.md).

**Time needed:** about 30 minutes plus your pilot period.

---

## Contents

- [Before you start](#before-you-start)
- [Step 1 — Edit your contacts](#step-1--edit-your-contacts)
- [Step 2 — Build and test it on your own PC](#step-2--build-and-test-it-on-your-own-pc)
- [Step 3 — Package it for Intune](#step-3--package-it-for-intune)
- [Step 4 — Create the Intune app](#step-4--create-the-intune-app)
- [Step 5 — Pilot, then roll out](#step-5--pilot-then-roll-out)
- [Deploying without Intune](#deploying-without-intune)
- [Changing contacts later — no rebuild needed](#changing-contacts-later--no-rebuild-needed)
- [Shipping a new version of the app itself](#shipping-a-new-version-of-the-app-itself)
- [Removing it](#removing-it)
- [Troubleshooting](#troubleshooting)
- [Appendix: announcement email template](#appendix-announcement-email-template)

---

## Before you start

You need:

- [ ] A **Windows 10 or 11 PC** to build on. Your own workstation is fine. Nothing needs to be installed.
- [ ] **Local administrator** rights on that PC (only to preview with real contacts, and to test `install.ps1`).
- [ ] Your **escalation chain**: for each contact, a role, name, phone number (with country code), email, and an instruction ("if no answer in 10 minutes, go to…").
- [ ] A **monitored mailbox** for contact #1. The **Email IT now** button sends to contact #1.
- [ ] For Intune: rights to create **Win32 apps** in the Intune admin center, and a small **pilot device group**.

Files in this folder:

| File | What it is | Do you edit it? |
|---|---|---|
| `contacts.json` | Organization name and escalation contacts | **Yes** |
| `IncidentButton.cs` | The app's source code | Optional ([CUSTOMIZATION.md](docs/CUSTOMIZATION.md)) |
| `app.ico` | The tray icon | Optional (replace it) |
| `Build.ps1` | Compiles the app | No |
| `install.ps1` | Deploys the app (Intune runs this) | Only `$Version` when shipping a new build |
| `uninstall.ps1` | Removes the app | No |

---

## Step 1 — Edit your contacts

1. Open `contacts.json` in Notepad (right-click → *Open with* → *Notepad*).
2. Replace `Example Corporation` with your organization's name.
3. Replace each example contact with a real one. Change **only the text between the quotes on the right-hand side**:

   ```json
   {
     "order": "1",
     "role": "IT Manager",
     "name": "Jordan Smith",
     "phone": "+1 (555) 555-0101",
     "email": "it-urgent@contoso.com",
     "note": "Call first. If no answer within 10 minutes, go to contact 2."
   },
   ```

4. **To add a contact**, copy a whole block from `{` to `},`, paste it after another one, and edit it.
   **To remove a contact**, delete its whole block from `{` to `}` and its comma.
5. Check the commas: there's a comma **after every contact block except the last one**.

   ```
       },        ← comma: another contact follows
       {
         ...
       }         ← no comma: this is the last contact
     ]
   }
   ```

6. Save. If you used accented letters, use *File → Save As → Encoding: UTF-8*.
7. **Validate it.** Open PowerShell in this folder (in File Explorer, click the address bar, type `powershell` and press Enter; on Windows 11 you can also right-click the folder → *Open in Terminal*) and run:

   ```powershell
   Get-Content .\contacts.json -Raw | ConvertFrom-Json | Select-Object -ExpandProperty contacts | Format-Table order, role, name, phone, email
   ```

   - You see a table of your contacts → ✅ the file is valid.
   - You see red error text → ❌ there's a missing quote or comma near the line it mentions. Fix it and run the command again.

Rules to remember (details in [CUSTOMIZATION.md](docs/CUSTOMIZATION.md#contactsjson-field-by-field)):

- Contacts show in **file order**, top to bottom.
- `role` + `name` together should stay under about 45 characters, because that line doesn't wrap.
- Don't use `{` or `}` inside any value.
- Every contact needs a `name`, or it's skipped.
- **Always run the validation command.** The app's reader is forgiving: a missing comma or quote usually doesn't produce an error message in the app, it just shows wrong or missing text. The validation command is what catches those mistakes.

> **Public repository?** Don't commit your real `contacts.json` to a public GitHub repo. Edit it only on the machine you package from, or keep your organization's copy in a private repository.

- [ ] Contacts edited
- [ ] Validation command shows the right table

---

## Step 2 — Build and test it on your own PC

1. **Put your contacts where the app looks for them.** The app always reads its contacts from `C:\ProgramData\IncidentButton\contacts.json`, not from this folder. Until that file exists, it shows the example contacts. Open PowerShell **as Administrator** in this folder and run:

   ```powershell
   New-Item -ItemType Directory -Force "$env:ProgramData\IncidentButton" | Out-Null
   Copy-Item .\contacts.json "$env:ProgramData\IncidentButton\contacts.json" -Force
   ```

   If you change `contacts.json` again later, run the `Copy-Item` line again. The panel picks up the change the next time you open it.

2. **Build and launch it.** In PowerShell, in this folder, run:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\Build.ps1 -Run
   ```

   You should see `Built: ...\IncidentButton.exe` in green, and a shield appears near the clock.

3. Click the shield and check:
   - [ ] Your organization name is in the window title
   - [ ] Every name, role, number and email is correct, and nothing is cut off on the right
   - [ ] Clicking a phone number opens your calling app (if none is set up, Windows asks which app to use)
   - [ ] Clicking an email opens a new message
   - [ ] **Email IT now** opens a message to contact #1 with your PC name in the subject
   - [ ] **Copy contact list** puts the list on the clipboard (paste into Notepad to check)

4. Close the test copy when you're done:

   ```powershell
   Stop-Process -Name IncidentButton -Force
   ```

**Optional before packaging:**

- **Different icon:** replace `app.ico` (keep the name) and run `Build.ps1` again. See [CUSTOMIZATION.md → Tray icon](docs/CUSTOMIZATION.md#tray-icon).
- **Code signing:** if you use WDAC/AppLocker or want to avoid SmartScreen warnings, sign `IncidentButton.exe` now:
  ```powershell
  Set-AuthenticodeSignature .\IncidentButton.exe -Certificate (Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert)[0] -TimestampServer http://timestamp.digicert.com
  ```

> **Why build first?** `install.ps1` copies the prebuilt `IncidentButton.exe` if it's in the package. If it isn't, it compiles one on each endpoint from `IncidentButton.cs`, which works but is slower and can't be code-signed. Building first is recommended.

---

## Step 3 — Package it for Intune

1. **Make a clean source folder** containing only these files:

   ```
   C:\IRButton\source\
       IncidentButton.exe     ← from Step 2
       IncidentButton.cs      (optional fallback for the build-on-endpoint path)
       app.ico
       contacts.json          ← your real one
       install.ps1
       uninstall.ps1
   ```

   The packaging tool includes **everything** in the folder, so leave out `.git`, docs and anything else.

2. Download the **Microsoft Win32 Content Prep Tool** (`IntuneWinAppUtil.exe`) from
   https://github.com/microsoft/Microsoft-Win32-Content-Prep-Tool

3. Create an empty output folder (e.g. `C:\IRButton\output`) and run:

   ```powershell
   .\IntuneWinAppUtil.exe -c "C:\IRButton\source" -s install.ps1 -o "C:\IRButton\output" -q
   ```

   The result is `C:\IRButton\output\install.intunewin`.

- [ ] `install.intunewin` created

---

## Step 4 — Create the Intune app

Go to **Intune admin center → Apps → Windows → + Add → App type: Windows app (Win32)**, then upload `install.intunewin`.

**App information**

| Field | Value |
|---|---|
| Name | `Cyber Incident Reporting` |
| Description | `Adds a shield icon next to the clock. Click it if you think a cyber incident has happened. It shows exactly who to call, in order.` |
| Publisher | Your organization |
| Show this as a featured app in the Company Portal | No (it's a required app) |

**Program**

| Field | Value |
|---|---|
| Install command | `%windir%\sysnative\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass -NoProfile -File install.ps1` |
| Uninstall command | `%windir%\sysnative\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass -NoProfile -File uninstall.ps1` |
| Installation time required (mins) | 60 (the default is fine) |
| Allow available uninstall | No |
| Install behavior | **System** |
| Device restart behavior | **No specific action** |

> ⚠ **Copy the `sysnative` path exactly.** The Intune agent is a 32-bit process. Plain `powershell.exe` would run 32-bit PowerShell, which can break detection.

**Requirements**

| Field | Value |
|---|---|
| Operating system architecture | 64-bit |
| Minimum operating system | Windows 10 1607 (or your fleet baseline) |

**Detection rules** → *Rules format: Manually configure detection rules* → **+ Add** → Rule type **File**

| Field | Value |
|---|---|
| Path | `C:\Program Files\IncidentButton` |
| File or folder | `version.txt` |
| Detection method | **String comparison** |
| Operator | **Equals** |
| Value | `1.0.0` |
| Associated with a 32-bit app on 64-bit clients | **No** |

> ⚠ **The value must exactly match `$Version` at the top of `install.ps1`.** If they differ, every install "succeeds" and then reports *not detected*.

**Dependencies / Supersedence:** none.

**Assignments** → **Required** → your **pilot group** only for now.

- [ ] Intune app created and assigned to the pilot group

---

## Step 5 — Pilot, then roll out

1. On a pilot PC, make Intune act now instead of waiting for its next check-in. In an **elevated** PowerShell:

   ```powershell
   Restart-Service IntuneManagementExtension
   ```

2. Wait 5–15 minutes, then check:

   ```powershell
   Get-Content "C:\Program Files\IncidentButton\version.txt"            # → 1.0.0
   Get-Content "C:\ProgramData\IncidentButton\contacts.json" -Raw | ConvertFrom-Json | Out-Null   # no output = valid
   Get-Content "$env:LOCALAPPDATA\IncidentButton\activity.log" -Tail 5  # run as the signed-in user → "App started"
   ```

3. Sign out and back in. This is what employees will experience every day.

Pilot checklist:

- [ ] Shield appears without a reboot after install
- [ ] After a sign-out/sign-in, the shield is **on the taskbar**, not hidden behind `^` (Windows 11 22H2+)
- [ ] Panel shows **your** contacts, not "Example Corporation"
- [ ] Phone and email links work on a typical employee PC
- [ ] Intune shows the app as **Installed** for the pilot devices

When the pilot looks good, change the assignment to your full device group and send the [announcement email](#appendix-announcement-email-template).

---

## Deploying without Intune

`install.ps1` works with any tool that can run a PowerShell script **as SYSTEM or as an administrator**, from a folder that also contains `IncidentButton.exe` (or `IncidentButton.cs`), `app.ico` and `contacts.json`. It works from 32-bit or 64-bit PowerShell.

### By hand (one PC)

Elevated PowerShell, in the folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

### RMM tools (NinjaOne, Datto RMM, ConnectWise Automate, N-able, Kaseya, Action1, …)

1. Upload the package folder (or a `.zip` of it) as a file or script attachment.
2. Create a script that runs **as SYSTEM**, extracts/copies the files to a temp folder, and runs:
   ```powershell
   powershell.exe -ExecutionPolicy Bypass -NoProfile -File "<temp folder>\install.ps1"
   ```
3. To check whether a PC needs it, test:
   ```powershell
   (Get-Content "$env:ProgramW6432\IncidentButton\version.txt" -ErrorAction SilentlyContinue) -eq '1.0.0'
   ```

### Configuration Manager (SCCM / MECM)

Create an **Application** with a **Script Installer** deployment type:

| Field | Value |
|---|---|
| Content location | Your package folder on a share |
| Installation program | `powershell.exe -ExecutionPolicy Bypass -NoProfile -File install.ps1` |
| Uninstall program | `powershell.exe -ExecutionPolicy Bypass -NoProfile -File uninstall.ps1` |
| Installation behavior | Install for system |
| Logon requirement | Whether or not a user is logged on |
| Detection method | *Use a custom script* (PowerShell): |

```powershell
if ((Get-Content "$env:ProgramW6432\IncidentButton\version.txt" -ErrorAction SilentlyContinue) -eq '1.0.0') { 'Installed' }
```

### Group Policy (domain-joined PCs)

1. Copy the package folder to a share that **Domain Computers** can read, e.g. `\\fileserver\deploy$\IncidentButton\`.
2. In a GPO linked to your workstations: **Computer Configuration → Policies → Windows Settings → Scripts (Startup/Shutdown) → Startup → Add**
   - Script name: `powershell.exe`
   - Script parameters: `-ExecutionPolicy Bypass -NoProfile -File "\\fileserver\deploy$\IncidentButton\install.ps1"`
3. The app is installed at the next restart and appears at the next sign-in.
4. Recommended: in the same GPO, enable **Computer Configuration → Policies → Administrative Templates → System → Logon → Always wait for the network at computer startup and logon**, so that the share is reachable when the startup script runs.

> With Group Policy, `install.ps1` runs at every startup. That's harmless, and it also means updating `contacts.json` on the share updates every PC at its next restart.

---

## Changing contacts later — no rebuild needed

The app reads `C:\ProgramData\IncidentButton\contacts.json` **every time the panel opens**, so to change a number for the whole fleet you only replace that file. No reinstall, version bump or restart is needed.

**With Intune:** *Devices → Scripts and remediations → Platform scripts → + Add → Windows 10 and later*, with this script. Settings: *Run this script using the logged on credentials* = **No**, *Run script in 64 bit PowerShell Host* = **Yes**.

```powershell
$dir = "$env:ProgramData\IncidentButton"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
Set-Content -Path "$dir\contacts.json" -Encoding UTF8 -Value @'
<paste the FULL contents of your updated, validated contacts.json here>
'@
```

> Platform scripts run once per device. For the next change, edit the script with the new contents; Intune re-runs it when the script content changes.

> The **contact cards and window title** update the next time the panel opens. The **tray tooltip and About box** read the organization name when the app starts, so a changed organization name shows there after the user's next sign-in.

Also put the updated `contacts.json` in your package source folder, so that new installs get the current list.

**With other tools:** copy the new file to `C:\ProgramData\IncidentButton\contacts.json` as SYSTEM/admin.

---

## Shipping a new version of the app itself

Only needed when you change `IncidentButton.cs` or `app.ico`.

1. Make the change, then run `.\Build.ps1` (and sign it, if you sign).
2. In `install.ps1`, raise `$Version` (e.g. `1.0.0` → `1.0.1`).
3. Repackage with `IntuneWinAppUtil` (Step 3).
4. In Intune, open the app → *Properties*:
   - **App information** → upload the new `.intunewin`
   - **Detection rules** → change the value to the new version (`1.0.1`)

PCs on the old version then fail detection, and Intune reinstalls over the top. `install.ps1` stops the running copy first.

---

## Removing it

- **Intune:** change the assignment from *Required* to *Uninstall* for the target group.
- **Any other tool / by hand:** run `uninstall.ps1` as SYSTEM or administrator.

`uninstall.ps1` stops the app and removes the program folder, the shared `contacts.json` and the sign-in entry. Per-user activity logs in each user's `%LOCALAPPDATA%\IncidentButton\` are left in place as a record.

---

## Troubleshooting

| What you see | Why | Fix |
|---|---|---|
| Panel shows **Example Corporation** / example contacts | `C:\ProgramData\IncidentButton\contacts.json` is missing, or so damaged that no contact in it can be read | Check the file exists, then run `Get-Content "C:\ProgramData\IncidentButton\contacts.json" -Raw \| ConvertFrom-Json` on that PC. Fix the error it reports, or re-push the file |
| Wrong, missing or merged text on a card | A missing quote or comma in `contacts.json`. The app's reader is forgiving, so it shows what it could read instead of an error | Run the validation command from [Step 1](#step-1--edit-your-contacts) and fix what it reports |
| You edited `contacts.json` but nothing changed | You edited the copy in your build folder. The app only reads the one in `C:\ProgramData\IncidentButton\` | Copy it there (elevated) or push it ([Changing contacts later](#changing-contacts-later--no-rebuild-needed)) |
| Some contacts are missing | That contact has an empty `name`, or a `{`/`}` inside a value | Fill in `name`; remove braces from values |
| Contacts in the wrong order | Cards follow **file order**, not the `order` field | Reorder the blocks in the file |
| Heading text runs off the edge of a card | `role` + `name` too long | Shorten them; move detail into `note` |
| Accented characters look garbled | File saved in ANSI encoding | Save as UTF-8 |
| Clicking a phone number asks which app to use, or does nothing useful | No app on the PC is set up for `tel:` links. (If Windows can't open the link at all, the app copies the number to the clipboard instead) | Set a default calling app (Teams, Phone Link…) under *Settings → Apps → Default apps → Choose defaults by link type → TEL* |
| **Email IT now** goes to the wrong person | It always sends to contact #1's email | Put a monitored address on contact #1, or hard-code one ([CUSTOMIZATION.md](docs/CUSTOMIZATION.md#email-it-now-recipient-subject-body)) |
| Intune: *"The application was not detected after installation completed successfully"* | Detection value ≠ `$Version` in `install.ps1`, or the install command isn't using `sysnative` | Make the two versions match exactly; copy the install command from Step 4 |
| Intune: install fails | Execution policy or a missing file in the package | Check `IntuneManagementExtension.log` and `AppWorkload.log` in `C:\ProgramData\Microsoft\IntuneManagementExtension\Logs\`; make sure the package folder had all files |
| Icon hidden behind the `^` arrow | Windows creates the taskbar entry the first time the app runs; the app pins itself once that exists | Sign out and in again. Users can also drag it onto the taskbar. Pre-22H2 Windows 11 and Windows 10 don't support auto-pinning |
| Icon doesn't appear right after install | The installer couldn't launch the app in the user's session | It appears at the next sign-in |
| `Build.ps1`: *"C# compiler not found"* | .NET Framework 4 compiler missing (very rare) | Check `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` exists; enable .NET Framework 4.x in *Windows Features* |
| `Build.ps1` won't run: *"running scripts is disabled"* | PowerShell execution policy | Use `powershell -ExecutionPolicy Bypass -File .\Build.ps1 -Run` |
| Packaging tool: *folder can't be accessed* | A PowerShell or Explorer window is open inside that folder, or a typo in the path | `cd C:\` in open shells, close Explorer windows on that folder |
| *Access denied* writing to `Program Files` / `ProgramData` | Not elevated | Use PowerShell **as Administrator** |
| SmartScreen / AppLocker / WDAC blocks the `.exe` | Unsigned binary | Sign it (Step 2) and allow your signing certificate |

**Useful locations on an endpoint:**

| What | Where |
|---|---|
| Program | `C:\Program Files\IncidentButton\IncidentButton.exe` |
| Installed version | `C:\Program Files\IncidentButton\version.txt` |
| Contacts the app shows | `C:\ProgramData\IncidentButton\contacts.json` |
| Activity log (per user) | `%LOCALAPPDATA%\IncidentButton\activity.log` |
| Starts at sign-in via | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` → `IncidentButton` |

The activity log records app starts, panel opens, button and link clicks, and when the icon was pinned to the taskbar. Example:

```
2026-10-16 09:12:03  jsmith  App started
2026-10-16 09:12:13  jsmith  Tray icon promoted to taskbar
2026-10-16 14:40:51  jsmith  Incident panel opened
2026-10-16 14:41:07  jsmith  Phone clicked for contact 1 (Jordan Smith)
```

---

## Appendix: announcement email template

Send this when you roll out to everyone, then edit it to suit.

> **Subject:** New: the red shield next to your clock
>
> Hi all,
>
> You'll now see a small **red shield** next to the clock at the bottom-right of your screen.
>
> **If you think something is wrong** — a suspicious email you clicked, a strange pop-up, files you can't open, a message demanding payment, your account doing things you didn't do — **click the shield**. It shows exactly who to call, in order, with their phone numbers.
>
> - Don't turn off or unplug your computer unless we ask you to.
> - When in doubt, report it. You will never be in trouble for reporting something that turns out to be nothing.
>
> Thanks,
> IT
