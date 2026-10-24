param([Parameter(Mandatory=$true)][int]$DemoProcessId)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class SailingOutPreviewWindow {
    public delegate bool EnumWindowsCallback(IntPtr window, IntPtr data);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr data);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr window, int command);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr window, StringBuilder text, int length);
}
'@
$script:shownDemoWindows = 0
$callback = [SailingOutPreviewWindow+EnumWindowsCallback]{
    param($window, $data)
    [uint32]$windowProcessId = 0
    [void][SailingOutPreviewWindow]::GetWindowThreadProcessId($window, [ref]$windowProcessId)
    if ($windowProcessId -eq $DemoProcessId) {
        $className = [System.Text.StringBuilder]::new(256)
        [void][SailingOutPreviewWindow]::GetClassName($window, $className, 256)
        if ($className.ToString() -eq 'UnityWndClass') {
            [void][SailingOutPreviewWindow]::ShowWindowAsync($window, 4)
            $script:shownDemoWindows++
        }
    }
    return $true
}
for ($attempt=0; $attempt -lt 50 -and $script:shownDemoWindows -eq 0; $attempt++) {
    [void][SailingOutPreviewWindow]::EnumWindows($callback,[IntPtr]::Zero)
    if ($script:shownDemoWindows -eq 0) { Start-Sleep -Milliseconds 200 }
}
Write-Output "Shown demo windows: $script:shownDemoWindows"
