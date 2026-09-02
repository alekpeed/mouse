namespace PeripheralCompanion;

internal static class Program
{
    // Single-instance guard so plugging in the stick and double-clicking twice
    // does not stack multiple tray icons.
    private static Mutex? _instanceMutex;

    [STAThread]
    private static void Main()
    {
        _instanceMutex = new Mutex(initiallyOwned: true, "PeripheralCompanion.SingleInstance", out bool isNew);
        if (!isNew)
        {
            // Another copy is already resident in the tray.
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());

        GC.KeepAlive(_instanceMutex);
    }
}
