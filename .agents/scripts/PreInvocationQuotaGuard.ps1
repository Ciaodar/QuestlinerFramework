$ErrorActionPreference = "SilentlyContinue"

$WorkspaceRoot = "c:\Users\meteh\RiderProjects\QuestlinerFramework"
$AgentsDir = Join-Path $WorkspaceRoot ".agents"
$WatchdogScript = Join-Path $AgentsDir "scripts\QuotaWatchdog.ps1"
$WatchdogPidPath = Join-Path $AgentsDir "quota_watchdog.pid"
$LiveStatusPath = Join-Path $AgentsDir "quota_live_status.json"
$LastTriggeredPromptPath = Join-Path $AgentsDir "prompts\KOD_5682_LAST_TRIGGERED.md"

# Read hook payload from stdin
$stdinText = [Console]::In.ReadToEnd()
$sessionId = "383ce584-3971-4efe-bd1d-24f2b857dc0a"
if (-not [string]::IsNullOrWhiteSpace($stdinText)) {
    try {
        $hookInput = $stdinText | ConvertFrom-Json
        if (-not [string]::IsNullOrWhiteSpace($hookInput.session_id)) {
            $sessionId = $hookInput.session_id
        }
    } catch {}
}

# 1. Ensure the 10-second QuotaWatchdog daemon is alive (Self-Healing Auto-Start)
$isDaemonRunning = $false
if (Test-Path $WatchdogPidPath) {
    $savedPid = (Get-Content $WatchdogPidPath -Raw).Trim()
    if ($savedPid -match '^\d+$') {
        $proc = Get-Process -Id ([int]$savedPid) -ErrorAction SilentlyContinue
        if ($null -ne $proc) {
            $isDaemonRunning = $true
        }
    }
}

if (-not $isDaemonRunning -and (Test-Path $WatchdogScript)) {
    Start-Process -FilePath "powershell.exe" -ArgumentList "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$WatchdogScript`" -ConversationId `"$sessionId`"" -WindowStyle Hidden | Out-Null
}

# 2. Check cached live quota status (fast <20ms synchronous check)
if (Test-Path $LiveStatusPath) {
    try {
        $status = Get-Content $LiveStatusPath -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach ($m in $status.monitoredModels) {
            if ([double]$m.remainingFraction -lt [double]$status.thresholdFraction) {
                $promptText = "ACIL DURUM: KOD 5682 (KOTA) - $($m.label) kotasi %$($m.remainingPercent) seviyesine dustu! Derhal KOD 5682 Guvenli Duraklatma Protokolunu uygula."
                if (Test-Path $LastTriggeredPromptPath) {
                    $promptText = Get-Content $LastTriggeredPromptPath -Raw -Encoding UTF8
                }
                $out = @{
                    userMessage        = "🚨 [KOD 5682 (KOTA)] $($m.label) kotasi %$($m.remainingPercent) (< %10)! Guvenli duraklatma protokolu enjekte edildi."
                    hookSpecificOutput = @{
                        hookEventName     = "PreInvocation"
                        additionalContext = $promptText
                    }
                }
                [Console]::Out.Write(($out | ConvertTo-Json -Depth 5 -Compress))
                exit 0
            }
        }
    } catch {}
}

[Console]::Out.Write("{}")
exit 0
