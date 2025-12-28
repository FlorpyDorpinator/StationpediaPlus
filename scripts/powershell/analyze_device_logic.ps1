# Load device data
$jsonData = Get-Content "device_logic_data.json" -Raw | ConvertFrom-Json

# Map prefab names to likely source file patterns
$classNameMap = @{
    "Landingpad_ThreshholdPiece" = "LandingPadTaxiThreshold"
    "LaunchSilo" = "LaunchSilo"
    "LogicStepSequencer8" = "LogicStepSequencer"
    "ModularDeviceAlarm" = "ModularDeviceAlarm"
    "ModularDeviceBigLever" = "ModularDeviceBigLever"
    "ModularDeviceCardReader" = "ModularDeviceCardReader"
    "ModularDeviceComputer" = "Computer"
    "ModularDeviceConsole" = "Console"
    "ModularDeviceDial" = "ModularDeviceDial"
    "ModularDeviceDialSmall" = "ModularDeviceDialSmall"
    "ModularDeviceEmergencyButton3x3" = "ModularDeviceEmergencyButton"
    "ModularDeviceFlipCoverSwitch" = "ModularDeviceFlipCoverSwitch"
    "ModularDeviceFlipSwitch" = "ModularDeviceFlipSwitch"
    "PassiveSpeaker" = "PassiveSpeaker"
    "PortableSolarPanel" = "PortableSolarPanel"
    "Robot" = "Robot"
    "Rover_MkI" = "Rover"
    "StopWatch" = "StopWatch"
    "StructureAccessBridge" = "AccessBridge"
    "StructureActiveVent" = "ActiveVent"
    "StructureAdvancedComposter" = "AdvancedComposter"
    "StructureAdvancedFurnace" = "AdvancedFurnace"
    "StructureAdvancedPackagingMachine" = "AdvancedPackagingMachine"
    "StructureAirConditioner" = "AirConditioner"
    "StructureAirlock" = "Airlock"
    "StructureAirlockGate" = "AirlockGate"
    "StructureAirlockWide" = "AirlockWide"
    "StructureAngledBench" = "AngledBench"
    "StructureArcFurnace" = "ArcFurnace"
    "StructureAreaPowerControl" = "AreaPowerControl"
    "StructureAutolathe" = "Autolathe"
    "StructureAutomatedOven" = "AutomatedOven"
    "StructureAutoMinerSmall" = "AutoMiner"
    "StructureBackLiquidPressureRegulator" = "BackLiquidPressureRegulator"
    "StructureBackPressureRegulator" = "BackPressureRegulator"
    "StructureBasketHoop" = "BasketHoop"
    "StructureBattery" = "Battery"
    "StructureBatteryCharger" = "BatteryCharger"
    "StructureBatteryChargerSmall" = "BatteryChargerSmall"
    "StructureBatteryLarge" = "BatteryLarge"
}

Write-Host "Analyzing devices and searching for source files..." -ForegroundColor Cyan

$results = @()

foreach ($device in $jsonData) {
    Write-Host "Processing: $($device.displayName)" -ForegroundColor Yellow
    
    $className = $classNameMap[$device.prefabName]
    if (-not $className) {
        $className = $device.prefabName
    }
    
    # Search for the class file
    $sourceFile = ""
    $searchResults = Get-ChildItem -Path "Assembly-CSharp" -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | 
        Select-String -Pattern "^\s*public class $className\b" -List
    
    if ($searchResults) {
        $sourceFile = ($searchResults | Select-Object -First 1).Path -replace [regex]::Escape($PWD.Path + "\"), ""
    }
    
    # Build findings summary
    $findings = @()
    
    # Analyze logic types
    $logicTypesList = $device.logicTypes
    if ($logicTypesList -is [array]) {
        foreach ($logicType in $logicTypesList) {
            switch ($logicType) {
                "Power" { $findings += "Power: Current power draw" }
                "On" { $findings += "On: Device power state (read/write)" }
                "Open" { $findings += "Open: Door/cover open state" }
                "Lock" { $findings += "Lock: Lock state for doors" }
                "Mode" { $findings += "Mode: Operating mode" }
                "Setting" { $findings += "Setting: Current setting value" }
                "Activate" { $findings += "Activate: Trigger button press" }
                "Error" { $findings += "Error: Error code state" }
                "Ratio" { $findings += "Ratio: Current percentage/ratio" }
                "Maximum" { $findings += "Maximum: Maximum value setting" }
                "Charge" { $findings += "Charge: Battery charge level" }
                "Color" { $findings += "Color: RGB color value" }
                "Temperature" { $findings += "Temperature: Temperature reading" }
                "Pressure" { $findings += "Pressure: Pressure reading" }
                "Quantity" { $findings += "Quantity: Item quantity" }
                "RequiredPower" { $findings += "RequiredPower: Power requirement" }
                "Idle" { $findings += "Idle: Device idle state" }
                "RecipeHash" { $findings += "RecipeHash: Current recipe hash" }
                "CompletionRatio" { $findings += "CompletionRatio: Production progress" }
                "ExportCount" { $findings += "ExportCount: Export slot occupancy" }
                "ImportCount" { $findings += "ImportCount: Import slot occupancy" }
                "Reagents" { $findings += "Reagents: Reagent mix level" }
                "Time" { $findings += "Time: Time value" }
                "Bpm" { $findings += "Bpm: Beats per minute" }
                "Volume" { $findings += "Volume: Audio volume" }
            }
        }
    }
    
    $result = [PSCustomObject]@{
        deviceKey = $device.deviceKey
        displayName = $device.displayName
        prefabName = $device.prefabName
        hasLogic = $device.hasLogic
        logicTypes = $logicTypesList
        sourceFile = $sourceFile
        findings = ($findings -join "; ")
        slots = $device.slots
    }
    
    $results += $result
}

# Export results
$output = $results | ConvertTo-Json -Depth 10
Set-Content -Path "device_analysis_complete.json" -Value $output

Write-Host "`nAnalysis complete! Results saved to device_analysis_complete.json" -ForegroundColor Green
Write-Host "Total devices analyzed: $($results.Count)" -ForegroundColor Green
Write-Host "Devices with source files found: $(($results | Where-Object { $_.sourceFile }).Count)" -ForegroundColor Green
