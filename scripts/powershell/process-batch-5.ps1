# Process Missing Batch 5 - Document logic descriptions
$ErrorActionPreference = "Continue"

# Load data
Write-Host "Loading data files..." -ForegroundColor Cyan
$stationpediaData = Get-Content "Stationeers\Stationpedia\Stationpedia.json" -Raw | ConvertFrom-Json
$stationpedia = $stationpediaData.pages
$batch5 = Get-Content 'StationpediaPlus\coordination\outputs\missing-batch-5.json' | ConvertFrom-Json
$devices = $batch5 | ForEach-Object { $_.value }

Write-Host "Stationpedia pages loaded: $($stationpedia.Count)" -ForegroundColor Cyan
Write-Host "Processing $($devices.Count) devices from Batch 5..." -ForegroundColor Cyan

# Result array
$results = @()

# Standard logic type descriptions
$standardDescriptions = @{
    "Power" = @{ dataType = "Boolean"; range = "0-1"; description = "Returns 1 if device is powered." }
    "On" = @{ dataType = "Boolean"; range = "0-1"; description = "Device on/off state." }
    "Lock" = @{ dataType = "Boolean"; range = "0-1"; description = "Locks device when set to 1." }
    "Battery" = @{ dataType = "Float"; range = "0+"; description = "Current battery charge level." }
    "Charge" = @{ dataType = "Float"; range = "0-100"; description = "Battery charge percentage." }
    "PrefabHash" = @{ dataType = "Integer"; range = "Any"; description = "Unique hash identifier for the device prefab." }
    "ReferenceId" = @{ dataType = "Integer"; range = "Any"; description = "Unique reference ID for this specific device instance." }
    "NameHash" = @{ dataType = "Integer"; range = "Any"; description = "Hash of the device's custom name if set." }
    "Pressure" = @{ dataType = "Float"; range = "0+ kPa"; description = "Current pressure reading." }
    "Temperature" = @{ dataType = "Float"; range = "0+ K"; description = "Current temperature reading." }
    "TotalMoles" = @{ dataType = "Float"; range = "0+"; description = "Total moles of gas." }
    "RatioOxygen" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of oxygen in gas mixture." }
    "RatioCarbonDioxide" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of carbon dioxide in gas mixture." }
    "RatioNitrogen" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of nitrogen in gas mixture." }
    "RatioPollutant" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of pollutants in gas mixture." }
    "RatioVolatiles" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of volatiles in gas mixture." }
    "RatioNitrousOxide" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of nitrous oxide in gas mixture." }
    "RatioHydrogen" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of hydrogen in gas mixture." }
    "RatioWater" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of water vapor in gas mixture." }
    "RatioLiquidNitrogen" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of liquid nitrogen." }
    "RatioLiquidOxygen" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of liquid oxygen." }
    "RatioLiquidVolatiles" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of liquid volatiles." }
    "RatioSteam" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of steam." }
    "RatioLiquidCarbonDioxide" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of liquid carbon dioxide." }
    "RatioLiquidPollutant" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of liquid pollutant." }
    "RatioLiquidNitrousOxide" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of liquid nitrous oxide." }
    "RatioLiquidHydrogen" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of liquid hydrogen." }
    "RatioPollutedWater" = @{ dataType = "Float"; range = "0-1"; description = "Ratio of polluted water." }
    "Quantity" = @{ dataType = "Float"; range = "0+"; description = "Current quantity or amount." }
    "Open" = @{ dataType = "Boolean"; range = "0-1"; description = "Device open/closed state." }
    "Activate" = @{ dataType = "Boolean"; range = "0-1"; description = "Trigger to activate device." }
    "Volume" = @{ dataType = "Float"; range = "0+"; description = "Current volume in liters." }
    "Combustion" = @{ dataType = "Boolean"; range = "0-1"; description = "Returns 1 if actively combusting." }
    "Flush" = @{ dataType = "Boolean"; range = "0-1"; description = "Trigger to flush contents." }
    "SoundAlert" = @{ dataType = "Boolean"; range = "0-1"; description = "Returns 1 if sound alert is active." }
    "PressureExternal" = @{ dataType = "Float"; range = "0+ kPa"; description = "External pressure reading." }
    "TemperatureSetting" = @{ dataType = "Float"; range = "0+ K"; description = "Target temperature setting." }
    "TemperatureExternal" = @{ dataType = "Float"; range = "0+ K"; description = "External temperature reading." }
    "PressureSetting" = @{ dataType = "Float"; range = "0+ kPa"; description = "Target pressure setting." }
    "Filtration" = @{ dataType = "Boolean"; range = "0-1"; description = "Filtration system on/off state." }
    "AirRelease" = @{ dataType = "Boolean"; range = "0-1"; description = "Air release valve state." }
    "PositionX" = @{ dataType = "Float"; range = "Any"; description = "X coordinate position." }
    "PositionY" = @{ dataType = "Float"; range = "Any"; description = "Y coordinate position." }
    "PositionZ" = @{ dataType = "Float"; range = "Any"; description = "Z coordinate position." }
    "VelocityMagnitude" = @{ dataType = "Float"; range = "0+"; description = "Magnitude of velocity vector." }
    "VelocityRelativeX" = @{ dataType = "Float"; range = "Any"; description = "Relative X velocity component." }
    "VelocityRelativeY" = @{ dataType = "Float"; range = "Any"; description = "Relative Y velocity component." }
    "VelocityRelativeZ" = @{ dataType = "Float"; range = "Any"; description = "Relative Z velocity component." }
    "VelocityX" = @{ dataType = "Float"; range = "Any"; description = "X velocity component." }
    "VelocityY" = @{ dataType = "Float"; range = "Any"; description = "Y velocity component." }
    "VelocityZ" = @{ dataType = "Float"; range = "Any"; description = "Z velocity component." }
    "ForwardX" = @{ dataType = "Float"; range = "-1 to 1"; description = "Forward vector X component." }
    "ForwardY" = @{ dataType = "Float"; range = "-1 to 1"; description = "Forward vector Y component." }
    "ForwardZ" = @{ dataType = "Float"; range = "-1 to 1"; description = "Forward vector Z component." }
    "Orientation" = @{ dataType = "Float"; range = "0-360"; description = "Orientation angle in degrees." }
    "EntityState" = @{ dataType = "Integer"; range = "0+"; description = "Current entity state code." }
    "RequiredPower" = @{ dataType = "Float"; range = "0+ W"; description = "Required power consumption in watts." }
    "Throttle" = @{ dataType = "Float"; range = "0-100"; description = "Throttle percentage." }
    "PassedMoles" = @{ dataType = "Float"; range = "0+"; description = "Total moles passed through." }
    "Maximum" = @{ dataType = "Float"; range = "0+"; description = "Maximum capacity or value." }
    "Ratio" = @{ dataType = "Float"; range = "0-1"; description = "Generic ratio value." }
    "Vertical" = @{ dataType = "Float"; range = "Any"; description = "Vertical position or angle." }
    "ContactTypeId" = @{ dataType = "Integer"; range = "Any"; description = "Type ID of contacted entity." }
    "LineNumber" = @{ dataType = "Integer"; range = "0+"; description = "Current line number for programmable devices." }
    "StackSize" = @{ dataType = "Integer"; range = "0+"; description = "Size of stack in programmable device." }
    "Idle" = @{ dataType = "Boolean"; range = "0-1"; description = "Returns 1 if device is idle." }
    "Extended" = @{ dataType = "Boolean"; range = "0-1"; description = "Returns 1 if device is extended." }
    "Color" = @{ dataType = "Integer"; range = "0+"; description = "Current color value or code." }
}

foreach ($deviceKey in $devices) {
    Write-Host "`nProcessing $deviceKey..." -ForegroundColor Yellow
    
    # Find device in Stationpedia
    $deviceEntry = $stationpedia | Where-Object { $_.Key -eq $deviceKey }
    
    if (-not $deviceEntry) {
        Write-Host "  WARNING: Device not found in Stationpedia.json" -ForegroundColor Red
        continue
    }
    
    $displayName = $deviceEntry.DisplayName
    Write-Host "  Display Name: $displayName"
    
    # Get logic insert array
    $logicInsert = $deviceEntry.LogicInsert
    
    if (-not $logicInsert -or $logicInsert.Count -eq 0) {
        Write-Host "  No logic types found" -ForegroundColor Gray
        $results += @{
            deviceKey = $deviceKey
            displayName = $displayName
            logicDescriptions = @{}
        }
        continue
    }
    
    Write-Host "  Logic types: $($logicInsert.Count)"
    
    # Build logic descriptions
    $logicDescriptions = @{}
    
    foreach ($logic in $logicInsert) {
        # Extract logic name from formatted string
        $logicName = $logic.LogicName
        if ($logicName -match '>([^<]+)</color>') {
            $logicName = $matches[1]
        }
        
        Write-Host "    - $logicName" -ForegroundColor Gray
        
        # Check if it's a standard type
        if ($standardDescriptions.ContainsKey($logicName)) {
            $logicDescriptions[$logicName] = $standardDescriptions[$logicName]
        }
        else {
            # Device-specific logic - add generic descriptions
            $description = ""
            $dataType = "Unknown"
            $range = "Unknown"
            
            switch ($logicName) {
                "Mode" {
                    $dataType = "Integer"
                    $range = "0+"
                    $description = "Operating mode. Check device source code for mode values."
                }
                "Setting" {
                    $dataType = "Float"
                    $range = "Any"
                    $description = "Device-specific setting value. Check source code for usage."
                }
                "Error" {
                    $dataType = "Integer"
                    $range = "0+"
                    $description = "Error state code. 0=No error. Check source code for error codes."
                }
                default {
                    Write-Host "      [Device-specific - needs verification]" -ForegroundColor DarkYellow
                    $dataType = "Unknown"
                    $range = "Unknown"
                    $description = "Device-specific logic type - requires source code verification."
                }
            }
            
            $logicDescriptions[$logicName] = @{
                dataType = $dataType
                range = $range
                description = $description
            }
        }
    }
    
    $results += @{
        deviceKey = $deviceKey
        displayName = $displayName
        logicDescriptions = $logicDescriptions
    }
}

# Save to JSON
Write-Host "`nSaving results to missing-batch-5-complete.json..." -ForegroundColor Cyan
$results | ConvertTo-Json -Depth 10 | Set-Content 'StationpediaPlus\coordination\outputs\missing-batch-5-complete.json'

Write-Host "Done! Processed $($results.Count) devices." -ForegroundColor Green
