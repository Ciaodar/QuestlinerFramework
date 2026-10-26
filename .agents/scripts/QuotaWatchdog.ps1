param(
    [string]$ConversationId = "383ce584-3971-4efe-bd1d-24f2b857dc0a",
    [double]$Threshold = 0.10,
    [int]$IntervalSeconds = 10,
    [string]$PrimaryMainModel = "MODEL_PLACEHOLDER_M417",   # Gemini 4 Argon (High)
    [string]$FallbackMainModel = "MODEL_PLACEHOLDER_M318",  # Gemini 3.8 Flash (High) for Mudur Fallback
    [string]$SubagentModel = "MODEL_PLACEHOLDER_M319",      # Gemini 3.8 Flash (Medium) for Subagents
    [int]$MaxTicks = 0,
    [switch]$CheckOnce,
    [switch]$SimulateArgonExhausted,
    [switch]$SimulateLowQuota
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$WorkspaceRoot = "c:\Users\meteh\RiderProjects\QuestlinerFramework"
$AgentsDir = Join-Path $WorkspaceRoot ".agents"
$PromptsDir = Join-Path $AgentsDir "prompts"
$PausePromptTemplatePath = Join-Path $PromptsDir "KOD_5682_KOTA_PROMPT.md"
$FallbackPromptTemplatePath = Join-Path $PromptsDir "KOD_5682_FALLBACK_FLASH_PROMPT.md"
$LastTriggeredPromptPath = Join-Path $PromptsDir "KOD_5682_LAST_TRIGGERED.md"
$LiveStatusPath = Join-Path $AgentsDir "quota_live_status.json"
$AlertStatePath = Join-Path $AgentsDir "quota_alert_state.json"
$WatchdogPidPath = Join-Path $AgentsDir "quota_watchdog.pid"
$WatchdogLogPath = Join-Path $AgentsDir "quota_watchdog.log"
$StatePbtxtPath = "C:\Users\meteh\.gemini\antigravity\antigravity_state.pbtxt"
$AgentApiBat = "C:\Users\meteh\.gemini\antigravity\bin\agentapi.bat"

if (-not (Test-Path $PromptsDir)) {
    New-Item -ItemType Directory -Path $PromptsDir -Force | Out-Null
}

function Get-QuotaBar {
    param([double]$Fraction, [int]$Width = 16)
    $clamped = [Math]::Max(0.0, [Math]::Min(1.0, $Fraction))
    $filled = [int][Math]::Round($clamped * $Width)
    $empty = $Width - $filled
    return "[" + ("#" * $filled) + ("-" * $empty) + "]"
}

function Format-DurationShort {
    param([int]$TotalSeconds)
    $ts = [TimeSpan]::FromSeconds([Math]::Max(0, $TotalSeconds))
    if ($ts.TotalHours -ge 1) {
        return "{0:00}h {1:00}m {2:00}s" -f [int][Math]::Floor($ts.TotalHours), $ts.Minutes, $ts.Seconds
    }
    return "{0:00}m {1:00}s" -f $ts.Minutes, $ts.Seconds
}

function Get-ActiveMainModelPlaceholder {
    if (Test-Path $StatePbtxtPath) {
        $content = Get-Content $StatePbtxtPath -Raw -ErrorAction SilentlyContinue
        if ($content -match 'last_selected_agent_model:\s*(\S+)') {
            return $matches[1].Trim()
        }
    }
    return $PrimaryMainModel
}

function Get-LanguageServerEndpoint {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $procs = Get-CimInstance Win32_Process | Where-Object { $_.Name -like 'language_server*' }
    foreach ($proc in $procs) {
        if ($proc.CommandLine -match '--csrf_token\s+([a-f0-9\-]+)') {
            $csrf = $matches[1]
            $conns = Get-NetTCPConnection -OwningProcess $proc.ProcessId -State Listen -ErrorAction SilentlyContinue
            foreach ($conn in $conns) {
                $port = $conn.LocalPort
                $url = "http://127.0.0.1:$port/exa.language_server_pb.LanguageServerService/GetUserStatus"
                try {
                    $headers = @{
                        "x-codeium-csrf-token"     = $csrf
                        "Connect-Protocol-Version" = "1"
                    }
                    $body = '{"metadata":{"ideName":"antigravity","extensionName":"antigravity","locale":"en"}}'
                    $resp = Invoke-RestMethod -Uri $url -Method Post -Headers $headers -ContentType "application/json" -Body $body -TimeoutSec 3 -ErrorAction Stop
                    if ($null -ne $resp.userStatus.cascadeModelConfigData.clientModelConfigs) {
                        $sw.Stop()
                        return @{
                            Port      = $port
                            ServerPid = $proc.ProcessId
                            LatencyMs = $sw.ElapsedMilliseconds
                            Csrf      = $csrf
                            Configs   = $resp.userStatus.cascadeModelConfigData.clientModelConfigs
                        }
                    }
                } catch {}
            }
        }
    }
    return $null
}

function Set-AntigravitySelectedModel {
    param(
        [int]$Port,
        [string]$Csrf,
        [string]$ModelPlaceholder
    )
    $url = "http://127.0.0.1:$Port/exa.language_server_pb.LanguageServerService/JetboxWriteState"
    $headers = @{
        "x-codeium-csrf-token"     = $Csrf
        "Connect-Protocol-Version" = "1"
    }
    $body = (@{
        appState = @{
            lastSelectedAgentModel = $ModelPlaceholder
        }
    } | ConvertTo-Json -Compress)

    $null = Invoke-RestMethod -Uri $url -Method Post -Headers $headers -ContentType "application/json" -Body $body -TimeoutSec 3 -ErrorAction Stop
}

function Format-Kod5682PausePrompt {
    param(
        [string]$ModelLabel,
        [string]$ModelId,
        [double]$RemainingFraction,
        [string]$ResetTimeUtcStr
    )

    $remainingPercent = [Math]::Round($RemainingFraction * 100, 2).ToString("0.00", [System.Globalization.CultureInfo]::InvariantCulture)
    $nowUtc = (Get-Date).ToUniversalTime()
    $resetUtc = [DateTime]::Parse($ResetTimeUtcStr).ToUniversalTime()
    $resetLocal = $resetUtc.ToLocalTime()

    $rawSeconds = [int][Math]::Ceiling(($resetUtc - $nowUtc).TotalSeconds) + 60
    $waitSeconds = [Math]::Max(120, $rawSeconds)
    $waitMinutes = [Math]::Round($waitSeconds / 60.0, 1).ToString("0.0", [System.Globalization.CultureInfo]::InvariantCulture)

    $template = Get-Content -Path $PausePromptTemplatePath -Raw -Encoding UTF8
    $rendered = $template.
        Replace("{{MODEL_LABEL}}", $ModelLabel).
        Replace("{{MODEL_ID}}", $ModelId).
        Replace("{{REMAINING_PERCENT}}", $remainingPercent).
        Replace("{{RESET_TIME_UTC}}", $resetUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'")).
        Replace("{{RESET_TIME_LOCAL}}", $resetLocal.ToString("yyyy-MM-dd HH:mm:ss zzz")).
        Replace("{{WAIT_SECONDS}}", $waitSeconds.ToString()).
        Replace("{{WAIT_MINUTES}}", $waitMinutes).
        Replace("{{CONVERSATION_ID}}", $ConversationId)

    return @{
        RenderedPrompt = $rendered
        WaitSeconds    = $waitSeconds
        ResetUtc       = $resetUtc.ToString("o")
        ResetLocal     = $resetLocal.ToString("yyyy-MM-dd HH:mm:ss zzz")
        Percent        = $remainingPercent
    }
}

function Format-Kod5682FallbackPrompt {
    param(
        [object]$ArgonEntry,
        [object]$FlashEntry,
        [double]$EffectiveArgonFraction
    )

    $argonPercent = [Math]::Round($EffectiveArgonFraction * 100, 2).ToString("0.00", [System.Globalization.CultureInfo]::InvariantCulture)
    $flashPercent = [Math]::Round($FlashEntry.remainingFraction * 100, 2).ToString("0.00", [System.Globalization.CultureInfo]::InvariantCulture)

    $template = Get-Content -Path $FallbackPromptTemplatePath -Raw -Encoding UTF8
    $rendered = $template.
        Replace("{{ARGON_LABEL}}", $ArgonEntry.label).
        Replace("{{ARGON_PERCENT}}", $argonPercent).
        Replace("{{ARGON_RESET_LOCAL}}", $ArgonEntry.resetTimeLocal).
        Replace("{{FLASH_LABEL}}", $FlashEntry.label).
        Replace("{{FLASH_PLACEHOLDER}}", $FlashEntry.placeholder).
        Replace("{{FLASH_PERCENT}}", $flashPercent).
        Replace("{{FLASH_RESET_LOCAL}}", $FlashEntry.resetTimeLocal)

    return @{
        RenderedPrompt = $rendered
        ArgonPercent   = $argonPercent
        FlashPercent   = $flashPercent
    }
}

function Invoke-QuotaCheckIteration {
    param([int]$TickNumber = 1)

    $ep = Get-LanguageServerEndpoint
    if ($null -eq $ep) {
        Write-Warning "[QuotaWatchdog] Could not reach local Antigravity language_server GetUserStatus endpoint."
        return $null
    }

    $configs = $ep.Configs
    $activePlaceholder = Get-ActiveMainModelPlaceholder
    $targetPlaceholders = @($activePlaceholder, $PrimaryMainModel, $FallbackMainModel, $SubagentModel) | Select-Object -Unique

    $nowUtc = (Get-Date).ToUniversalTime()
    $monitoredList = @()
    $allSummaryList = @()
    $byPlaceholder = @{}

    foreach ($cfg in $configs) {
        $placeholder = $cfg.modelOrAlias.model
        $fraction = [double]$cfg.quotaInfo.remainingFraction
        $resetStr = [string]$cfg.quotaInfo.resetTime
        $resetDt = [DateTime]::Parse($resetStr).ToUniversalTime()
        $secondsToReset = [int][Math]::Max(0, [Math]::Ceiling(($resetDt - $nowUtc).TotalSeconds))

        $entry = [ordered]@{
            label             = $cfg.label
            modelId           = $cfg.modelId
            placeholder       = $placeholder
            remainingFraction = $fraction
            remainingPercent  = [Math]::Round($fraction * 100, 2)
            resetTimeUtc      = $resetStr
            resetTimeLocal    = $resetDt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz")
            secondsUntilReset = $secondsToReset
            isActiveMain      = ($placeholder -eq $activePlaceholder)
            isMonitored       = ($targetPlaceholders -contains $placeholder)
        }

        $allSummaryList += $entry
        $byPlaceholder[$placeholder] = $entry
        if ($targetPlaceholders -contains $placeholder) {
            $monitoredList += $entry
        }
    }

    $argonModel = $byPlaceholder[$PrimaryMainModel]
    $flashHighModel = $byPlaceholder[$FallbackMainModel]
    $flashMedModel = $byPlaceholder[$SubagentModel]

    $effArgonFraction = if ($null -ne $argonModel) { [double]$argonModel.remainingFraction } else { 1.0 }
    $effFlashFraction = if ($null -ne $flashMedModel) { [double]$flashMedModel.remainingFraction } else { 1.0 }

    if ($SimulateArgonExhausted) {
        $effArgonFraction = 0.075
    }
    if ($SimulateLowQuota) {
        $effArgonFraction = 0.050
        $effFlashFraction = 0.085
    }

    # Determine 3-Tier Quota Operational Mode:
    # 1. NORMAL_ARGON: Argon >= 10% and Flash >= 10%
    # 2. FALLBACK_FLASH: Argon < 10%, but Flash >= 10% -> Mudur switches to Gemini 3.8 Flash (High)
    # 3. PAUSE_ALL: Flash < 10% -> Full KOD 5682 Safe Pause & Schedule Timer
    $operationalMode = "NORMAL_ARGON"
    if ($effFlashFraction -lt $Threshold) {
        $operationalMode = "PAUSE_ALL"
    } elseif ($effArgonFraction -lt $Threshold) {
        $operationalMode = "FALLBACK_FLASH"
    }

    $prevAlert = $null
    if (Test-Path $AlertStatePath) {
        try {
            $prevAlert = Get-Content $AlertStatePath -Raw -Encoding UTF8 | ConvertFrom-Json
        } catch {}
    }

    if ($operationalMode -eq "NORMAL_ARGON") {
        if ($null -ne $prevAlert -and $prevAlert.mode -eq "FALLBACK_FLASH" -and (-not $SimulateArgonExhausted) -and (-not $SimulateLowQuota)) {
            if ($activePlaceholder -ne $PrimaryMainModel) {
                Set-AntigravitySelectedModel -Port $ep.Port -Csrf $ep.Csrf -ModelPlaceholder $PrimaryMainModel
                $activePlaceholder = $PrimaryMainModel
                Write-Host "[KOD 5682 AUTO-RESTORE] Argon quota restored (>=$($Threshold*100)%). Switched Mudur back to $($argonModel.label) ($PrimaryMainModel)." -ForegroundColor Cyan
            }
            Remove-Item -Path $AlertStatePath -Force -ErrorAction SilentlyContinue
        }
    }
    elseif ($operationalMode -eq "FALLBACK_FLASH") {
        $alertKey = "FALLBACK_FLASH|$($argonModel.resetTimeUtc)"
        $alreadyTriggered = ($null -ne $prevAlert -and $prevAlert.alertKey -eq $alertKey -and $prevAlert.conversationId -eq $ConversationId -and (-not $SimulateArgonExhausted))

        $fbFormatted = Format-Kod5682FallbackPrompt -ArgonEntry $argonModel -FlashEntry $flashHighModel -EffectiveArgonFraction $effArgonFraction
        [System.IO.File]::WriteAllText($LastTriggeredPromptPath, $fbFormatted.RenderedPrompt, [System.Text.UTF8Encoding]::new($false))

        if ($activePlaceholder -ne $FallbackMainModel) {
            Set-AntigravitySelectedModel -Port $ep.Port -Csrf $ep.Csrf -ModelPlaceholder $FallbackMainModel
            $activePlaceholder = $FallbackMainModel
        }

        if (-not $alreadyTriggered) {
            $alertState = [ordered]@{
                mode              = "FALLBACK_FLASH"
                alertKey          = $alertKey
                triggeredAtUtc    = $nowUtc.ToString("o")
                triggeredAtLocal  = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss zzz")
                fromModel         = $argonModel.label
                toModel           = $flashHighModel.label
                toPlaceholder     = $FallbackMainModel
                argonPercent      = $fbFormatted.ArgonPercent
                flashPercent      = $fbFormatted.FlashPercent
                argonResetUtc     = $argonModel.resetTimeUtc
                conversationId    = $ConversationId
                simulated         = [bool]$SimulateArgonExhausted
            }
            [System.IO.File]::WriteAllText($AlertStatePath, ($alertState | ConvertTo-Json -Depth 4), [System.Text.UTF8Encoding]::new($false))

            if (-not $SimulateArgonExhausted) {
                Write-Host "[KOD 5682-VARDIYA] Argon dropped to $($fbFormatted.ArgonPercent)%, Flash at $($fbFormatted.FlashPercent)%. Switched Mudur to $($flashHighModel.label)!" -ForegroundColor Yellow
                & $AgentApiBat send-message --title="KOD 5682-VARDIYA: OTOMATIK FLASH GECISI" $ConversationId $fbFormatted.RenderedPrompt
            } else {
                Set-AntigravitySelectedModel -Port $ep.Port -Csrf $ep.Csrf -ModelPlaceholder $PrimaryMainModel
                $activePlaceholder = $PrimaryMainModel
                Write-Host "[KOD 5682-VARDIYA SIMULATION] Verified live RPC switch -> $($flashHighModel.label) ($FallbackMainModel) & restored $($argonModel.label)." -ForegroundColor Yellow
            }
        }
    }
    elseif ($operationalMode -eq "PAUSE_ALL") {
        $alertKey = "PAUSE_ALL|$($flashMedModel.resetTimeUtc)"
        $alreadyTriggered = ($null -ne $prevAlert -and $prevAlert.alertKey -eq $alertKey -and $prevAlert.conversationId -eq $ConversationId -and (-not $SimulateLowQuota))

        $pauseFormatted = Format-Kod5682PausePrompt `
            -ModelLabel $flashMedModel.label `
            -ModelId $flashMedModel.modelId `
            -RemainingFraction $effFlashFraction `
            -ResetTimeUtcStr $flashMedModel.resetTimeUtc

        [System.IO.File]::WriteAllText($LastTriggeredPromptPath, $pauseFormatted.RenderedPrompt, [System.Text.UTF8Encoding]::new($false))

        if (-not $alreadyTriggered) {
            $alertState = [ordered]@{
                mode              = "PAUSE_ALL"
                alertKey          = $alertKey
                triggeredAtUtc    = $nowUtc.ToString("o")
                triggeredAtLocal  = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss zzz")
                modelLabel        = $flashMedModel.label
                modelId           = $flashMedModel.modelId
                remainingPercent  = $pauseFormatted.Percent
                resetTimeUtc      = $flashMedModel.resetTimeUtc
                resetTimeLocal    = $pauseFormatted.ResetLocal
                waitSeconds       = $pauseFormatted.WaitSeconds
                conversationId    = $ConversationId
                simulated         = [bool]$SimulateLowQuota
            }
            [System.IO.File]::WriteAllText($AlertStatePath, ($alertState | ConvertTo-Json -Depth 4), [System.Text.UTF8Encoding]::new($false))

            if (-not $SimulateLowQuota) {
                Write-Host "[KOD 5682 PAUSE TRIGGERED] Flash dropped to $($pauseFormatted.Percent)% (< $($Threshold*100)%). Sending emergency pause prompt to $ConversationId..." -ForegroundColor Red
                & $AgentApiBat send-message --title="ACIL DURUM: KOD 5682 (KOTA)" $ConversationId $pauseFormatted.RenderedPrompt
            } else {
                Write-Host "[KOD 5682 PAUSE SIMULATION] Rendered emergency pause prompt for $($flashMedModel.label) ($($pauseFormatted.Percent)%) -> $LastTriggeredPromptPath" -ForegroundColor Yellow
            }
        }
    }

    $statusPayload = [ordered]@{
        checkedAtUtc             = $nowUtc.ToString("o")
        checkedAtLocal           = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss zzz")
        watchdogPid              = $PID
        rpcPort                  = $ep.Port
        rpcLatencyMs             = $ep.LatencyMs
        conversationId           = $ConversationId
        operationalMode          = $operationalMode
        thresholdFraction        = $Threshold
        thresholdPercent         = [Math]::Round($Threshold * 100, 2)
        activeMainPlaceholder    = $activePlaceholder
        monitoredModels          = $monitoredList
        allModels                = $allSummaryList
    }

    $statusJson = $statusPayload | ConvertTo-Json -Depth 6
    [System.IO.File]::WriteAllText($LiveStatusPath, $statusJson, [System.Text.UTF8Encoding]::new($false))

    # Print rich debug telemetry to terminal on every tick
    $tsStr = (Get-Date).ToString("HH:mm:ss")
    $modeColor = switch ($operationalMode) {
        "NORMAL_ARGON"   { "Green" }
        "FALLBACK_FLASH" { "Yellow" }
        "PAUSE_ALL"      { "Red" }
        default          { "Cyan" }
    }
    Write-Host ""
    Write-Host ("[{0}] [TICK #{1:0000}] RPC 127.0.0.1:{2} ({3}ms) | GUARD: {4} | THRESHOLD: {5}%" -f $tsStr, $TickNumber, $ep.Port, $ep.LatencyMs, $operationalMode, ($Threshold * 100)) -ForegroundColor $modeColor

    $logLines = @("[$tsStr] [TICK #$TickNumber] Mode=$operationalMode | RPC=127.0.0.1:$($ep.Port) ($($ep.LatencyMs)ms)")
    $displayOrder = @($PrimaryMainModel, $FallbackMainModel, $SubagentModel)
    $sortedMonitored = @()
    foreach ($ph in $displayOrder) {
        $match = $monitoredList | Where-Object { $_.placeholder -eq $ph } | Select-Object -First 1
        if ($null -ne $match) { $sortedMonitored += $match }
    }
    foreach ($m in $monitoredList) {
        if ($displayOrder -notcontains $m.placeholder) { $sortedMonitored += $m }
    }

    foreach ($m in $sortedMonitored) {
        $dispFrac = [double]$m.remainingFraction
        if ($SimulateArgonExhausted -and $m.placeholder -eq $PrimaryMainModel) { $dispFrac = $effArgonFraction }
        if ($SimulateLowQuota -and $m.placeholder -eq $PrimaryMainModel) { $dispFrac = $effArgonFraction }
        if ($SimulateLowQuota -and $m.placeholder -eq $SubagentModel) { $dispFrac = $effFlashFraction }

        $dispPct = ($dispFrac * 100).ToString("0.0", [System.Globalization.CultureInfo]::InvariantCulture) + "%"
        $bar = Get-QuotaBar -Fraction $dispFrac -Width 16
        $ttl = Format-DurationShort -TotalSeconds $m.secondsUntilReset

        $effectiveActivePh = if ($operationalMode -eq "FALLBACK_FLASH") { $FallbackMainModel } else { $activePlaceholder }
        $roleTag = if ($m.placeholder -eq $effectiveActivePh) {
            if ($operationalMode -eq "FALLBACK_FLASH") { "[MUDUR / FAILOVER]" } else { "[MUDUR / ACTIVE]" }
        } elseif ($m.placeholder -eq $PrimaryMainModel) {
            if ($dispFrac -lt $Threshold) { "[MUDUR / DEPLETED]" } else { "[MUDUR / PRIMARY]" }
        } elseif ($m.placeholder -eq $FallbackMainModel) {
            "[MUDUR / STANDBY]"
        } elseif ($m.placeholder -eq $SubagentModel) {
            "[SUBAGENTS / 5x]"
        } else {
            "[MONITORED]"
        }

        $lineColor = if ($dispFrac -lt $Threshold) { "Red" } elseif ($dispFrac -lt 0.25) { "Yellow" } else { "Gray" }
        $formattedLine = ("  |-- {0,-18} {1,-28} {2} {3,6} | Reset in {4} ({5})" -f $roleTag, $m.label, $bar, $dispPct, $ttl, $m.resetTimeLocal.Substring(11, 8))
        Write-Host $formattedLine -ForegroundColor $lineColor
        $logLines += $formattedLine
    }

    try {
        Add-Content -Path $WatchdogLogPath -Value ($logLines -join [Environment]::NewLine) -Encoding UTF8 -ErrorAction SilentlyContinue
    } catch {}

    return $statusPayload
}

if ($CheckOnce) {
    $null = Invoke-QuotaCheckIteration -TickNumber 1
    exit 0
}

[System.IO.File]::WriteAllText($WatchdogPidPath, $PID.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "================================================================================================" -ForegroundColor DarkCyan
Write-Host "  ANTIGRAVITY MULTI-AGENT QUOTA & FAILOVER WATCHDOG [PROTOCOL: KOD 5682]" -ForegroundColor Cyan
Write-Host "  PID: $PID | Poll Interval: ${IntervalSeconds}s | Threshold: $($Threshold*100)% | Session: $($ConversationId.Substring(0,8))..." -ForegroundColor DarkGray
Write-Host "  Architecture: Mudur (Argon -> Flash High Failover) + 4x Subagents (Flash Medium) + Safe Pause" -ForegroundColor DarkGray
Write-Host "================================================================================================" -ForegroundColor DarkCyan

$tick = 1
while ($true) {
    try {
        $null = Invoke-QuotaCheckIteration -TickNumber $tick
    } catch {
        Write-Warning "[QuotaWatchdog] Tick #$tick error: $($_.Exception.Message)"
    }
    if ($MaxTicks -gt 0 -and $tick -ge $MaxTicks) {
        break
    }
    $tick++
    Start-Sleep -Seconds $IntervalSeconds
}

