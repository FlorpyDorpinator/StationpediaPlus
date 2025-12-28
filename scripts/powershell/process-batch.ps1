# Process missing batch devices and create complete JSON
param(
    [Parameter(Mandatory=$true)]
    [int]$BatchNumber
)

# Standard logic descriptions
$standardLogic = @{
    "Power" = @{
        "dataType" = "Boolean"
        "range" = "0-1"
        "description" = "Returns 1 if device is powered."
    }
    "On" = @{
        "dataType" = "Boolean"
        "range" = "0-1"
        "description" = "Device on/off state."
    }
    "Lock" = @{
        "dataType" = "Boolean"
        "range" = "0-1"
        "description" = "Locks device when set to 1."
    }
    "RequiredPower" = @{
        "dataType" = "Number"
        "range" = "0+"
        "description" = "Power required to operate device."
    }
    "PrefabHash" = @{
        "dataType" = "Integer"
        "range" = "Any"
        "description" = "Unique hash identifier for the device prefab."
    }
    "ReferenceId" = @{
        "dataType" = "Integer"
        "range" = "Any"
        "description" = "Unique reference ID for this specific device instance."
    }
    "NameHash" = @{
        "dataType" = "Integer"
        "range" = "Any"
        "description" = "Hash of the device's custom name if set."
    }
}

$basePath = "c:\Dev\12-17-25 Stationeers Respawn Update Code"
$deviceListPath = "$basePath\StationpediaPlus\coordination\outputs\batch$BatchNumber-devices.txt"
$outputPath = "$basePath\StationpediaPlus\coordination\outputs\missing-batch-$BatchNumber-complete.json"

Write-Host "Loading Stationpedia..."
$stationpedia = Get-Content "$basePath\Stationeers\Stationpedia\Stationpedia.json" -Raw | ConvertFrom-Json

Write-Host "Loading device list..."
$devices = Get-Content $deviceListPath | Where-Object { $_.Trim() -ne "" }

Write-Host "Processing $($devices.Count) devices..."

$results = @()

foreach ($deviceKey in $devices) {
    Write-Host "  Processing: $deviceKey"
    
    # Find device in stationpedia
    $device = $stationpedia.pages | Where-Object { $_.Key -eq $deviceKey } | Select-Object -First 1
    
    if (-not $device) {
        Write-Warning "    Device not found in Stationpedia: $deviceKey"
        continue
    }
    
    $logicDescriptions = @{}
    
    # Process each logic insert
    foreach ($logicInsert in $device.LogicInsert) {
        # Extract logic name (remove HTML tags and color)
        $logicName = $logicInsert.LogicName -replace '<[^>]+>', ''
        
        if ($standardLogic.ContainsKey($logicName)) {
            $logicDescriptions[$logicName] = $standardLogic[$logicName]
        } else {
            # For non-standard logic, we'd need to search source code
            # For now, add a placeholder
            $logicDescriptions[$logicName] = @{
                "dataType" = "Unknown"
                "range" = "Unknown"
                "description" = "TODO: Verify in source code"
            }
        }
    }
    
    $result = [PSCustomObject]@{
        deviceKey = $deviceKey
        displayName = $device.Title -replace '<[^>]+>', ''
        logicDescriptions = $logicDescriptions
    }
    
    $results += $result
}

Write-Host "Saving results to: $outputPath"
$results | ConvertTo-Json -Depth 10 | Set-Content $outputPath -Encoding UTF8

Write-Host "Complete! Processed $($results.Count) devices."
