$missingTypes = Get-Content "C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\coordination\outputs\missing-logic-types.json" -Raw | ConvertFrom-Json
$op = "C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\coordination\outputs"

# Sort by complexity (most complex first for better distribution)
$sorted = $missingTypes | Sort-Object -Property missingCount -Descending

$totalWork = ($sorted | Measure-Object -Property missingCount -Sum).Sum
$numAgents = 16  # Target 16 agents
$targetWorkPerAgent = [math]::Ceiling($totalWork / $numAgents)

Write-Host "Total work: $totalWork missing entries"
Write-Host "Target per agent: ~$targetWorkPerAgent entries"
Write-Host ""

# Distribute using greedy algorithm (assign to agent with least work)
$agentWork = @{}
$agentDevices = @{}
for ($i = 1; $i -le $numAgents; $i++) {
    $agentWork[$i] = 0
    $agentDevices[$i] = @()
}

foreach ($device in $sorted) {
    # Find agent with least work
    $minAgent = 1
    $minWork = $agentWork[1]
    for ($i = 2; $i -le $numAgents; $i++) {
        if ($agentWork[$i] -lt $minWork) {
            $minWork = $agentWork[$i]
            $minAgent = $i
        }
    }
    
    $agentWork[$minAgent] += $device.missingCount
    $agentDevices[$minAgent] += $device.deviceKey
}

# Output batches
Write-Host "=== Batch Distribution ==="
for ($i = 1; $i -le $numAgents; $i++) {
    $count = $agentDevices[$i].Count
    $work = $agentWork[$i]
    Write-Host "Review Batch $i : $count devices, $work missing entries"
    
    # Save batch file
    $batchData = @{
        batchNumber = $i
        deviceCount = $count
        missingEntries = $work
        devices = $agentDevices[$i]
    }
    $batchData | ConvertTo-Json | Out-File (Join-Path $op "review-batch-$i.json") -Encoding UTF8
}

Write-Host ""
Write-Host "Created 16 review batch files in coordination/outputs/"
