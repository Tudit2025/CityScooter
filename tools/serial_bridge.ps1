<#
.SYNOPSIS
  Serial bridge: lets the Unity Editor (Play mode) talk to a real micro:bit over USB.

.DESCRIPTION
  The Editor does not open serial ports itself; the Web build uses the browser's Web Serial instead.
  This script reads the micro:bit's USB serial port and forwards every line to the Editor's
  UdpDeviceTransport (127.0.0.1:47800) as "usb1|<line>", and writes game messages
  (go / crash / gift / spd:N) back to the board. It finds the micro:bit by its USB vendor id (0D28)
  and reconnects by itself after the board is unplugged or re-flashed.
  Uses only Windows PowerShell and .NET: nothing to install.

  The board must run a program that speaks the protocol (docs/protocol.md), for example
  tools/labcheck/labcheck.py or the Lesson 1 starter, flashed from python.microbit.org.
  Close any browser tab that is connected to the same board first (one program per port).

.EXAMPLE
  tools\serial_bridge.cmd                  (double-click, or run from a terminal)
  powershell -ExecutionPolicy Bypass -File tools\serial_bridge.ps1 -Port COM5 -Id usb2
#>
param(
    [string]$Port = "",
    [int]$Baud = 115200,
    [int]$UdpPort = 47800,
    [string]$Id = "usb1",
    [int]$DurationSeconds = 0,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

function Find-MicrobitPort {
    $dev = Get-CimInstance Win32_PnPEntity -ErrorAction SilentlyContinue |
        Where-Object { $_.PNPDeviceID -match 'VID_0D28' -and $_.Name -match '\(COM\d+\)' } |
        Select-Object -First 1
    if ($dev) { return [regex]::Match($dev.Name, 'COM\d+').Value }
    return $null
}

function Open-Board([string]$name) {
    $sp = New-Object System.IO.Ports.SerialPort $name, $Baud
    $sp.Encoding = [System.Text.Encoding]::ASCII
    $sp.Open()
    return $sp
}

$udp = New-Object System.Net.Sockets.UdpClient 0
# Windows reports "port unreachable" from earlier sends (Unity not in Play mode yet) as a receive error; ignore it.
$udp.Client.IOControl(-1744830452, [byte[]](0, 0, 0, 0), $null) | Out-Null
$game = New-Object System.Net.IPEndPoint ([System.Net.IPAddress]::Loopback, $UdpPort)

$deadline = if ($DurationSeconds -gt 0) { (Get-Date).AddSeconds($DurationSeconds) } else { [datetime]::MaxValue }
$sp = $null
$buf = ""
$nextTry = Get-Date
$lines = 0
$fromGame = 0
$lastSteer = "-"
$version = $null
$openedAt = Get-Date
$warnedSilent = $false
$nextStatus = (Get-Date).AddSeconds(5)

Write-Host "CityScooter serial bridge -> Unity Editor on 127.0.0.1:$UdpPort as '$Id'. Ctrl+C to stop."
Write-Host "In Unity: press Play, then press A on the micro:bit (or Space) to start a run. F1 shows devices."

try {
    while ((Get-Date) -lt $deadline) {
        if ($null -eq $sp) {
            if ((Get-Date) -ge $nextTry) {
                $nextTry = (Get-Date).AddSeconds(1)
                $name = if ($Port) { $Port } else { Find-MicrobitPort }
                if (-not $name) {
                    Write-Host "Waiting for a micro:bit on USB..."
                    $nextTry = (Get-Date).AddSeconds(3)
                } else {
                    try {
                        $sp = Open-Board $name
                        $buf = ""
                        $openedAt = Get-Date
                        $warnedSilent = $false
                        Write-Host "Connected to micro:bit on $name"
                    } catch {
                        Write-Host "Cannot open ${name}: $($_.Exception.Message)  (is a browser tab or another program using the board?)"
                        $sp = $null
                        $nextTry = (Get-Date).AddSeconds(3)
                    }
                }
            }
        }

        if ($null -ne $sp) {
            try {
                if ($sp.BytesToRead -gt 0) {
                    $buf += $sp.ReadExisting()
                    $out = New-Object System.Collections.Generic.List[string]
                    while (($i = $buf.IndexOf("`n")) -ge 0) {
                        $line = $buf.Substring(0, $i).TrimEnd("`r")
                        $buf = $buf.Substring($i + 1)
                        if ($line.Length -eq 0) { continue }
                        $lines++
                        $out.Add("$Id|$line")
                        if ($line.StartsWith("st:")) { $lastSteer = $line.Substring(3) }
                        elseif ($line.StartsWith("hi:")) {
                            if ($version -ne $line.Substring(3)) { $version = $line.Substring(3); Write-Host "Board says it is $($version.ToUpper())" }
                        }
                        elseif ($line -ne "br" -and -not $Quiet) { Write-Host "board > $line" }
                    }
                    if ($buf.Length -gt 1000) { $buf = "" }
                    if ($out.Count -gt 0) {
                        $bytes = [System.Text.Encoding]::UTF8.GetBytes(($out -join "`n") + "`n")
                        [void]$udp.Send($bytes, $bytes.Length, $game)
                    }
                }
            } catch {
                Write-Host "Board disconnected ($($_.Exception.Message)); reconnecting..."
                try { $sp.Close() } catch { }
                $sp = $null
                $nextTry = (Get-Date).AddSeconds(1)
            }
            if ($null -ne $sp -and $lines -eq 0 -and -not $warnedSilent -and ((Get-Date) - $openedAt).TotalSeconds -gt 3) {
                $warnedSilent = $true
                Write-Host "The board is connected but sends nothing. Flash a protocol program (tools/labcheck/labcheck.py or the starter) from python.microbit.org, then press reset."
            }
        }

        while ($udp.Available -gt 0) {
            $from = New-Object System.Net.IPEndPoint ([System.Net.IPAddress]::Any, 0)
            $data = $udp.Receive([ref]$from)
            $text = [System.Text.Encoding]::UTF8.GetString($data)
            $fromGame++
            if ($null -ne $sp) { try { $sp.Write($text) } catch { } }
            if (-not $Quiet) { foreach ($l in $text.Split("`n")) { if ($l.Trim()) { Write-Host "game  > $l" } } }
        }

        if ((Get-Date) -ge $nextStatus) {
            $nextStatus = (Get-Date).AddSeconds(5)
            if (-not $Quiet -and $null -ne $sp) { Write-Host ("[status] {0} lines from board, last steer {1}, {2} messages from the game" -f $lines, $lastSteer, $fromGame) }
        }
        Start-Sleep -Milliseconds 5
    }
} finally {
    if ($null -ne $sp) { try { $sp.Close() } catch { } }
    $udp.Close()
    Write-Host ("Bridge stopped: {0} lines from board, {1} messages from the game." -f $lines, $fromGame)
}
