' Launches the portable anti-idle utility with no visible console window,
' so it runs quietly in the background like a resident peripheral utility.
' Double-click this file to start. Use Task Manager to end "powershell" to stop.
Dim shell, scriptDir
Set shell = CreateObject("WScript.Shell")
scriptDir = Left(WScript.ScriptFullName, InStrRev(WScript.ScriptFullName, "\"))
shell.CurrentDirectory = scriptDir
shell.Run "powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & scriptDir & "Jiggle.ps1""", 0, False
