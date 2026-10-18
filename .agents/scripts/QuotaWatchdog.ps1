param(
    [string]$ConversationId = "383ce584-3971-4efe-bd1d-24f2b857dc0a",
    [double]$Threshold = 0.10,
    [int]$IntervalSeconds = 10,
    [switch]$CheckOnce,
    [switch]$SimulateLowQuota
)

$ErrorActionPreference = "Stop"

$WorkspaceRoot = "c:\Users\meteh\RiderProjects\QuestlinerFramework"
$AgentsDir = Join-Path $WorkspaceRoot ".agents"
$PromptsDir = Join-Path $AgentsDir "prompts"
$PromptTemplatePath = Join-Path $PromptsDir "KOD_5682_KOTA_PROMPT.md"
$LastTriggeredPromptPath = Join-Path $PromptsDir "KOD_5682_LAST_TRIGGERED.md"
$LiveStatusPath = Join-Path $AgentsDir "quota_live_status.json"
$AlertStatePath = Join-Path $AgentsDir "quota_alert_state.json"
$WatchdogPidPath = Join-Path $AgentsDir "quota_watchdog.pid"
$StatePbtxtPath = "C:\Users\meteh\.gemini\antigravity\antigravity_state.pbtxt"
$AgentApiBat = "C:\Users\meteh\.gemini\antigravity\bin\agentapi.bat"

if (-not (Test-Path $PromptsDir)) {
    New-Item -ItemType Directory -Path $PromptsDir -Force | Out-Null
}

function Get-ActiveMainModelPlaceholder {
    if (Test-Path $StatePbtxtPath) {
        $content = Get-Content $StatePbtxtPath -Raw -ErrorAction SilentlyContinue
        if ($content -match 'last_selected_agent_model:\s*(\S+)') {
            return $matches[1].Trim()
        }
    }
    return "MODEL_PLACEHOLDER_M417"
}

function Get-AntigravityQuotaStatus {
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
                        return $resp.userStatus.cascadeModelConfigData.clientModelConfigs
                    }
                } catch {
                    # Continue checking other ports
                }
            }
        }
    }
    return $null
}

function Format-Kod5682Prompt {
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

    # Exact seconds until reset + 60s safety buffer (minimum 120s)
    $rawSeconds = [int][Math]::Ceiling(($resetUtc - $nowUtc).TotalSeconds) + 60
    $waitSeconds = [Math]::Max(120, $rawSeconds)
    $waitMinutes = [Math]::Round($waitSeconds / 60.0, 1).ToString("0.0", [System.Globalization.CultureInfo]::InvariantCulture)

    $template = Get-Content -Path $PromptTemplatePath -Raw -Encoding UTF8
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

function Invoke-QuotaCheckIteration {
    $configs = Get-AntigravityQuotaStatus
    if ($null -eq $configs) {
        Write-Warning "[QuotaWatchdog] Could not reach local Antigravity language_server GetUserStatus endpoint."
        return $null
    }

    $activePlaceholder = Get-ActiveMainModelPlaceholder
    # Always monitor:
    # 1. Currently selected main model in IDE (e.g. MODEL_PLACEHOLDER_M417 = Gemini 4 Argon High)
    # 2. Gemini 4 Argon (High) explicitly (MODEL_PLACEHOLDER_M417)
    # 3. Gemini 3.8 Flash (Medium) explicitly (MODEL_PLACEHOLDER_M319 - subagent model)
    $targetPlaceholders = @($activePlaceholder, "MODEL_PLACEHOLDER_M417", "MODEL_PLACEHOLDER_M319") | Select-Object -Unique

    $nowUtc = (Get-Date).ToUniversalTime()
    $monitoredList = @()
    $allSummaryList = @()

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
        if ($targetPlaceholders -contains $placeholder) {
            $monitoredList += $entry
        }
    }

    $statusPayload = [ordered]@{
        checkedAtUtc             = $nowUtc.ToString("o")
        checkedAtLocal           = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss zzz")
        watchdogPid              = $PID
        conversationId           = $ConversationId
        thresholdFraction        = $Threshold
        thresholdPercent         = [Math]::Round($Threshold * 100, 2)
        activeMainPlaceholder    = $activePlaceholder
        monitoredModels          = $monitoredList
        allModels                = $allSummaryList
    }

    $statusJson = $statusPayload | ConvertTo-Json -Depth 6
    [System.IO.File]::WriteAllText($LiveStatusPath, $statusJson, [System.Text.UTF8Encoding]::new($false))

    # Check if any monitored model is below Threshold (or SimulateLowQuota is set)
    foreach ($m in $monitoredList) {
        $effectiveFraction = $m.remainingFraction
        if ($SimulateLowQuota -and $m.isActiveMain) {
            $effectiveFraction = 0.085
        }

        if ($effectiveFraction -lt $Threshold) {
            $alertKey = "$($m.modelId)|$($m.resetTimeUtc)"
            $alreadyTriggered = $false

            if ((-not $SimulateLowQuota) -and (Test-Path $AlertStatePath)) {
                try {
                    $prevAlert = Get-Content $AlertStatePath -Raw | ConvertFrom-Json
                    if ($prevAlert.alertKey -eq $alertKey -and $prevAlert.conversationId -eq $ConversationId) {
                        $alreadyTriggered = $true
                    }
                } catch {}
            }

            $formatted = Format-Kod5682Prompt `
                -ModelLabel $m.label `
                -ModelId $m.modelId `
                -RemainingFraction $effectiveFraction `
                -ResetTimeUtcStr $m.resetTimeUtc

            [System.IO.File]::WriteAllText($LastTriggeredPromptPath, $formatted.RenderedPrompt, [System.Text.UTF8Encoding]::new($false))

            if (-not $alreadyTriggered) {
                $alertState = [ordered]@{
                    alertKey          = $alertKey
                    triggeredAtUtc    = $nowUtc.ToString("o")
                    triggeredAtLocal  = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss zzz")
                    modelLabel        = $m.label
                    modelId           = $m.modelId
                    remainingPercent  = $formatted.Percent
                    resetTimeUtc      = $m.resetTimeUtc
                    resetTimeLocal    = $formatted.ResetLocal
                    waitSeconds       = $formatted.WaitSeconds
                    conversationId    = $ConversationId
                    simulated         = [bool]$SimulateLowQuota
                }
                [System.IO.File]::WriteAllText($AlertStatePath, ($alertState | ConvertTo-Json -Depth 4), [System.Text.UTF8Encoding]::new($false))

                if (-not $SimulateLowQuota) {
                    Write-Host "[KOD 5682 TRIGGERED] $($m.label) dropped to $($formatted.Percent)% (< $($Threshold*100)%). Sending emergency prompt to $ConversationId..." -ForegroundColor Red
                    & $AgentApiBat send-message --title="ACIL DURUM: KOD 5682 (KOTA)" $ConversationId $formatted.RenderedPrompt
                } else {
                    Write-Host "[KOD 5682 SIMULATION] Rendered emergency prompt for $($m.label) ($($formatted.Percent)%) -> $LastTriggeredPromptPath" -ForegroundColor Yellow
                }
            }
        }
    }

    return $statusPayload
}

if ($CheckOnce) {
    $res = Invoke-QuotaCheckIteration
    if ($null -ne $res) {
        Write-Host "=== KOD 5682 CANLI KOTA DURUMU ($($res.checkedAtLocal)) ===" -ForegroundColor Cyan
        foreach ($m in $res.monitoredModels) {
            $roleTag = if ($m.isActiveMain) { "[MUDUR / AKTIF]" } else { "[ALT AJAN / FLASH]" }
            Write-Host ("{0,-20} {1,-28} Kalan: %{2,-6} | Sifirlanma: {3} ({4} sn)" -f $roleTag, $m.label, $m.remainingPercent, $m.resetTimeLocal, $m.secondsUntilReset) -ForegroundColor Green
        }
    }
    exit 0
}

# Write daemon PID
[System.IO.File]::WriteAllText($WatchdogPidPath, $PID.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "[QuotaWatchdog] Started 10s KOD 5682 Quota Monitor Daemon (PID: $PID, Conversation: $ConversationId, Threshold: $($Threshold*100)%)" -ForegroundColor Cyan

while ($true) {
    try {
        $null = Invoke-QuotaCheckIteration
    } catch {
        Write-Warning "[QuotaWatchdog] Tick error: $($_.Exception.Message)"
    }
    Start-Sleep -Seconds $IntervalSeconds
}
