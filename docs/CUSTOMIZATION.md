# Customization Reference

Every change you can make, where to make it, and what it affects.

**You only have to edit `contacts.json`.** Everything below is optional. Line numbers are approximate. Search for the quoted text instead (Notepad: `Ctrl+F`).

After changing **`IncidentButton.cs` or `app.ico`**, you must rebuild (`.\Build.ps1`) and ship a new version (see [SETUP-GUIDE.md → Shipping a new version](../SETUP-GUIDE.md#shipping-a-new-version-of-the-app-itself)).
After changing **only `contacts.json`**, you don't need to rebuild. Just push the file (see [SETUP-GUIDE.md → Changing contacts later](../SETUP-GUIDE.md#changing-contacts-later--no-rebuild-needed)).

---

## Contents

- [contacts.json: field by field](#contactsjson-field-by-field)
- [Fallback contacts](#fallback-contacts)
- [Company name in file properties](#company-name-in-file-properties)
- [Header wording](#header-wording)
- [Footer tip wording](#footer-tip-wording)
- [Colors](#colors)
- [Tray icon](#tray-icon)
- [Tray menu and About box text](#tray-menu-and-about-box-text)
- [Email IT now: recipient, subject, body](#email-it-now-recipient-subject-body)
- [Install and config locations](#install-and-config-locations)
- [Version number](#version-number)

---

## contacts.json: field by field

| Field | Shown where | Notes |
|---|---|---|
| `organization` | Window title, tray tooltip, About box | If the tooltip `<organization> - Report a Cyber Incident` would be longer than 63 characters (a Windows limit), the tooltip drops the name and shows `Report a Cyber Incident`. |
| `contacts` | One card per entry, top to bottom in file order | Add or remove entries freely. |
| `order` | Red square badge on the card | Text, 1–2 characters. Cards are **not** sorted by it; file order wins. |
| `role` | Bold heading: `role: name` | Keep `role` + `name` under ~45 characters; this line doesn't wrap. |
| `name` | Bold heading: `role: name` | **Required.** A contact with an empty name is ignored. |
| `phone` | Red click-to-call link | Write it any way you like for display. When clicked, everything except digits and `+` is removed before dialing, so include the country code (`+1 ...`) for reliable dialing. |
| `email` | Blue click-to-email link | Contact #1's email is also the recipient of **Email IT now**. |
| `note` | Grey italic text under the email | Wraps automatically; the card grows. Keep it to one or two sentences. |
| `_README` | Nowhere | Instruction text for whoever edits the file. Safe to delete. |

**Validate after every edit** (PowerShell, in the folder with the file):

```powershell
Get-Content .\contacts.json -Raw | ConvertFrom-Json | Select-Object -ExpandProperty contacts | Format-Table order, role, name, phone, email
```

If it prints a table of your contacts, the file is valid. If it prints red error text, the error message points to the line with the missing comma or quote.

Characters to avoid in any value: `{` and `}`. To include a double quote inside a value, write `\"`.

---

## Fallback contacts

**File:** `IncidentButton.cs` · **Search for:** `FALLBACK DATA` (around line 66)

```csharp
private const string FallbackOrgName = "Example Corporation";

private static ContactBook Fallback()
{
    ...
        new Contact { Order="1", Role="IT Manager", Name="First Contact Name",
                      Phone="+1 (555) 555-0101", Email="first.contact@example.com",
                      Note="Call first. If no answer within 10 minutes, go to contact 2." },
    ...
}
```

These values are compiled into the `.exe` and used **only** when `C:\ProgramData\IncidentButton\contacts.json` is missing, unreadable, or contains no valid contacts.

**Recommendation:** for an internal (private) build, copy your real contacts here as well. If the JSON file is ever damaged, employees still see the right people instead of example data. Keep the same structure: each contact is a `new Contact { ... },` block, and the last one has no trailing comma.

> If your repository is public, make this change only in your local working copy or a private fork. Don't commit real contact details publicly.

---

## Company name in file properties

**File:** `IncidentButton.cs` · **Search for:** `AssemblyCompany` (around line 25)

```csharp
[assembly: AssemblyCompany("Example Corporation")]
```

This changes what appears under *Properties → Details* of `IncidentButton.exe` and in Task Manager's details. It doesn't change anything in the panel.

---

## Header wording

**File:** `IncidentButton.cs` · **Search for:** `CYBER INCIDENT?` (around line 358)

```csharp
Text = "\u26A0  CYBER INCIDENT?",   // \u26A0 = warning sign
...
Text = "Stay calm. Do NOT shut down or unplug your computer unless told to.\n" +
       "Disconnect from Wi-Fi/network if you suspect active compromise, then call:",
```

- `\n` starts a new line. `\u26A0` is the ⚠ symbol, written as a code so it survives any text editor's encoding. The header area is a fixed 84 px tall, so keep the second label to **two lines** of about 75 characters each.
- Make sure this advice matches your incident response plan. For example, some organizations want users to **leave the network connected** so the EDR can respond. If so, change the second line.

---

## Footer tip wording

**File:** `IncidentButton.cs` · **Search for:** `Write down what you saw` (around line 428)

```csharp
Text = "Write down what you saw and when. Your report is never wrong - when in doubt, report it.",
```

It wraps in a 236 px wide column to the right of the buttons. Three short lines is the comfortable maximum.

---

## Colors

**File:** `IncidentButton.cs` · **Search for:** `Panel colors` (around line 335)

```csharp
private static readonly Color Accent = Color.FromArgb(178, 34, 34);  // header, badges, phone links, "Email IT now"
private static readonly Color Dark   = Color.FromArgb(32, 32, 36);   // contact headings, "Copy contact list"
```

The values are red, green, blue (0–255). Keep the accent dark enough that white text on it stays readable (contrast ratio ≥ 4.5:1). A strong red or dark orange works best, because the panel is meant to look urgent.

Other fixed colors, if you want to go further: card background `Color.FromArgb(246, 246, 248)`, email link `Color.FromArgb(0, 90, 158)`.

---

## Tray icon

**File:** `app.ico` (replace it, keep the name)

- Use a real `.ico` file with at least **16×16, 20×20, 24×24 and 32×32** images inside, or the tray icon will look blurry. Most icon editors and online converters can produce a multi-size `.ico`.
- `Build.ps1` embeds it twice: once as the `.exe`'s icon (Explorer, Task Manager) and once as a resource used for the tray and window.
- If `app.ico` is missing at build time, the app uses the built-in Windows shield icon.

---

## Tray menu and About box text

**File:** `IncidentButton.cs` · **Search for:** `Report a Cyber Incident...` and `Managed by IT.` (around lines 215–225)

You can change the menu label or the About box message here. To show a help-desk number in the About box, add a line to that string, for example `"Help desk: x1234\n\n" +`.

---

## Email IT now: recipient, subject, body

**File:** `IncidentButton.cs` · **Search for:** `Email IT clicked` (around line 410)

- **Recipient:** contact #1's `email` from `contacts.json`. To send it to a dedicated address no matter who is first, replace
  `var first = contacts.Count > 0 ? contacts[0].Email : "";`
  with
  `var first = "security@yourcompany.com";`
- **Subject:** `"POSSIBLE CYBER INCIDENT - " + Environment.MachineName`
- **Body:** the lines after `"&body="`. Each `\n` is a new line.

---

## Install and config locations

These are referenced in several places. If you change them, change **all** of them to match:

| Location | Set in |
|---|---|
| `C:\Program Files\IncidentButton\` | `install.ps1` (`$InstallDir`), `uninstall.ps1`, the Intune detection rule |
| `C:\ProgramData\IncidentButton\contacts.json` | `IncidentButton.cs` (`ConfigDir`), `install.ps1` (`$ConfigDir`), `uninstall.ps1`, any contact-update script |
| `%LOCALAPPDATA%\IncidentButton\activity.log` | `IncidentButton.cs` (`LogDir`) |
| Registry Run value `IncidentButton` | `install.ps1`, `uninstall.ps1` |

The defaults are fine for almost everyone.

---

## Version number

**File:** `install.ps1` · **Line:** `$Version = '1.0.0'`

Raise it every time you ship a new **build** (code or icon changes), and set the Intune detection rule to the **exact same string**. You don't need to change it for `contacts.json` updates.

Optionally keep `AssemblyFileVersion` / `AssemblyVersion` at the top of `IncidentButton.cs` in step, so the `.exe` file properties show the same version.
