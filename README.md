# Incident Response Button

A lightweight Windows system tray app that shows employees exactly who to call during a cyber incident — in escalation order, with click-to-call and click-to-email links.

No installer framework, no SDK, no dependencies. It compiles with the C# compiler already built into Windows and deploys through Intune or any tool that can run a PowerShell script.

<!-- Recommended: take a screenshot of the panel with example contacts, save it as
     docs/screenshot.png, and uncomment the line below.
![The panel listing escalation contacts in order](docs/screenshot.png)
-->


## Why

During an incident, the slowest step is often an employee deciding whether something is worth reporting and then hunting for the right phone number. This puts the answer one click away, on every endpoint, with no login and no portal.

It also serves as an auditable control artifact: a persistent, documented incident-reporting mechanism on every workstation, with a local activity log. That maps cleanly to NIST SP 800-171 3.6.1 and 3.6.2 (incident-handling capability and incident reporting).

## Features

- Shield icon in the notification area, started automatically at every sign-in
- Click opens a panel with your escalation chain (1 → 2 → 3 → …), each with tap-to-call and mail links
- Contacts live in a JSON file read at runtime — update the whole fleet without recompiling or reinstalling
- Pins itself to the taskbar rather than hiding behind the overflow arrow (Windows 11 22H2+)
- "Email IT now" button pre-fills a report with the username and machine name
- Local activity log of opens and clicks
- No network code — nothing is transmitted anywhere

## Quick start

```powershell
# 1. Edit contacts.json with your organization and escalation chain
# 2. Build and run it
.\Build.ps1 -Run
```

Full deployment instructions, including Intune packaging and the detection rule, are in **[SETUP-GUIDE.md](SETUP-GUIDE.md)**.

## Repository layout

| File | Purpose |
|---|---|
| `contacts.json` | Organization name and escalation contacts — the only file most people edit |
| `IncidentButton.cs` | Application source (single file, WinForms) |
| `app.ico` | Tray icon — replace with your own |
| `Build.ps1` | Compiles `IncidentButton.exe` |
| `install.ps1` | Deployment script (Intune runs this as SYSTEM) |
| `uninstall.ps1` | Removal script |
| `SETUP-GUIDE.md` | Step-by-step setup and troubleshooting |

## Requirements

- Windows 10 or 11, 64-bit
- .NET Framework 4.x (present on all supported Windows builds — nothing to install)
- Taskbar pinning requires Windows 11 22H2 or later; on older builds the app works normally, the icon just stays in the overflow area

## Configuration

Everything an organization needs to change lives in `contacts.json`:

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

The app reads `C:\ProgramData\IncidentButton\contacts.json` every time the panel opens, so pushing an updated file changes the escalation chain fleet-wide with no reinstall. Values compiled into the executable are used only if that file is missing or unreadable.

## Building

`Build.ps1` invokes the .NET Framework compiler directly:

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
    /target:winexe /platform:anycpu `
    /win32icon:app.ico /resource:app.ico,app.ico `
    /out:IncidentButton.exe IncidentButton.cs
```

The compiled binary is intentionally not committed. Build from source, or attach a signed build to a GitHub Release.

## License

MIT — see [LICENSE](LICENSE).
