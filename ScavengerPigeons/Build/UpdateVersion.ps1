param (
    [string]$BuildType = "Debug"
)

$scriptPath = $MyInvocation.MyCommand.Path
$projectDir = Split-Path (Split-Path $scriptPath)
$submodulePath = Join-Path $projectDir "SubModule.xml"

if (-not (Test-Path $submodulePath)) {
    Write-Error "SubModule.xml not found at $submodulePath"
    exit 1
}

$content = Get-Content $submodulePath -Raw -Encoding UTF8

if ($content -match '<Version\s+value="v(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?"\s*/>') {
    $fullMatch = $matches[0]
    [int]$major = $matches[1]
    [int]$update = $matches[2]
    [int]$hotfix = $matches[3]
    [int]$debug = if ($matches[4]) { $matches[4] } else { 0 }
    
    switch ($BuildType.ToLower()) {
        "hotfix" {
            $hotfix++
            $debug++
        }
        "update" {
            $update++
            $hotfix = 0
            $debug++
        }
        default {
            $debug++
        }
    }
    
    $newVersionStr = "v${major}.${update}.${hotfix}.${debug}"
    $newTag = "<Version value=""$newVersionStr"" />"
    
    $content = $content -replace [regex]::Escape($fullMatch), $newTag
    
    # Write as UTF8 without BOM
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [IO.File]::WriteAllText($submodulePath, $content, $utf8NoBom)
    
    Write-Host "Version updated to $newVersionStr ($BuildType)"
    exit 0
} else {
    Write-Error "Version tag not found or invalid format in SubModule.xml"
    exit 1
}
