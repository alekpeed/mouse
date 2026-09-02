using System.Drawing;
using System.Drawing.Drawing2D;

namespace PeripheralCompanion;

/// <summary>
/// Owns the tray icon, context menu, and the running <see cref="JiggleEngine"/>.
/// Presents as a small system-tray device companion utility.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private const string ProductName = "Peripheral Companion";

    private readonly Settings _settings;
    private readonly JiggleEngine _engine = new();
    private readonly NotifyIcon _tray = new();
    private readonly Icon _iconActive;
    private readonly Icon _iconIdle;

    private ToolStripMenuItem _toggleItem = null!;

    public TrayApplicationContext()
    {
        _settings = Settings.Load();

        _iconActive = BuildIcon(active: true);
        _iconIdle = BuildIcon(active: false);

        _engine.Mode = _settings.Mode;
        _engine.IntervalSeconds = _settings.IntervalSeconds;
        _engine.RespectUserActivity = _settings.RespectUserActivity;
        _engine.KeepDisplayAwake = _settings.KeepDisplayAwake;

        _tray.Icon = _iconIdle;
        _tray.Text = ProductName;
        _tray.Visible = true;
        _tray.ContextMenuStrip = BuildMenu();
        _tray.DoubleClick += (_, _) => Toggle();

        if (_settings.AutoStart)
        {
            SetRunning(true, showBalloon: false);
        }
    }

    // ----- Menu -----

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        var header = new ToolStripMenuItem(ProductName) { Enabled = false };
        menu.Items.Add(header);
        menu.Items.Add(new ToolStripSeparator());

        _toggleItem = new ToolStripMenuItem("Active", null, (_, _) => Toggle())
        {
            Checked = _engine.IsRunning,
            CheckOnClick = false,
        };
        menu.Items.Add(_toggleItem);

        // Interval submenu.
        var intervals = new ToolStripMenuItem("Interval");
        foreach ((string label, int seconds) in new[]
        {
            ("30 seconds", 30),
            ("1 minute", 60),
            ("2 minutes", 120),
            ("5 minutes", 300),
        })
        {
            int s = seconds;
            var item = new ToolStripMenuItem(label)
            {
                Checked = _settings.IntervalSeconds == s,
                CheckOnClick = false,
            };
            item.Click += (_, _) =>
            {
                _settings.IntervalSeconds = s;
                _engine.IntervalSeconds = s;
                Persist();
                foreach (ToolStripMenuItem sib in intervals.DropDownItems)
                    sib.Checked = sib == item;
            };
            intervals.DropDownItems.Add(item);
        }
        menu.Items.Add(intervals);

        // Mode submenu.
        var modes = new ToolStripMenuItem("Method");
        AddModeItem(modes, "Invisible (F15 key)", JiggleMode.Invisible);
        AddModeItem(modes, "Mouse nudge (1 px)", JiggleMode.MouseNudge);
        menu.Items.Add(modes);

        menu.Items.Add(new ToolStripSeparator());

        var respect = new ToolStripMenuItem("Pause while I'm using the PC")
        {
            Checked = _settings.RespectUserActivity,
            CheckOnClick = true,
        };
        respect.CheckedChanged += (_, _) =>
        {
            _settings.RespectUserActivity = respect.Checked;
            _engine.RespectUserActivity = respect.Checked;
            Persist();
        };
        menu.Items.Add(respect);

        var awake = new ToolStripMenuItem("Keep display awake")
        {
            Checked = _settings.KeepDisplayAwake,
            CheckOnClick = true,
        };
        awake.CheckedChanged += (_, _) =>
        {
            _settings.KeepDisplayAwake = awake.Checked;
            _engine.KeepDisplayAwake = awake.Checked;
            Persist();
        };
        menu.Items.Add(awake);

        var autostart = new ToolStripMenuItem("Start active on launch")
        {
            Checked = _settings.AutoStart,
            CheckOnClick = true,
        };
        autostart.CheckedChanged += (_, _) =>
        {
            _settings.AutoStart = autostart.Checked;
            Persist();
        };
        menu.Items.Add(autostart);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("About", null, (_, _) => ShowAbout()));
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitApp()));

        return menu;
    }

    private void AddModeItem(ToolStripMenuItem parent, string label, JiggleMode mode)
    {
        var item = new ToolStripMenuItem(label)
        {
            Checked = _settings.Mode == mode,
            CheckOnClick = false,
        };
        item.Click += (_, _) =>
        {
            _settings.Mode = mode;
            _engine.Mode = mode;
            Persist();
            foreach (ToolStripMenuItem sib in parent.DropDownItems)
                sib.Checked = sib == item;
        };
        parent.DropDownItems.Add(item);
    }

    // ----- State -----

    private void Toggle() => SetRunning(!_engine.IsRunning, showBalloon: true);

    private void SetRunning(bool run, bool showBalloon)
    {
        if (run) _engine.Start();
        else _engine.Stop();

        _toggleItem.Checked = _engine.IsRunning;
        _tray.Icon = _engine.IsRunning ? _iconActive : _iconIdle;
        _tray.Text = _engine.IsRunning
            ? $"{ProductName} — active"
            : $"{ProductName} — paused";

        if (showBalloon)
        {
            _tray.BalloonTipTitle = ProductName;
            _tray.BalloonTipText = _engine.IsRunning ? "Keeping session active." : "Paused.";
            _tray.ShowBalloonTip(1500);
        }
    }

    private void Persist() => _settings.Save();

    private void ShowAbout()
    {
        MessageBox.Show(
            $"{ProductName} 1.0\n\n" +
            "A portable system-tray utility that keeps a Windows session marked " +
            "as active by issuing periodic input. Settings are stored next to the " +
            "executable so the tool runs directly from removable media.\n\n" +
            "Use only on machines you are authorized to operate.",
            $"About {ProductName}",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ExitApp()
    {
        _tray.Visible = false;
        _engine.Dispose();
        _tray.Dispose();
        ExitThread();
    }

    // ----- Icon rendering -----

    /// <summary>
    /// Draws a small mouse-shaped glyph at runtime so the app ships without a
    /// separate .ico asset. The active variant is tinted; the idle variant is
    /// muted.
    /// </summary>
    private static Icon BuildIcon(bool active)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            Color body = active ? Color.FromArgb(0x2E, 0x7D, 0x32) : Color.FromArgb(0x60, 0x60, 0x60);
            Color outline = Color.FromArgb(0x20, 0x20, 0x20);

            using var bodyBrush = new SolidBrush(body);
            using var pen = new Pen(outline, 1.5f);

            // Mouse body: rounded capsule.
            var rect = new Rectangle(8, 4, 16, 24);
            using (var path = RoundedCapsule(rect))
            {
                g.FillPath(bodyBrush, path);
                g.DrawPath(pen, path);
            }

            // Split line between the two buttons.
            g.DrawLine(pen, 16, 5, 16, 13);

            // Scroll wheel.
            using var wheel = new SolidBrush(active ? Color.White : Color.FromArgb(0xC0, 0xC0, 0xC0));
            g.FillRectangle(wheel, 15, 7, 2, 5);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }

    private static GraphicsPath RoundedCapsule(Rectangle r)
    {
        int d = r.Width;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 180);                 // top
        path.AddArc(r.X, r.Bottom - d, d, d, 0, 180);          // bottom
        path.CloseFigure();
        return path;
    }
}
