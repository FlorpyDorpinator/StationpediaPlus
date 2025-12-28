$stationpedia = Get-Content "C:\Dev\12-17-25 Stationeers Respawn Update Code\Stationeers\Stationpedia\Stationpedia.json" -Raw | ConvertFrom-Json
$descriptions = Get-Content "C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\mod\descriptions.json" -Raw | ConvertFrom-Json
$op = "C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\coordination\outputs"

$allMissing = @()

foreach ($device in $descriptions.devices) {
    $spPage = $stationpedia.pages | Where-Object { $_.Key -eq $device.deviceKey }
    if ($spPage -and $spPage.LogicInsert) {
        $spLogicTypes = $spPage.LogicInsert | ForEach-Object { $_.LogicName -replace 'LogicType', '' }
        $docLogicTypes = if ($device.logicDescriptions) { $device.logicDescriptions.PSObject.Properties.Name } else { @() }
        $missing = $spLogicTypes | Where-Object { $_ -notin $docLogicTypes }
        if ($missing.Count -gt 0) {
            $allMissing += [PSCustomObject]@{
                deviceKey = $device.deviceKey
                missingTypes = ($missing -join ", ")
                missingCount = $missing.Count
            }
        }
    }
}

Write-Host "Devices with missing logic types: $($allMissing.Count)"
Write-Host "Total missing entries: $(($allMissing | Measure-Object -Property missingCount -Sum).Sum)"
Write-Host ""
Write-Host "Top 10 devices with most missing:"
$allMissing | Sort-Object -Property missingCount -Descending | Select-Object -First 10 | ForEach-Object {
    Write-Host "  $($_.deviceKey): $($_.missingCount) missing - $($_.missingTypes)"
}

$allMissing | ConvertTo-Json | Out-File (Join-Path $op "missing-logic-types.json") -Encoding UTF8
Write-Host ""
Write-Host "Full list saved to: missing-logic-types.json"
