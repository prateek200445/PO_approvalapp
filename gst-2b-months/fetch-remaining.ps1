$ErrorActionPreference = "Stop"
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class GstUi {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
  [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint data, UIntPtr extra);
  public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
  public static string Title(IntPtr hWnd) {
    var sb = new StringBuilder(300);
    GetWindowText(hWnd, sb, sb.Capacity);
    return sb.ToString();
  }
  public static void Click(int x, int y) {
    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(x, y);
    mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
    System.Threading.Thread.Sleep(60);
    mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
  }
}
"@ -ReferencedAssemblies System.Windows.Forms, System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$hwnd = (Get-Process -Id 29004).MainWindowHandle
if ($hwnd -eq [IntPtr]::Zero) { throw "GST window is not open." }
$scale = [GstUi]::GetDpiForWindow($hwnd) / 96.0
if ($scale -le 0) { $scale = 1 }
function L([int]$physicalX, [int]$physicalY) {
  [GstUi]::Click([int]($physicalX / $scale), [int]($physicalY / $scale))
}
function Title { [GstUi]::Title($hwnd) }
function Shot([string]$path) {
  $rect = New-Object GstUi+RECT
  [GstUi]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
  $w = $rect.Right - $rect.Left
  $h = $rect.Bottom - $rect.Top
  $bmp = New-Object System.Drawing.Bitmap $w, $h
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc()
  [GstUi]::PrintWindow($hwnd, $hdc, 2) | Out-Null
  $g.ReleaseHdc($hdc)
  $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
}
function OpenDashboard {
  [GstUi]::ShowWindow($hwnd, 9) | Out-Null
  [GstUi]::SetForegroundWindow($hwnd) | Out-Null
  Start-Sleep -Milliseconds 300
  [System.Windows.Forms.Clipboard]::SetText("https://return.gst.gov.in/returns/auth/dashboard")
  [System.Windows.Forms.SendKeys]::SendWait("^l")
  Start-Sleep -Milliseconds 200
  [System.Windows.Forms.SendKeys]::SendWait("^v")
  Start-Sleep -Milliseconds 200
  [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
  $deadline = (Get-Date).AddSeconds(20)
  while ((Get-Date) -lt $deadline -and (Title -notmatch "User Dashboard")) { Start-Sleep -Milliseconds 500 }
}
function ChromeBack {
  $root = [System.Windows.Automation.AutomationElement]::RootElement
  $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, 29004)
  $win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
  $btnType = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
  foreach ($b in $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnType)) {
    if ($b.Current.Name -eq "Back") {
      $b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
      return
    }
  }
  throw "Chrome Back was not found."
}
function TaxPeriod([string]$path) {
  $zip = [IO.Compression.ZipFile]::OpenRead($path)
  try {
    $entry = $zip.GetEntry("xl/sharedStrings.xml")
    $reader = New-Object IO.StreamReader($entry.Open())
    $text = $reader.ReadToEnd()
    $reader.Close()
  } finally { $zip.Dispose() }
  $vals = [regex]::Matches($text, '>([^<]{1,40})<') | ForEach-Object { $_.Groups[1].Value }
  $at = 0..($vals.Count - 1) | Where-Object { $vals[$_] -eq "Tax Period" } | Select-Object -First 1
  if ($null -eq $at) { return "" }
  return [string]$vals[$at + 1]
}
function KnownFiles {
  @(Get-ChildItem "$env:TEMP\playwright-artifacts-*" -File -Recurse -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)
}
function WaitFile($known) {
  $deadline = (Get-Date).AddSeconds(25)
  while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 1
    $now = Get-ChildItem "$env:TEMP\playwright-artifacts-*" -File -Recurse -ErrorAction SilentlyContinue |
      Where-Object { $known -notcontains $_.FullName -and $_.Length -gt 8000 -and $_.LastWriteTime -gt (Get-Date).AddMinutes(-2) }
    foreach ($file in $now) {
      $stream = [IO.File]::Open($file.FullName, "Open", "Read", "ReadWrite")
      $buf = New-Object byte[] 2
      [void]$stream.Read($buf, 0, 2)
      $stream.Close()
      if ($buf[0] -eq 80 -and $buf[1] -eq 75) { return $file }
    }
  }
  return $null
}
function FocusClick([int]$x, [int]$y, [string]$keys) {
  [GstUi]::ShowWindow($hwnd, 9) | Out-Null
  [GstUi]::SetForegroundWindow($hwnd) | Out-Null
  Start-Sleep -Milliseconds 200
  L $x $y
  Start-Sleep -Milliseconds 250
  if ($keys) {
    [System.Windows.Forms.SendKeys]::SendWait($keys)
    Start-Sleep -Milliseconds 300
  }
}
function ClickBlueButton([int]$which, [int]$fromY) {
  $shot = "c:\Users\Admin\Desktop\approval\PO_approvalapp\gst-2b-months\live.png"
  Shot $shot
  $bmp = [System.Drawing.Bitmap]::FromFile($shot)
  try {
    $bands = @()
    $open = $false
    $start = 0
    for ($y = $fromY; $y -lt $bmp.Height; $y++) {
      $count = 0
      for ($x = 200; $x -lt ($bmp.Width - 200); $x += 4) {
        $color = $bmp.GetPixel($x, $y)
        if ($color.B -gt 110 -and $color.R -lt 80 -and $color.G -lt 130 -and $color.G -gt 30) { $count++ }
      }
      if ($count -gt 12 -and -not $open) { $open = $true; $start = $y }
      elseif ($count -le 12 -and $open) {
        $open = $false
        if (($y - $start) -gt 18) { $bands += ,@($start, ($y - 1)) }
      }
    }
    if ($bands.Count -le $which) { throw "Blue button $which was not on the page. Title=$(Title)" }
    $band = $bands[$which]
    $midY = [int](($band[0] + $band[1]) / 2)
    $runs = @()
    $openX = $false
    $startX = 0
    for ($x = 0; $x -lt $bmp.Width; $x++) {
      $color = $bmp.GetPixel($x, $midY)
      $blue = $color.B -gt 110 -and $color.R -lt 90
      if ($blue -and -not $openX) { $openX = $true; $startX = $x }
      elseif (-not $blue -and $openX) { $openX = $false; if (($x - $startX) -gt 40) { $runs += ,@($startX, ($x - 1)) } }
    }
    if ($runs.Count -eq 0) { throw "Blue button $which had no clickable area." }
    $run = $runs[-1]
    $rect = New-Object GstUi+RECT
    [GstUi]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
    $cx = $rect.Left + [int](($run[0] + $run[1]) / 2)
    $cy = $rect.Top + $midY
    "button $which at $cx,$cy"
    FocusClick $cx $cy ""
  } finally { $bmp.Dispose() }
}

$months = @(
  @{ Name = "June"; Quarter = "{HOME}"; Period = "{HOME}{DOWN}{DOWN}"; File = "2026-06" },
  @{ Name = "July"; Quarter = "{HOME}{DOWN}"; Period = "{HOME}"; File = "2026-07" },
  @{ Name = "August"; Quarter = "{HOME}{DOWN}"; Period = "{HOME}{DOWN}"; File = "2026-08" },
  @{ Name = "September"; Quarter = "{HOME}{DOWN}"; Period = "{HOME}{DOWN}{DOWN}"; File = "2026-09" }
)
$dir = "c:\Users\Admin\Desktop\approval\PO_approvalapp\gst-2b-months"
foreach ($month in $months) {
  if ((Title) -notmatch "User Dashboard") { throw "Not on the returns dashboard before $($month.Name). Title=$(Title)" }
  FocusClick 666 598 $month.Quarter
  FocusClick 953 598 $month.Period
  FocusClick 1184 562 ""
  Start-Sleep -Seconds 2
  $known = KnownFiles
  ClickBlueButton 0 520
  $deadline = (Get-Date).AddSeconds(12)
  while ((Get-Date) -lt $deadline -and (Title -notmatch "GSTR2B")) { Start-Sleep -Milliseconds 500 }
  if ((Title) -notmatch "GSTR2B") { throw "GSTR-2B page did not open for $($month.Name). Title=$(Title)" }
  ClickBlueButton 1 200
  $file = WaitFile $known
  if (-not $file) { throw "No workbook arrived for $($month.Name)." }
  $period = TaxPeriod $file.FullName
  "downloaded $($month.Name) filePeriod=$period size=$($file.Length)"
  if ($period -ne $month.Name) { throw "Stopped. Expected $($month.Name) but the workbook says $period. It was not saved." }
  $dest = Join-Path $dir "oswal-$($month.File).xlsx"
  Copy-Item $file.FullName $dest -Force
  $env:ASPNETCORE_CONTENTROOT = "c:\Users\Admin\Desktop\approval\PO_approvalapp\POApprovalAPI"
  dotnet "c:\Users\Admin\Desktop\approval\PO_approvalapp\POApprovalAPI\bin\Debug\net10.0\POApprovalAPI.dll" --import-2b-portal $dest
  if ($LASTEXITCODE -ne 0) { throw "Store failed for $($month.Name)." }
  ChromeBack
  Start-Sleep -Seconds 2
}
"done"
