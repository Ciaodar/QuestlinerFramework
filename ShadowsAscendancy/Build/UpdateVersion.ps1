param (
    [string]$BuildType = "Debug"
)

$xmlPath = Join-Path $PSScriptRoot "..\SubModule.xml"

if (-Not (Test-Path $xmlPath)) {
    Write-Error "SubModule.xml not found at $xmlPath"
    exit 1
}

$content = Get-Content $xmlPath -Raw
if ($content -match '<Version\s+value="v?(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?"\s*/>') {
    $major = [int]$matches[1]
    $update = [int]$matches[2]
    $hotfix = [int]$matches[3]
    if ($matches.Count -ge 5 -and $matches[4] -ne '') {
        $debug = [int]$matches[4]
    } else {
        $debug = 0
    }

    switch ($BuildType.ToLower()) {
        "update" {
            $update += 1
            $hotfix = 0
            $debug += 1
        }
        "hotfix" {
            $hotfix += 1
            $debug += 1
        }
        default {
            $debug += 1
        }
    }

    $newVersion = "v${major}.${update}.${hotfix}.${debug}"
    $newContent = $content -replace '<Version\s+value="[^"]+"\s*/>', "<Version value=`"$newVersion`" />"
    [System.IO.File]::WriteAllText((Resolve-Path $xmlPath).Path, $newContent.TrimEnd() + "`r`n", [System.Text.Encoding]::UTF8)
    Write-Host "Version updated to $newVersion"
    exit 0
} else {
    Write-Error "Could not parse Version tag in SubModule.xml"
    exit 1
}
