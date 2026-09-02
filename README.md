# Peripheral Companion

A portable **mouse jiggler / anti-idle utility for Windows 11** that runs
directly from a USB stick. It keeps a session marked as active by issuing
periodic synthetic input. It runs quietly in the background with **no window
and no tray icon**.

> **Authorized use only.** This is a legitimate anti-idle tool. Run it only on
> machines you own or are explicitly permitted to operate. It does not hide
> itself from administrators or security software, and it requests no elevated
> privileges (`asInvoker`).

## Two ways to run it

| Option | Build required | Best for |
| --- | --- | --- |
| **A. Compiled `.exe`** (`src/`, prebuilt) | None to run | Drop one file on the stick and double-click |
| **B. Portable script** (`portable/`) | None | Same behavior with visible on/off control |

---

## A. Compiled executable (recommended)

`PeripheralCompanion.exe` is a **single, self-contained file**. It needs nothing
installed on the target machine — no .NET runtime, no admin rights.

**Use it:**

1. Copy `PeripheralCompanion.exe` onto your USB stick.
2. Double-click it on the Windows 11 machine.
3. It starts working immediately and silently. Nothing appears on screen.

**Stop it:** open Task Manager, find `PeripheralCompanion.exe`, and click
**End task**. (Running it a second time does nothing — only one copy runs.)

**Defaults:** presses the invisible F15 key every 60 seconds, skips a cycle
while you are actively using the machine, and asks Windows not to sleep or blank
the display.

**Optional configuration:** place a file named `settings.json` next to the
`.exe` to change behavior. It travels with the stick; nothing is written to the
host user profile. Example:

```json
{
  "Mode": "Invisible",
  "IntervalSeconds": 60,
  "RespectUserActivity": true,
  "KeepDisplayAwake": true,
  "AutoStart": true
}
```

- `Mode`: `"Invisible"` (F15 key, nothing visible) or `"MouseNudge"` (moves the
  cursor one pixel and back).
- `IntervalSeconds`: seconds between jiggles.
- `RespectUserActivity`: `true` = stay out of the way while you are active.
- `KeepDisplayAwake`: `true` = also prevent sleep and screen blanking.

### Rebuilding it yourself

The prebuilt file is produced from `src/PeripheralCompanion/`. To rebuild on a
machine with the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0):

```
build\publish.cmd
```

or

```
powershell -ExecutionPolicy Bypass -File build\publish.ps1
```

Output: **`build\dist\PeripheralCompanion.exe`**.

---

## B. Portable script (no build, visible control)

Everything is in `portable/`. Copy that folder to the stick.

- **`Run-Visible.cmd`** — run in a console window you can watch and close to stop.
- **`Run-Hidden.vbs`** — run silently with no window (stop via Task Manager).
- **`Jiggle.ps1`** — the script. Options:

```
powershell -NoProfile -ExecutionPolicy Bypass -File .\Jiggle.ps1 -Mode Mouse -IntervalSeconds 30 -RespectActivity
```

`-IntervalSeconds <n>`, `-Mode Invisible|Mouse`, `-RespectActivity`.

---

## How it works

- **F15 key** (`Invisible`): F15 is a real virtual key that no physical keyboard
  sends. Pressing it resets the Windows idle timer without moving the cursor or
  affecting any application.
- **Mouse nudge** (`MouseNudge` / `Mouse`): moves the cursor one pixel and back.
- **Idle-aware**: uses `GetLastInputInfo` to detect real activity and stays out
  of the way when you are actually using the machine.
- **Stay-awake**: uses `SetThreadExecutionState` to keep the display and system
  from sleeping.

## Layout

```
src/PeripheralCompanion/   Headless background jiggler (C#, .NET 8, no UI)
  Program.cs                 Entry point; single-instance; hidden message loop
  JiggleEngine.cs            Timer + input logic
  NativeMethods.cs           Win32 P/Invoke
  Settings.cs                Optional portable settings.json
  app.manifest               asInvoker; no elevation
  PeripheralCompanion.csproj Single-file, self-contained, win-x64
build/                     Publish scripts -> build/dist/PeripheralCompanion.exe
portable/                  Zero-build PowerShell version
```

## Requirements

- Windows 11 (also works on Windows 10, 64-bit).
- Compiled `.exe`: nothing at runtime; .NET 8 SDK only if you rebuild.
- Portable script: Windows PowerShell 5.1 (built into Windows).
