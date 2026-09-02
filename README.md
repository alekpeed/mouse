# Peripheral Companion

A portable **mouse jiggler / anti-idle utility for Windows 11** that runs
directly from a USB stick. It keeps a session marked as active by issuing
periodic synthetic input, and presents itself as a small system-tray
peripheral utility.

> **Authorized use only.** This is a legitimate anti-idle tool. Run it only on
> machines you own or are explicitly permitted to operate. It does not hide
> itself from administrators or security software, and it requests no elevated
> privileges.

## Two ways to run it

| Option | Build required | Best for |
| --- | --- | --- |
| **A. Portable script** (`portable/`) | None | Immediate use — drop on the stick and double-click |
| **B. Tray app** (`src/`) | .NET 8 SDK, once | A single `.exe` with a tray icon and menu |

---

## A. Portable script (no build)

Everything is in `portable/`. Copy that folder to your USB stick.

- **`Run-Visible.cmd`** — double-click to run in a console window you can watch.
- **`Run-Hidden.vbs`** — double-click to run quietly with no window. Stop it by
  ending the `powershell` process in Task Manager.
- **`Jiggle.ps1`** — the underlying script. Run directly for options:

```
powershell -NoProfile -ExecutionPolicy Bypass -File .\Jiggle.ps1 -Mode Mouse -IntervalSeconds 30 -RespectActivity
```

**Parameters**

- `-IntervalSeconds <n>` — seconds between jiggles (default `60`).
- `-Mode Invisible|Mouse` — `Invisible` presses the F15 key (nothing moves
  on screen); `Mouse` nudges the cursor one pixel and returns it. Default
  `Invisible`.
- `-RespectActivity` — skip a cycle if you moved the mouse or typed within the
  interval, so it never fights you.

No installation, no admin rights, no files written to the host beyond the stick.

---

## B. Tray app (single portable .exe)

The `src/PeripheralCompanion/` project is a Windows Forms system-tray app. Build
it once on any Windows 11 machine with the
[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), then copy the
resulting single `.exe` to your USB stick.

```
build\publish.cmd
```

or

```
powershell -ExecutionPolicy Bypass -File build\publish.ps1
```

Output: **`build\dist\PeripheralCompanion.exe`** — a self-contained, single-file
executable. No .NET runtime needs to be installed on the machine that runs it.

### Using it

Double-click `PeripheralCompanion.exe`. A mouse-shaped icon appears in the
system tray. Right-click it for the menu:

- **Active** — start/stop (double-click the icon also toggles).
- **Interval** — 30 s, 1 min, 2 min, or 5 min.
- **Method** — Invisible (F15 key) or Mouse nudge (1 px).
- **Pause while I'm using the PC** — only jiggles when genuinely idle.
- **Keep display awake** — also asks Windows not to sleep or blank the screen.
- **Start active on launch** — begin working the moment it opens.
- **About**, **Exit**.

Settings are saved to `settings.json` **next to the executable**, so your
configuration travels on the stick and nothing is left in the host user
profile.

To make it a true "plug and go" tool, place a shortcut to
`PeripheralCompanion.exe` in the machine's `shell:startup` folder, or just
launch it from the stick when needed.

---

## How it works

- **F15 key** (`Invisible` mode): F15 is a real virtual key that no physical
  keyboard sends. Pressing it resets the Windows idle timer without moving the
  cursor or affecting any application.
- **Mouse nudge** (`Mouse` mode): moves the cursor one pixel and immediately
  back via relative `SendInput` / `SetCursorPos`.
- **Idle-aware**: uses `GetLastInputInfo` to detect real activity and stays out
  of the way when you are actually using the machine.
- **Stay-awake**: uses `SetThreadExecutionState` to optionally keep the display
  and system from sleeping.

## Layout

```
portable/                 Zero-build PowerShell version
  Jiggle.ps1
  Run-Visible.cmd
  Run-Hidden.vbs
src/PeripheralCompanion/   Windows Forms tray app (C#, .NET 8)
  Program.cs
  TrayApplicationContext.cs
  JiggleEngine.cs
  NativeMethods.cs
  Settings.cs
  app.manifest
  PeripheralCompanion.csproj
build/                     Publish scripts -> build/dist/PeripheralCompanion.exe
  publish.cmd
  publish.ps1
```

## Requirements

- Windows 11 (also works on Windows 10).
- Portable script: Windows PowerShell 5.1 (built into Windows) — no install.
- Tray app: .NET 8 SDK to build; the published `.exe` needs nothing at runtime.
