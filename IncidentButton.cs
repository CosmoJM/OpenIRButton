// ============================================================================
// INCIDENT RESPONSE BUTTON
//
// A system tray app that shows employees who to call during a cyber incident.
//
// ADMIN: You normally DO NOT need to edit this file.
//        Put your organization name and escalation contacts in contacts.json.
//        The values below are only used if contacts.json is missing or unreadable.
//
// Build:  run Build.ps1   (or see SETUP-GUIDE.md)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Incident Response Button")]
[assembly: AssemblyDescription("Cyber incident reporting quick-reference")]
[assembly: AssemblyCompany("Example Corporation")]
[assembly: AssemblyProduct("Incident Response Button")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyVersion("1.0.0.0")]

namespace IncidentButton
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (var mutex = new System.Threading.Mutex(true, "IncidentButtonSingleInstance", out createdNew))
            {
                if (!createdNew) return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayContext());
            }
        }
    }

    internal class Contact
    {
        public string Order;
        public string Role;
        public string Name;
        public string Phone;
        public string Email;
        public string Note;
    }

    internal class ContactBook
    {
        public string Organization;
        public List<Contact> Contacts;
    }

    internal static class Config
    {
        // ====================================================================
        // FALLBACK DATA - used ONLY when contacts.json is missing or corrupt.
        // contacts.json always wins. Edit contacts.json, not this.
        // ====================================================================
        private const string FallbackOrgName = "Example Corporation";

        private static ContactBook Fallback()
        {
            return new ContactBook
            {
                Organization = FallbackOrgName,
                Contacts = new List<Contact>
                {
                    new Contact { Order="1", Role="IT Manager", Name="First Contact Name",
                                  Phone="+1 (555) 555-0101", Email="first.contact@example.com",
                                  Note="Call first. If no answer within 10 minutes, go to contact 2." },
                    new Contact { Order="2", Role="Operations Executive", Name="Second Contact Name",
                                  Phone="+1 (555) 555-0102", Email="second.contact@example.com",
                                  Note="If no answer within 10 minutes, go to contact 3." },
                    new Contact { Order="3", Role="24/7 Security Operations Center", Name="Security Provider Name",
                                  Phone="+1 (555) 555-0103", Email="soc@example-security.com",
                                  Note="Final escalation. Tell them which company you are calling from. Keep trying contacts until you reach someone." }
                }
            };
        }
        // ====================================================================

        public static readonly string ConfigDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                         "IncidentButton");
        public static string ConfigPath { get { return Path.Combine(ConfigDir, "contacts.json"); } }

        public static readonly string LogDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "IncidentButton");
        public static string LogPath { get { return Path.Combine(LogDir, "activity.log"); } }

        public static ContactBook Load()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return Fallback();
                string json = File.ReadAllText(ConfigPath);

                var contacts = new List<Contact>();
                foreach (Match obj in Regex.Matches(json, @"\{[^{}]*\}"))
                {
                    var c = new Contact
                    {
                        Order = Field(obj.Value, "order"),
                        Role  = Field(obj.Value, "role"),
                        Name  = Field(obj.Value, "name"),
                        Phone = Field(obj.Value, "phone"),
                        Email = Field(obj.Value, "email"),
                        Note  = Field(obj.Value, "note")
                    };
                    if (!string.IsNullOrEmpty(c.Name)) contacts.Add(c);
                }
                if (contacts.Count == 0) return Fallback();

                string org = Field(json, "organization");
                return new ContactBook
                {
                    Organization = string.IsNullOrEmpty(org) ? FallbackOrgName : org,
                    Contacts = contacts
                };
            }
            catch { return Fallback(); }
        }

        private static string Field(string text, string key)
        {
            var m = Regex.Match(text, "\"" + key + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"",
                                RegexOptions.IgnoreCase);
            return m.Success ? Regex.Unescape(m.Groups[1].Value) : "";
        }

        public static void Log(string action)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                File.AppendAllText(LogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " +
                    Environment.UserName + "  " + action + Environment.NewLine);
            }
            catch { }
        }
    }

    internal static class AppIcons
    {
        private static byte[] _raw;
        private static bool _tried;

        private static byte[] Raw()
        {
            if (!_tried)
            {
                _tried = true;
                try
                {
                    using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico"))
                    {
                        if (s != null)
                        {
                            _raw = new byte[s.Length];
                            int read = 0;
                            while (read < _raw.Length)
                            {
                                int n = s.Read(_raw, read, _raw.Length - read);
                                if (n <= 0) break;
                                read += n;
                            }
                        }
                    }
                }
                catch { _raw = null; }
            }
            return _raw;
        }

        private static Icon Get(Size size)
        {
            var raw = Raw();
            if (raw != null)
            {
                try { using (var ms = new MemoryStream(raw)) return new Icon(ms, size); }
                catch { }
            }
            return SystemIcons.Shield;
        }

        public static Icon Tray   { get { return Get(SystemInformation.SmallIconSize); } }
        public static Icon Window { get { return Get(new Size(32, 32)); } }
    }

    internal class TrayContext : ApplicationContext
    {
        private readonly NotifyIcon _tray;
        private IncidentForm _form;
        private Timer _promoteTimer;
        private int _promoteAttempts;

        public TrayContext()
        {
            string org = Config.Load().Organization;

            var menu = new ContextMenuStrip();
            var open = new ToolStripMenuItem("Report a Cyber Incident...", null, (s, e) => ShowForm());
            open.Font = new Font(open.Font, FontStyle.Bold);
            menu.Items.Add(open);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("About", null, (s, e) =>
                MessageBox.Show(
                    org + " Incident Response Button\n\n" +
                    "If you think a cyber incident has happened or is happening,\n" +
                    "click \"Report a Cyber Incident\" for who to contact, in order.\n\n" +
                    "Managed by IT.",
                    "About", MessageBoxButtons.OK, MessageBoxIcon.Information)));

            // Windows caps tray tooltips at 63 characters
            string tip = org + " - Report a Cyber Incident";
            if (tip.Length > 63) tip = "Report a Cyber Incident";

            _tray = new NotifyIcon
            {
                Icon = AppIcons.Tray,
                Text = tip,
                Visible = true,
                ContextMenuStrip = menu
            };
            _tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) ShowForm(); };
            Config.Log("App started");

            _promoteTimer = new Timer { Interval = 10000 };
            _promoteTimer.Tick += (s, e) =>
            {
                _promoteAttempts++;
                if (TryPromoteTrayIcon() || _promoteAttempts >= 360) _promoteTimer.Stop();
            };
            _promoteTimer.Start();
        }

        // Pins the tray icon to the taskbar instead of hiding it behind the
        // overflow arrow (Windows 11 22H2+). Explorer creates the registry entry
        // lazily, so this retries for up to an hour and on every panel open.
        private static bool TryPromoteTrayIcon()
        {
            try
            {
                string exe = Application.ExecutablePath;
                using (var baseKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings"))
                {
                    if (baseKey == null) return true;
                    foreach (string sub in baseKey.GetSubKeyNames())
                    {
                        using (var k = Registry.CurrentUser.OpenSubKey(
                            @"Control Panel\NotifyIconSettings\" + sub, true))
                        {
                            if (k == null) continue;
                            var path = k.GetValue("ExecutablePath") as string;
                            if (PathMatchesUs(path, exe))
                            {
                                var val = k.GetValue("IsPromoted");
                                if (!(val is int) || (int)val != 1)
                                {
                                    k.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
                                    Config.Log("Tray icon promoted to taskbar");
                                }
                                return true;
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        // Windows records apps under Program Files with a {KNOWNFOLDERID} prefix
        // instead of a literal path, so both forms have to be matched.
        private static bool PathMatchesUs(string regPath, string exePath)
        {
            if (string.IsNullOrEmpty(regPath)) return false;
            if (regPath.Equals(exePath, StringComparison.OrdinalIgnoreCase)) return true;

            if (regPath.StartsWith("{", StringComparison.Ordinal))
            {
                int brace = regPath.IndexOf('}');
                if (brace > 0 && brace < regPath.Length - 1)
                {
                    string rel = regPath.Substring(brace + 1).TrimStart('\\');
                    if (rel.Length > 0 &&
                        exePath.EndsWith("\\" + rel, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private void ShowForm()
        {
            Config.Log("Incident panel opened");
            TryPromoteTrayIcon();
            if (_form == null || _form.IsDisposed)
            {
                _form = new IncidentForm();
                _form.Show();
            }
            _form.WindowState = FormWindowState.Normal;
            _form.Activate();
            _form.BringToFront();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_promoteTimer != null) _promoteTimer.Dispose();
                _tray.Visible = false;
                _tray.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal class IncidentForm : Form
    {
        // Panel colors. Change these if you want the window to match your brand.
        private static readonly Color Accent = Color.FromArgb(178, 34, 34);
        private static readonly Color Dark   = Color.FromArgb(32, 32, 36);

        public IncidentForm()
        {
            var book = Config.Load();
            var contacts = book.Contacts;

            Text = book.Organization + " - Cyber Incident Response";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            TopMost = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
            ClientSize = new Size(600, 400);
            Icon = AppIcons.Window;

            var header = new Panel { BackColor = Accent, Bounds = new Rectangle(0, 0, 600, 84) };
            header.Controls.Add(new Label
            {
                Text = "⚠  CYBER INCIDENT?",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                AutoSize = true, Location = new Point(18, 10), BackColor = Accent
            });
            header.Controls.Add(new Label
            {
                Text = "Stay calm. Do NOT shut down or unplug your computer unless told to.\n" +
                       "Disconnect from Wi-Fi/network if you suspect active compromise, then call:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true, Location = new Point(20, 46), BackColor = Accent
            });
            Controls.Add(header);

            int y = 96;
            for (int i = 0; i < contacts.Count; i++)
            {
                var step = BuildStep(contacts[i], y);
                Controls.Add(step);
                y += step.Height + 10;
            }

            var copyBtn = new Button
            {
                Text = "Copy contact list",
                Bounds = new Rectangle(18, y + 12, 160, 36),
                FlatStyle = FlatStyle.Flat, BackColor = Dark, ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            copyBtn.FlatAppearance.BorderSize = 0;
            copyBtn.Click += (s, e) =>
            {
                var sb = new System.Text.StringBuilder();
                foreach (var c in contacts)
                    sb.AppendLine(c.Order + ". " + c.Role + " - " + c.Name + " - " + c.Phone + " - " + c.Email);
                Clipboard.SetText(sb.ToString());
                Config.Log("Contact list copied to clipboard");
                copyBtn.Text = "Copied!";
            };
            Controls.Add(copyBtn);

            var mailBtn = new Button
            {
                Text = "Email IT now",
                Bounds = new Rectangle(190, y + 12, 140, 36),
                FlatStyle = FlatStyle.Flat, BackColor = Accent, ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            mailBtn.FlatAppearance.BorderSize = 0;
            mailBtn.Click += (s, e) =>
            {
                Config.Log("Email IT clicked");
                var first = contacts.Count > 0 ? contacts[0].Email : "";
                try
                {
                    Process.Start("mailto:" + first +
                        "?subject=" + Uri.EscapeDataString("POSSIBLE CYBER INCIDENT - " + Environment.MachineName) +
                        "&body=" + Uri.EscapeDataString(
                            "Reporting a possible cyber incident.\n" +
                            "User: " + Environment.UserName + "\n" +
                            "Computer: " + Environment.MachineName + "\n" +
                            "Time noticed: \nWhat I saw: \n"));
                }
                catch { }
            };
            Controls.Add(mailBtn);

            var tip = new Label
            {
                Text = "Write down what you saw and when. Your report is never wrong - when in doubt, report it.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                MaximumSize = new Size(236, 0),
                Location = new Point(346, y + 12)
            };
            Controls.Add(tip);

            // Size the window to its contents, but never taller than the screen
            int footerBottom = Math.Max(copyBtn.Bottom,
                tip.Location.Y + tip.GetPreferredSize(new Size(236, 0)).Height);
            int height = footerBottom + 14;
            int maxHeight = (int)(Screen.PrimaryScreen.WorkingArea.Height * 0.9);
            if (height > maxHeight) { height = maxHeight; AutoScroll = true; }
            ClientSize = new Size(600, height);
        }

        // Each card grows to fit however many lines its note wraps to.
        private Panel BuildStep(Contact c, int y)
        {
            var noteFont = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            int noteHeight = TextRenderer.MeasureText(
                string.IsNullOrEmpty(c.Note) ? " " : c.Note, noteFont,
                new Size(496, int.MaxValue), TextFormatFlags.WordBreak).Height;
            int cardHeight = Math.Max(108, 82 + noteHeight + 8);

            var p = new Panel
            {
                Bounds = new Rectangle(14, y, 572, cardHeight),
                BackColor = Color.FromArgb(246, 246, 248),
                BorderStyle = BorderStyle.FixedSingle
            };

            p.Controls.Add(new Label
            {
                Text = c.Order,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Accent,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(10, 10, 44, 44)
            });

            p.Controls.Add(new Label
            {
                Text = c.Role + ": " + c.Name,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Dark, AutoSize = true, Location = new Point(66, 8)
            });

            var phone = new LinkLabel
            {
                Text = "☎ " + c.Phone,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(64, 34),
                LinkColor = Accent
            };
            phone.LinkClicked += (s, e) =>
            {
                Config.Log("Phone clicked for contact " + c.Order + " (" + c.Name + ")");
                try { Process.Start("tel:" + Regex.Replace(c.Phone, @"[^\d+]", "")); }
                catch { Clipboard.SetText(c.Phone); }
            };
            p.Controls.Add(phone);

            var mail = new LinkLabel
            {
                Text = c.Email,
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true, Location = new Point(66, 62),
                LinkColor = Color.FromArgb(0, 90, 158)
            };
            mail.LinkClicked += (s, e) =>
            {
                Config.Log("Email clicked for contact " + c.Order + " (" + c.Name + ")");
                try { Process.Start("mailto:" + c.Email); } catch { }
            };
            p.Controls.Add(mail);

            p.Controls.Add(new Label
            {
                Text = c.Note,
                Font = noteFont,
                ForeColor = Color.DimGray, AutoSize = true,
                MaximumSize = new Size(496, 0),
                Location = new Point(66, 82)
            });

            return p;
        }
    }
}
