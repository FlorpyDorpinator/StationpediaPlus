$missingTypes = Get-Content "C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\coordination\outputs\missing-logic-types.json" -Raw | ConvertFrom-Json

# Calculate complexity scores
$deviceWork = @()
foreach ($d in $missingTypes) {
    $deviceWork += [PSCustomObject]@{
        deviceKey = $d.deviceKey
        missingCount = $d.missingCount
        complexity = if ($d.missingCount -ge 30) { "complex" } elseif ($d.missingCount -ge 10) { "medium" } else { "simple" }
    }
}

$complex = ($deviceWork | Where-Object { $_.complexity -eq "complex" }).Count
$medium = ($deviceWork | Where-Object { $_.complexity -eq "medium" }).Count  
$simple = ($deviceWork | Where-Object { $_.complexity -eq "simple" }).Count

Write-Host "=== Complexity Breakdown ==="
Write-Host "Complex (30+ missing): $complex devices"
Write-Host "Medium (10-29 missing): $medium devices"
Write-Host "Simple (<10 missing): $simple devices"
Write-Host ""
Write-Host "Total missing entries: $(($deviceWork | Measure-Object -Property missingCount -Sum).Sum)"

# Sort by missing count descending
$sorted = $deviceWork | Sort-Object -Property missingCount -Descending

Write-Host ""
Write-Host "=== Top 15 Most Complex ==="
$sorted | Select-Object -First 15 | ForEach-Object { Write-Host "  $($_.deviceKey): $($_.missingCount) missing" }
