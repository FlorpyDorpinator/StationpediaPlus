# merge-review-batches.ps1
# Merges all 16 review batch outputs into a single descriptions.json

$outputDir = "C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\coordination\outputs"
$modDir = "C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\mod"

$allDevices = @()

for ($i = 1; $i -le 16; $i++) {
    $file = Join-Path $outputDir "review-batch-$i-complete.json"
    Write-Host "Processing batch $i..."
    
    $content = Get-Content $file -Raw
    $json = $content | ConvertFrom-Json
    
    # Handle both formats: flat array or wrapper object with devices array
    if ($json -is [array]) {
        $devices = $json
    } elseif ($json.devices) {
        $devices = $json.devices
    } else {
        Write-Host "  WARNING: Unknown format in batch $i"
        continue
    }
    
    Write-Host "  Found $($devices.Count) devices"
    
    foreach ($device in $devices) {
        $allDevices += $device
    }
}

Write-Host "`nTotal devices collected: $($allDevices.Count)"

# Build the final structure
$finalJson = @{
    "schema" = "2.0.0"
    "devices" = $allDevices
    "genericDescriptions" = @{
        "logic" = @{
            "Power" = @{ "dataType" = "Boolean"; "range" = "0-1"; "description" = "Returns 1 if device is receiving power." }
            "On" = @{ "dataType" = "Boolean"; "range" = "0-1"; "description" = "Device on/off state. Can be set via logic." }
            "Lock" = @{ "dataType" = "Boolean"; "range" = "0-1"; "description" = "Locks device interaction when set to 1." }
            "Error" = @{ "dataType" = "Boolean"; "range" = "0-1"; "description" = "Returns 1 if device has an error state." }
            "Activate" = @{ "dataType" = "Boolean"; "range" = "0-1"; "description" = "Triggers device activation." }
            "Open" = @{ "dataType" = "Boolean"; "range" = "0-1"; "description" = "Opens (1) or closes (0) the device." }
            "Pressure" = @{ "dataType" = "Float"; "range" = "0+ kPa"; "description" = "Internal pressure in kilopascals." }
            "Temperature" = @{ "dataType" = "Float"; "range" = "0+ K"; "description" = "Internal temperature in Kelvin." }
            "TotalMoles" = @{ "dataType" = "Float"; "range" = "0+"; "description" = "Total moles of gas in internal atmosphere." }
            "RatioOxygen" = @{ "dataType" = "Float"; "range" = "0-1"; "description" = "Ratio of oxygen in internal atmosphere." }
            "RatioNitrogen" = @{ "dataType" = "Float"; "range" = "0-1"; "description" = "Ratio of nitrogen in internal atmosphere." }
            "RatioCarbonDioxide" = @{ "dataType" = "Float"; "range" = "0-1"; "description" = "Ratio of CO2 in internal atmosphere." }
            "RatioVolatiles" = @{ "dataType" = "Float"; "range" = "0-1"; "description" = "Ratio of volatiles in internal atmosphere." }
            "RatioPollutant" = @{ "dataType" = "Float"; "range" = "0-1"; "description" = "Ratio of pollutants (X) in internal atmosphere." }
            "RatioNitrousOxide" = @{ "dataType" = "Float"; "range" = "0-1"; "description" = "Ratio of nitrous oxide (N2O) in internal atmosphere." }
            "RatioWater" = @{ "dataType" = "Float"; "range" = "0-1"; "description" = "Ratio of water vapor (steam) in internal atmosphere." }
            "Quantity" = @{ "dataType" = "Integer"; "range" = "0+"; "description" = "Number of items in device or stack quantity." }
            "RequiredPower" = @{ "dataType" = "Float"; "range" = "0+ W"; "description" = "Power required by device in watts." }
            "PrefabHash" = @{ "dataType" = "Integer"; "range" = "varies"; "description" = "Hash identifying the prefab type." }
            "ReferenceId" = @{ "dataType" = "Integer"; "range" = "varies"; "description" = "Unique reference ID for this device instance." }
        }
        "slots" = @{
            "Occupied" = @{ "dataType" = "Boolean"; "range" = "0-1"; "description" = "Returns 1 if slot contains an item." }
            "OccupantHash" = @{ "dataType" = "Integer"; "range" = "varies"; "description" = "PrefabHash of item in slot, or 0 if empty." }
            "Quantity" = @{ "dataType" = "Integer"; "range" = "0+"; "description" = "Stack quantity of item in slot." }
            "Damage" = @{ "dataType" = "Float"; "range" = "0-1"; "description" = "Damage level of item in slot (0=pristine, 1=destroyed)." }
            "Charge" = @{ "dataType" = "Float"; "range" = "0+"; "description" = "Charge level of battery/cell in slot." }
            "ChargeRatio" = @{ "dataType" = "Float"; "range" = "0-1"; "description" = "Charge as ratio of maximum capacity." }
            "Class" = @{ "dataType" = "Integer"; "range" = "varies"; "description" = "Class type hash of item in slot." }
            "MaxQuantity" = @{ "dataType" = "Integer"; "range" = "0+"; "description" = "Maximum stack size for slot." }
            "PrefabHash" = @{ "dataType" = "Integer"; "range" = "varies"; "description" = "Same as OccupantHash - prefab hash of item." }
            "SortingClass" = @{ "dataType" = "Integer"; "range" = "varies"; "description" = "Sorting class for the item in slot." }
            "ReferenceId" = @{ "dataType" = "Integer"; "range" = "varies"; "description" = "Reference ID of item in slot." }
        }
    }
}

# Convert to JSON with proper formatting
$jsonOutput = $finalJson | ConvertTo-Json -Depth 10

# Save to mod folder
$outputFile = Join-Path $modDir "descriptions.json"
$jsonOutput | Out-File $outputFile -Encoding UTF8

Write-Host "`nSaved to: $outputFile"
Write-Host "File size: $((Get-Item $outputFile).Length / 1KB) KB"
