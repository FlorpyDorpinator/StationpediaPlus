$problematicTypes = @('Mode', 'Setting', 'Ratio', 'Error', 'Lock', 'Activate', 'Output', 'Output2', 'Input', 'Input2')
$op = "C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\coordination\outputs"

for ($i = 1; $i -le 8; $i++) {
    $file = Join-Path $op "batch-$i-output.json"
    $devices = Get-Content $file -Raw | ConvertFrom-Json
    $reviewList = @()
    
    foreach ($d in $devices) {
        if ($d.logicDescriptions) {
            $props = $d.logicDescriptions.PSObject.Properties.Name
            $needsReview = $props | Where-Object { $_ -in $problematicTypes }
            if ($needsReview) {
                $reviewList += [PSCustomObject]@{
                    deviceKey = $d.deviceKey
                    reviewTypes = ($needsReview -join ", ")
                }
            }
        }
    }
    
    $outFile = Join-Path $op "batch-$i-review-list.json"
    $reviewList | ConvertTo-Json | Out-File $outFile -Encoding UTF8
    Write-Host "Batch $i : Created review list with $($reviewList.Count) devices"
}
