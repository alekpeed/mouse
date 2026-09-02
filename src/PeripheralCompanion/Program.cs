namespace PeripheralCompanion;

internal static class Program
{
    // Single-instance guard so launching twice from the stick does not stack
    // two background jigglers.
    private static Mutex? _instanceMutex;

    [STAThread]
    private static void Main()
    {
        _instanceMutex = new Mutex(initiallyOwned: true, "PeripheralCompanion.SingleInstance", out bool isNew);
        if (!isNew)
        {
            // Another copy is already running in the background.
            return;
        }

        ApplicationConfiguration.Initialize();

        // Read optional settings.json sitting next to the executable; fall back
        // to sensible defaults (invisible F15 method, 60 s, idle-aware).
        Settings settings = Settings.Load();

        using var engine = new JiggleEngine
        {
            Mode = settings.Mode,
            IntervalSeconds = settings.IntervalSeconds,
            RespectUserActivity = settings.RespectUserActivity,
            KeepDisplayAwake = settings.KeepDisplayAwake,
        };
        engine.Start();

        // No window, no tray icon. A hidden message loop keeps the timer alive
        // so the process runs quietly in the background until it is ended from
        // Task Manager.
        Application.Run(new ApplicationContext());

        GC.KeepAlive(_instanceMutex);
    }
}
