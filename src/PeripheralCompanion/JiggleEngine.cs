using static PeripheralCompanion.NativeMethods;

namespace PeripheralCompanion;

/// <summary>
/// Selects how the utility keeps the session marked as active.
/// </summary>
public enum JiggleMode
{
    /// <summary>Press the invisible F15 key. No cursor movement.</summary>
    Invisible,

    /// <summary>Nudge the cursor one pixel and immediately return it.</summary>
    MouseNudge,
}

/// <summary>
/// Generates periodic synthetic input to prevent the session from being
/// reported as idle. When <see cref="RespectUserActivity"/> is enabled the
/// engine stays out of the way while a real person is using the machine and
/// only acts once genuine input has been quiet for a full interval.
/// </summary>
public sealed class JiggleEngine : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer = new();

    public JiggleMode Mode { get; set; } = JiggleMode.Invisible;

    /// <summary>Interval between jiggles, in seconds.</summary>
    public int IntervalSeconds
    {
        get => _timer.Interval / 1000;
        set => _timer.Interval = Math.Max(1, value) * 1000;
    }

    /// <summary>
    /// When true, the engine skips a tick if the user produced real input
    /// within the last interval, so it never fights an active operator.
    /// </summary>
    public bool RespectUserActivity { get; set; } = true;

    /// <summary>
    /// When true, also request that Windows keep the display and system awake
    /// via SetThreadExecutionState for as long as the engine is running.
    /// </summary>
    public bool KeepDisplayAwake { get; set; } = true;

    public bool IsRunning { get; private set; }

    /// <summary>Raised after each performed jiggle, for UI feedback.</summary>
    public event EventHandler? Jiggled;

    public JiggleEngine()
    {
        _timer.Interval = 60_000;
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;
        ApplyExecutionState();
        _timer.Start();
    }

    public void Stop()
    {
        if (!IsRunning) return;
        IsRunning = false;
        _timer.Stop();
        // Release any awake request so normal power policy resumes.
        SetThreadExecutionState(ExecutionState.ES_CONTINUOUS);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (RespectUserActivity && GetIdleMilliseconds() < _timer.Interval)
        {
            // A human is active. Do nothing this cycle.
            return;
        }

        ApplyExecutionState();

        switch (Mode)
        {
            case JiggleMode.MouseNudge:
                SendMouseNudge();
                break;
            case JiggleMode.Invisible:
            default:
                SendF15();
                break;
        }

        Jiggled?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyExecutionState()
    {
        if (!KeepDisplayAwake) return;
        SetThreadExecutionState(
            ExecutionState.ES_CONTINUOUS |
            ExecutionState.ES_SYSTEM_REQUIRED |
            ExecutionState.ES_DISPLAY_REQUIRED);
    }

    private static uint GetIdleMilliseconds()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info)) return uint.MaxValue;
        return unchecked(GetTickCount() - info.dwTime);
    }

    private static void SendF15()
    {
        var down = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION { ki = new KEYBDINPUT { wVk = VK_F15 } },
        };
        var up = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION { ki = new KEYBDINPUT { wVk = VK_F15, dwFlags = KEYEVENTF_KEYUP } },
        };
        SendInput(2, new[] { down, up }, System.Runtime.InteropServices.Marshal.SizeOf<INPUT>());
    }

    private static void SendMouseNudge()
    {
        var forward = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUTUNION { mi = new MOUSEINPUT { dx = 1, dy = 0, dwFlags = MOUSEEVENTF_MOVE } },
        };
        var back = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUTUNION { mi = new MOUSEINPUT { dx = -1, dy = 0, dwFlags = MOUSEEVENTF_MOVE } },
        };
        int size = System.Runtime.InteropServices.Marshal.SizeOf<INPUT>();
        SendInput(1, new[] { forward }, size);
        SendInput(1, new[] { back }, size);
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
    }
}
