# Process missing-batch-2 devices
$ErrorActionPreference = "Stop"

# Load device list
$batch = Get-Content "StationpediaPlus\coordination\outputs\missing-batch-2.json" | ConvertFrom-Json
$deviceKeys = $batch | ForEach-Object { $_.value }

# Load Stationpedia
$stationpediaJson = Get-Content "Stationeers\Stationpedia\Stationpedia.json" -Raw | ConvertFrom-Json

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
    "Open" = @{
        "dataType" = "Boolean"
        "range" = "0-1"
        "description" = "Device open/closed state."
    }
    "Pressure" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Current internal pressure in kPa."
    }
    "Temperature" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Current internal temperature in Kelvin."
    }
    "TotalMoles" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Total moles of gas in the internal atmosphere."
    }
    "RatioOxygen" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Oxygen in the internal atmosphere."
    }
    "RatioCarbonDioxide" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Carbon Dioxide in the internal atmosphere."
    }
    "RatioNitrogen" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Nitrogen in the internal atmosphere."
    }
    "RatioPollutant" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Pollutant in the internal atmosphere."
    }
    "RatioVolatiles" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Volatiles in the internal atmosphere."
    }
    "RatioWater" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Water (steam) in the internal atmosphere."
    }
    "RatioNitrousOxide" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Nitrous Oxide in the internal atmosphere."
    }
    "RatioHydrogen" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Hydrogen in the internal atmosphere."
    }
    "RatioSteam" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Steam in the internal atmosphere."
    }
    "RatioLiquidNitrogen" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Liquid Nitrogen in the internal atmosphere."
    }
    "RatioLiquidOxygen" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Liquid Oxygen in the internal atmosphere."
    }
    "RatioLiquidVolatiles" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Liquid Volatiles in the internal atmosphere."
    }
    "RatioLiquidCarbonDioxide" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Liquid Carbon Dioxide in the internal atmosphere."
    }
    "RatioLiquidPollutant" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Liquid Pollutant in the internal atmosphere."
    }
    "RatioLiquidNitrousOxide" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Liquid Nitrous Oxide in the internal atmosphere."
    }
    "RatioLiquidHydrogen" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Liquid Hydrogen in the internal atmosphere."
    }
    "RatioPollutedWater" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Polluted Water in the internal atmosphere."
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
    "RequiredPower" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Power required by the device in Watts."
    }
    "Maximum" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Maximum capacity or pressure setting."
    }
    "Combustion" = @{
        "dataType" = "Boolean"
        "range" = "0-1"
        "description" = "Returns 1 if combustion is occurring."
    }
}

# Output array
$output = @()

# Process each device
foreach ($deviceKey in $deviceKeys) {
    Write-Host "Processing: $deviceKey"
    
    # Find device in Stationpedia
    $device = $stationpediaJson.pages | Where-Object { $_.Key -eq $deviceKey }
    
    if (-not $device) {
        Write-Warning "Device not found in Stationpedia: $deviceKey"
        continue
    }
    
    $deviceEntry = @{
        "deviceKey" = $deviceKey
        "displayName" = $device.Title
        "logicDescriptions" = @{}
    }
    
    # Process each logic type
    foreach ($logic in $device.LogicInsert) {
        # Extract logic name from XML-like string
        $logicName = if ($logic.LogicName -match '>([^<]+)</color>') {
            $matches[1]
        } else {
            continue
        }
        
        # Check if it's a standard logic type
        if ($standardLogic.ContainsKey($logicName)) {
            $deviceEntry.logicDescriptions[$logicName] = $standardLogic[$logicName]
        } else {
            # Device-specific logic - need to investigate
            Write-Host "  Custom logic: $logicName (needs source code review)"
            
            # For now, add placeholder
            $deviceEntry.logicDescriptions[$logicName] = @{
                "dataType" = "Unknown"
                "range" = "Unknown"
                "description" = "TODO: Review source code for $deviceKey"
            }
        }
    }
    
    $output += $deviceEntry
}

# Save output
$output | ConvertTo-Json -Depth 10 | Set-Content "StationpediaPlus\coordination\outputs\missing-batch-2-complete.json"

Write-Host "`nProcessed $($output.Count) devices"
Write-Host "Output saved to: StationpediaPlus\coordination\outputs\missing-batch-2-complete.json"
