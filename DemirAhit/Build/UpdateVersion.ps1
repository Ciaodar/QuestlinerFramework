param (
    [string]$BuildType = "Debug"
)

$scriptDir = $PSScriptRoot
$submodulePath = Join-Path (Split-Path $scriptDir -Parent) "SubModule.xml"

if (-not (Test-Path $submodulePath)) {
    Write-Error "SubModule.xml bulunamadi: $submodulePath"
    exit 1
}

$content = Get-Content $submodulePath -Raw
if ($content -match '<Version value="v(\d+)\.(\d+)\.(\d+)\.(\d+)"\s*/>') {
    $major = [int]$matches[1]
    $update = [int]$matches[2]
    $hotfix = [int]$matches[3]
    $debug = [int]$matches[4]

    if ($BuildType -eq "Update") {
        $update++
        $hotfix = 0
        $debug++
    } elseif ($BuildType -eq "Hotfix") {
        $hotfix++
        $debug++
    } else {
        $debug++
    }

    $newVersion = "v$major.$update.$hotfix.$debug"
    $newContent = $content -replace '<Version value="v\d+\.\d+\.\d+\.\d+"\s*/>', "<Version value=`"$newVersion`" />"
    
    # Save with UTF8 to preserve Bannerlord XML standards
    [System.IO.File]::WriteAllText($submodulePath, $newContent, [System.Text.Encoding]::UTF8)
    Write-Host "Success: Version bumped to $newVersion"
    exit 0
} else {
    Write-Error "SubModule.xml icinde version etiketi bulunamadi veya format hatali! (Beklenen: vMAJOR.UPDATE.HOTFIX.DEBUG)"
    exit 1
}
