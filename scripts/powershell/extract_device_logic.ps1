# Extract device logic information from Stationpedia.json
$devices = @(
    "ThingLandingpad_ThreshholdPiece", "ThingLaunchSilo", "ThingLogicStepSequencer8", 
    "ThingModularDeviceAlarm", "ThingModularDeviceBigLever", "ThingModularDeviceCardReader",
    "ThingModularDeviceComputer", "ThingModularDeviceConsole", "ThingModularDeviceDial",
    "ThingModularDeviceDialSmall", "ThingModularDeviceEmergencyButton3x3", "ThingModularDeviceFlipCoverSwitch",
    "ThingModularDeviceFlipSwitch", "ThingModularDeviceGauge2x2", "ThingModularDeviceGauge3x3",
    "ThingModularDeviceLabelDiode2", "ThingModularDeviceLabelDiode3", "ThingModularDeviceLEDdisplay2",
    "ThingModularDeviceLEDdisplay3", "ThingModularDeviceLight", "ThingModularDeviceLightLarge",
    "ThingModularDeviceLightSmall", "ThingModularDeviceMeter3x3", "ThingModularDeviceNumpad",
    "ThingModularDeviceRoundButton", "ThingModularDeviceSlider", "ThingModularDeviceSliderDiode1",
    "ThingModularDeviceSliderDiode2", "ThingModularDeviceSquareButton", "ThingModularDeviceSwitch",
    "ThingModularDeviceThrottle3x2", "ThingModularDeviceUtilityButton2x2", "ThingMotherboardDebugAnalyzer",
    "ThingPassiveSpeaker", "ThingPortableSolarPanel", "ThingRobot", "ThingRover_MkI", "ThingStopWatch",
    "ThingStructureAccessBridge", "ThingStructureActiveVent", "ThingStructureAdvancedComposter",
    "ThingStructureAdvancedFurnace", "ThingStructureAdvancedPackagingMachine", "ThingStructureAirConditioner",
    "ThingStructureAirlock", "ThingStructureAirlockGate", "ThingStructureAirlockWide",
    "ThingStructureAngledBench", "ThingStructureArcFurnace", "ThingStructureAreaPowerControl",
    "ThingStructureAreaPowerControlReversed", "ThingStructureAutolathe", "ThingStructureAutomatedOven",
    "ThingStructureAutoMinerSmall", "ThingStructureBackLiquidPressureRegulator", "ThingStructureBackPressureRegulator",
    "ThingStructureBasketHoop", "ThingStructureBattery", "ThingStructureBatteryCharger",
    "ThingStructureBatteryChargerSmall", "ThingStructureBatteryLarge"
)

Write-Host "Loading Stationpedia.json..." -ForegroundColor Cyan
$json = Get-Content "Stationeers\Stationpedia\Stationpedia.json" -Raw | ConvertFrom-Json

$results = @()

foreach ($deviceKey in $devices) {
    Write-Host "Processing $deviceKey..." -ForegroundColor Yellow
    
    $page = $json.pages | Where-Object { $_.Key -eq $deviceKey } | Select-Object -First 1
    
    if ($page) {
        $logicTypes = @()
        if ($page.LogicInsert -and $page.LogicInsert.Count -gt 0) {
            $logicTypes = $page.LogicInsert | ForEach-Object { 
                $_.LogicName -replace '<[^>]+>', ''
            }
        }
        
        $slots = @()
        if ($page.Slots -and $page.Slots.Count -gt 0) {
            $slots = $page.Slots | ForEach-Object { 
                $_.SlotName 
            }
        }
        
        $result = [PSCustomObject]@{
            deviceKey = $deviceKey
            displayName = $page.Title -replace '<[^>]+>', ''
            prefabName = $page.PrefabName
            hasLogic = ($logicTypes.Count -gt 0)
            logicTypes = $logicTypes
            slots = $slots
        }
        
        $results += $result
    } else {
        Write-Host "  NOT FOUND!" -ForegroundColor Red
    }
}

# Export to JSON
$output = $results | ConvertTo-Json -Depth 10
Set-Content -Path "device_logic_data.json" -Value $output

Write-Host "`nExported data to device_logic_data.json" -ForegroundColor Green
Write-Host "Total devices processed: $($results.Count)" -ForegroundColor Green
