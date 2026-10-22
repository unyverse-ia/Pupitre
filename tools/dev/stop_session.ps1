. "C:\Users\conta\AppData\Local\Temp\claude\C--Users-conta-Desktop-Claude-Code\2a4c2e22-2236-4ad7-ad9d-7a81cdc4c311\scratchpad\t11lib.ps1"
Add-Type -TypeDefinition @"
using System; using System.Runtime.InteropServices; using System.Collections.Generic;
public static class AllWins {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc p, IntPtr l);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  public static List<long> Of(uint pid) { var r = new List<long>(); EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p == pid) r.Add((long)h); return true; }, IntPtr.Zero); return r; }
}
"@
$p = Get-Process Pupitre -ErrorAction SilentlyContinue
if (-not $p) { "deja arrete"; exit 0 }
"pid " + $p.Id + " : envoi WM_QUERYENDSESSION"
foreach ($h in [AllWins]::Of($p.Id)) { [void][T11]::Send($h, 0x11, 0, 0) }
for ($i = 0; $i -lt 16; $i++) { Start-Sleep -Milliseconds 500; if (-not (Get-Process Pupitre -ErrorAction SilentlyContinue)) { "arrete proprement"; exit 0 } }
Get-Process Pupitre -ErrorAction SilentlyContinue | Stop-Process -Force
"arrete de force"
