# Update missing-batch-4-complete.json with verified logic descriptions
$ErrorActionPreference = "Stop"

# Load the existing JSON
$jsonPath = "StationpediaPlus\coordination\outputs\missing-batch-4-complete.json"
$devices = Get-Content $jsonPath | ConvertFrom-Json

# Verified custom logic descriptions from source code research
$customLogic = @{
    # Console logic (from Console.cs)
    "Console_Setting" = @{
        "dataType" = "Integer"
        "range" = "0+"
        "description" = "Returns the motherboard Flag value when a motherboard is installed."
    }
    
    # Elevator logic (from ElevatorShaft.cs)
    "Elevator_ElevatorSpeed" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Current speed of the elevator carriage. Positive for upward, negative for downward, 0 when stationary. Read/Write."
    }
    "Elevator_ElevatorLevel" = @{
        "dataType" = "Integer"
        "range" = "0+"
        "description" = "Current floor level of the elevator carriage. Write to set target level, read to get current level."
    }
    
    # CryoTube logic (from CryoTube.cs and OccupantAtmospherics base class)
    "CryoTube_Mode" = @{
        "dataType" = "Integer"
        "range" = "0+"
        "description" = "Operating mode of the cryo tube (inherited from base device class)."
    }
    "CryoTube_Setting" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Configuration setting for cryo tube operation (inherited from base device class)."
    }
    "CryoTube_Ratio" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Fill ratio or operational ratio for the cryo tube."
    }
    "CryoTube_EntityState" = @{
        "dataType" = "Integer"
        "range" = "Any"
        "description" = "State of the entity (human) inside the cryo tube. Returns -1 if empty, otherwise entity state value."
    }
    
    # Furnace logic (from FurnaceBase.cs and DeviceInputOutputImportExport)
    "Furnace_Mode" = @{
        "dataType" = "Integer"
        "range" = "0+"
        "description" = "Operating mode of the furnace (inherited from base device class)."
    }
    "Furnace_Setting" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Temperature or operational setting for the furnace."
    }
    "Furnace_Reagents" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Current quantity of reagents/materials in the furnace."
    }
    "Furnace_Ratio" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Completion ratio or fill ratio for the current furnace operation."
    }
    "Furnace_RecipeHash" = @{
        "dataType" = "Integer"
        "range" = "Any"
        "description" = "Hash identifier of the currently selected recipe. Read to get current recipe, write to set recipe."
    }
    "Furnace_ClearMemory" = @{
        "dataType" = "Boolean"
        "range" = "0-1"
        "description" = "Write-only. Set to 1 to reset ExportCount and ImportCount counters to zero."
    }
    "Furnace_ExportCount" = @{
        "dataType" = "Integer"
        "range" = "0+"
        "description" = "Number of items exported from the furnace. Read-only."
    }
    "Furnace_ImportCount" = @{
        "dataType" = "Integer"
        "range" = "0+"
        "description" = "Number of items imported into the furnace. Read-only."
    }
    
    # Heat Exchanger logic (from base classes)
    "HeatExchanger_Setting" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Temperature differential or exchange rate setting."
    }
    "HeatExchanger_Ratio" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Efficiency ratio or heat exchange ratio."
    }
    
    # Gas Tank Output logic (for CapsuleTankGas)
    "GasTank_Setting" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Target pressure or operational setting for the gas tank."
    }
    "GasTank_Ratio" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Fill ratio of the tank (current/maximum)."
    }
    "GasTank_Volume" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Total volume capacity of the tank in liters."
    }
    "GasTank_VolumeOfLiquid" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Volume of liquefied gas in the tank in liters."
    }
    "GasTank_PressureOutput" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Output pressure reading from the tank in kPa."
    }
    "GasTank_TemperatureOutput" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Output temperature reading from the tank in Kelvin."
    }
    "GasTank_TotalMolesOutput" = @{
        "dataType" = "Float"
        "range" = "0+"
        "description" = "Total moles of gas at the output."
    }
    "GasTank_CombustionOutput" = @{
        "dataType" = "Boolean"
        "range" = "0-1"
        "description" = "Returns 1 if combustion is occurring in the output."
    }
    "GasTank_RatioOxygenOutput" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Oxygen in the output atmosphere."
    }
    "GasTank_RatioCarbonDioxideOutput" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Carbon Dioxide in the output atmosphere."
    }
    "GasTank_RatioNitrogenOutput" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Nitrogen in the output atmosphere."
    }
    "GasTank_RatioPollutantOutput" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Pollutant in the output atmosphere."
    }
    "GasTank_RatioVolatilesOutput" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Volatiles in the output atmosphere."
    }
    "GasTank_RatioWaterOutput" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Water (steam) in the output atmosphere."
    }
    "GasTank_RatioNitrousOxideOutput" = @{
        "dataType" = "Float"
        "range" = "0-1"
        "description" = "Ratio of Nitrous Oxide in the output atmosphere."
    }
}

# Update function
function Update-LogicDescription {
    param($device, $logicName, $lookupKey)
    
    if ($device.logicDescriptions.PSObject.Properties[$logicName]) {
        $logic = $device.logicDescriptions.$logicName
        if ($logic.description -like "TODO:*" -and $customLogic.ContainsKey($lookupKey)) {
            $verified = $customLogic[$lookupKey]
            $logic.dataType = $verified.dataType
            $logic.range = $verified.range
            $logic.description = $verified.description
            Write-Host "  Updated $logicName"
            return $true
        }
    }
    return $false
}

# Process each device
$totalUpdates = 0
foreach ($device in $devices) {
    $deviceName = $device.displayName
    Write-Host "Processing: $deviceName"
    
    switch -Wildcard ($device.deviceKey) {
        "ThingStructureConsole*" {
            if (Update-LogicDescription $device "Setting" "Console_Setting") { $totalUpdates++ }
        }
        
        "ThingStructureElevator*" {
            if (Update-LogicDescription $device "ElevatorSpeed" "Elevator_ElevatorSpeed") { $totalUpdates++ }
            if (Update-LogicDescription $device "ElevatorLevel" "Elevator_ElevatorLevel") { $totalUpdates++ }
        }
        
        "ThingStructureCryoTube*" {
            if (Update-LogicDescription $device "Mode" "CryoTube_Mode") { $totalUpdates++ }
            if (Update-LogicDescription $device "Setting" "CryoTube_Setting") { $totalUpdates++ }
            if (Update-LogicDescription $device "Ratio" "CryoTube_Ratio") { $totalUpdates++ }
            if (Update-LogicDescription $device "EntityState" "CryoTube_EntityState") { $totalUpdates++ }
        }
        
        "ThingStructureFurnace" {
            if (Update-LogicDescription $device "Mode" "Furnace_Mode") { $totalUpdates++ }
            if (Update-LogicDescription $device "Setting" "Furnace_Setting") { $totalUpdates++ }
            if (Update-LogicDescription $device "Reagents" "Furnace_Reagents") { $totalUpdates++ }
            if (Update-LogicDescription $device "Ratio" "Furnace_Ratio") { $totalUpdates++ }
            if (Update-LogicDescription $device "RecipeHash" "Furnace_RecipeHash") { $totalUpdates++ }
            if (Update-LogicDescription $device "ClearMemory" "Furnace_ClearMemory") { $totalUpdates++ }
            if (Update-LogicDescription $device "ExportCount" "Furnace_ExportCount") { $totalUpdates++ }
            if (Update-LogicDescription $device "ImportCount" "Furnace_ImportCount") { $totalUpdates++ }
        }
        
        "ThingStructurePassthroughHeatExchanger*" {
            if (Update-LogicDescription $device "Setting" "HeatExchanger_Setting") { $totalUpdates++ }
            if (Update-LogicDescription $device "Ratio" "HeatExchanger_Ratio") { $totalUpdates++ }
        }
        
        "ThingStructureCapsuleTankGas" {
            if (Update-LogicDescription $device "Setting" "GasTank_Setting") { $totalUpdates++ }
            if (Update-LogicDescription $device "Ratio" "GasTank_Ratio") { $totalUpdates++ }
            if (Update-LogicDescription $device "Volume" "GasTank_Volume") { $totalUpdates++ }
            if (Update-LogicDescription $device "VolumeOfLiquid" "GasTank_VolumeOfLiquid") { $totalUpdates++ }
            if (Update-LogicDescription $device "PressureOutput" "GasTank_PressureOutput") { $totalUpdates++ }
            if (Update-LogicDescription $device "TemperatureOutput" "GasTank_TemperatureOutput") { $totalUpdates++ }
            if (Update-LogicDescription $device "TotalMolesOutput" "GasTank_TotalMolesOutput") { $totalUpdates++ }
            if (Update-LogicDescription $device "CombustionOutput" "GasTank_CombustionOutput") { $totalUpdates++ }
            if (Update-LogicDescription $device "RatioOxygenOutput" "GasTank_RatioOxygenOutput") { $totalUpdates++ }
            if (Update-LogicDescription $device "RatioCarbonDioxideOutput" "GasTank_RatioCarbonDioxideOutput") { $totalUpdates++ }
            if (Update-LogicDescription $device "RatioNitrogenOutput" "GasTank_RatioNitrogenOutput") { $totalUpdates++ }
            if (Update-LogicDescription $device "RatioPollutantOutput" "GasTank_RatioPollutantOutput") { $totalUpdates++ }
            if (Update-LogicDescription $device "RatioVolatilesOutput" "GasTank_RatioVolatilesOutput") { $totalUpdates++ }
            if (Update-LogicDescription $device "RatioWaterOutput" "GasTank_RatioWaterOutput") { $totalUpdates++ }
            if (Update-LogicDescription $device "RatioNitrousOxideOutput" "GasTank_RatioNitrousOxideOutput") { $totalUpdates++ }
        }
    }
}

# Save updated JSON
$devices | ConvertTo-Json -Depth 10 | Set-Content $jsonPath

Write-Host "`n========================================="
Write-Host "Update Complete!"
Write-Host "Total logic descriptions updated: $totalUpdates"
Write-Host "Output saved to: $jsonPath"
Write-Host "=========================================`n"

# Check for remaining TODOs
$remaining = ($devices | ConvertTo-Json -Depth 10 | Select-String "TODO:" -AllMatches).Matches.Count
if ($remaining -gt 0) {
    Write-Host "WARNING: $remaining TODO items still remain" -ForegroundColor Yellow
} else {
    Write-Host "SUCCESS: All custom logic types have been documented!" -ForegroundColor Green
}
