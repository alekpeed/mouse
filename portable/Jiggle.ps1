<#
.SYNOPSIS
    Portable, zero-install anti-idle utility for Windows 11.

.DESCRIPTION
    Keeps the current session marked as active by issuing periodic synthetic
    input. Designed to run directly from a USB stick with no build step and no
    installation. Presents itself as a lightweight peripheral utility.

    Use only on machines you are authorized to operate.

.PARAMETER IntervalSeconds
    Seconds between jiggles. Default 60.

.PARAMETER Mode
    Invisible : press the F15 key (no cursor movement, nothing visible).
    Mouse     : nudge the cursor one pixel and return it.
    Default Invisible.

.PARAMETER RespectActivity
    When set, skip a cycle if real user input occurred within the interval.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Jiggle.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Jiggle.ps1 -Mode Mouse -IntervalSeconds 30
#>
[CmdletBinding()]
param(
    [int]$IntervalSeconds = 60,
    [ValidateSet('Invisible', 'Mouse')]
    [string]$Mode = 'Invisible',
    [switch]$RespectActivity
)

$ErrorActionPreference = 'Stop'

# Ensure System.Drawing is available for the Point type used below.
Add-Type -AssemblyName System.Drawing

Add-Type -Namespace Win32 -Name Native -MemberDefinition @'
    [StructLayout(LayoutKind.Sequential)]
    public struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

    [DllImport("user32.dll")]
    public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("kernel32.dll")]
    public static extern uint GetTickCount();

    [DllImport("kernel32.dll")]
    public static extern uint SetThreadExecutionState(uint esFlags);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, System.IntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out System.Drawing.Point p);
'@ -ReferencedAssemblies System.Drawing

$ES_CONTINUOUS       = [uint32]'0x80000000'
$ES_SYSTEM_REQUIRED  = [uint32]'0x00000001'
$ES_DISPLAY_REQUIRED = [uint32]'0x00000002'
$VK_F15              = [byte]0x7E
$KEYEVENTF_KEYUP     = [uint32]0x0002

function Get-IdleMilliseconds {
    $info = New-Object Win32.Native+LASTINPUTINFO
    $info.cbSize = [System.Runtime.InteropServices.Marshal]::SizeOf($info)
    [void][Win32.Native]::GetLastInputInfo([ref]$info)
    return [Win32.Native]::GetTickCount() - $info.dwTime
}

function Invoke-Jiggle {
    if ($Mode -eq 'Mouse') {
        $p = New-Object System.Drawing.Point
        [void][Win32.Native]::GetCursorPos([ref]$p)
        [void][Win32.Native]::SetCursorPos($p.X + 1, $p.Y)
        Start-Sleep -Milliseconds 40
        [void][Win32.Native]::SetCursorPos($p.X, $p.Y)
    }
    else {
        [Win32.Native]::keybd_event($VK_F15, 0, 0, [System.IntPtr]::Zero)
        [Win32.Native]::keybd_event($VK_F15, 0, $KEYEVENTF_KEYUP, [System.IntPtr]::Zero)
    }
}

Write-Host "Peripheral Companion (portable) - active." -ForegroundColor Green
Write-Host ("Mode: {0}  Interval: {1}s  RespectActivity: {2}" -f $Mode, $IntervalSeconds, [bool]$RespectActivity)
Write-Host "Press Ctrl+C to stop."

$intervalMs = $IntervalSeconds * 1000

try {
    while ($true) {
        # Ask Windows to keep the display and system awake for this cycle.
        [void][Win32.Native]::SetThreadExecutionState($ES_CONTINUOUS -bor $ES_SYSTEM_REQUIRED -bor $ES_DISPLAY_REQUIRED)

        if (-not ($RespectActivity -and (Get-IdleMilliseconds) -lt $intervalMs)) {
            Invoke-Jiggle
        }

        Start-Sleep -Seconds $IntervalSeconds
    }
}
finally {
    # Restore normal power policy on exit.
    [void][Win32.Native]::SetThreadExecutionState($ES_CONTINUOUS)
    Write-Host "Stopped. Power policy restored."
}
