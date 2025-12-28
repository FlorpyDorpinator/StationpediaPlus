# Build complete device entries for review-batch-6

$standardDescs = @{
    'Power' = 'Returns 1 if device is powered.'
    'On' = 'Device on/off state.'
    'RequiredPower' = 'Power draw in watts.'
    'Temperature' = 'Temperature in Kelvin.'
    'TemperatureInput' = 'Temperature at input in Kelvin.'
    'TemperatureOutput' = 'Temperature at output in Kelvin.'
    'TemperatureOutput2' = 'Temperature at second output in Kelvin.'
    'Pressure' = 'Pressure in kPa.'
    'PressureInput' = 'Pressure at input in kPa.'
    'PressureOutput' = 'Pressure at output in kPa.'
    'PressureOutput2' = 'Pressure at second output in kPa.'
    'Open' = 'Whether device is open.'
    'ReferenceId' = 'Unique reference ID.'
    'PrefabHash' = 'Device type hash.'
    'NameHash' = 'Custom name hash.'
    'TotalMoles' = 'Total gas moles in device.'
    'TotalMolesInput' = 'Total gas moles at input.'
    'TotalMolesOutput' = 'Total gas moles at output.'
    'TotalMolesOutput2' = 'Total gas moles at second output.'
    'Combustion' = 'Whether combustion is occurring.'
    'CombustionInput' = 'Whether combustion is occurring at input.'
    'CombustionOutput' = 'Whether combustion is occurring at output.'
    'CombustionOutput2' = 'Whether combustion is occurring at second output.'
    'Maximum' = 'Maximum capacity or value.'
    'Quantity' = 'Quantity of stored material.'
    'Charge' = 'Current charge level.'
    'Horizontal' = 'Horizontal angle setting.'
    'Vertical' = 'Vertical angle setting.'
    'Ratio' = 'Fill ratio or progress ratio.'
    'Lock' = 'Device lock state.'
    'Activate' = 'Trigger activation.'
    'Idle' = 'Whether device is idle.'
    'ClearMemory' = 'Write 1 to clear memory.'
    'RecipeHash' = 'Currently selected recipe hash.'
    'ImportCount' = 'Count of items in import slot.'
    'ExportCount' = 'Count of items in export slot.'
    'StackSize' = 'Stack size in current slot.'
    'Reagents' = 'Number of reagents in current recipe.'
    'CompletionRatio' = 'Manufacturing completion ratio (0-1).'
    'Color' = 'Display color setting.'
    'Volume' = 'Audio volume level.'
    'SoundAlert' = 'Sound alert mode.'
}

# Add gas ratios
$gasTypes = @('Oxygen','Nitrogen','CarbonDioxide','Volatiles','Water','Pollutant','NitrousOxide','Hydrogen','Steam','PollutedWater','LiquidOxygen','LiquidNitrogen','LiquidCarbonDioxide','LiquidVolatiles','LiquidPollutant','LiquidNitrousOxide','LiquidHydrogen')
foreach ($gas in $gasTypes) {
    $key = "Ratio$gas"
    $standardDescs[$key] = "$gas ratio in internal atmosphere."
    $standardDescs["${key}Input"] = "$gas ratio at input."
    $standardDescs["${key}Output"] = "$gas ratio at output."
    $standardDescs["${key}Output2"] = "$gas ratio at second output."
}

# Device-specific descriptions
$deviceSpecific = @{
    'ThingStructureFiltration' = @{
        'Mode' = 'Filtration mode: 0=Waste, 1=Filtration.'
        'Setting' = 'Target gas type to filter.'
        'Error' = 'Error state: 1 if filtration system has error.'
    }
    'ThingStructureFridgeSmall' = @{
        'Setting' = 'Target temperature setting in Kelvin.'
    }
    'ThingStructureAutomatedOven' = @{
        'Error' = 'Error state: 1 if recipe incomplete or slot issues.'
    }
    'ThingStructureArcFurnace' = @{
        'Error' = 'Error state: 1 if smelting issue or insufficient resources.'
    }
    'ThingStructureLiquidTankStorage' = @{}
    'ThingStructureTransformerSmallReversed' = @{
        'Setting' = 'Power transformation ratio setting.'
        'Error' = 'Error state: 1 if power issues.'
    }
    'ThingStructureConsole' = @{
        'Setting' = 'Current console display mode.'
        'Error' = 'Error state: 1 if console has error.'
    }
    'ThingStructureSuitStorageLocker' = @{
        'Setting' = 'Pressure/temperature target setting.'
        'Error' = 'Error state: 1 if environmental control fails.'
    }
    'ThingStructureSolarPanelFlat' = @{}
    'ThingStructureLogicReagentReader' = @{
        'Setting' = 'Selected reagent type to read.'
        'Error' = 'Error state: 1 if no valid target.'
    }
    'ThingModularDeviceLabelDiode3' = @{
        'Mode' = 'Display mode setting.'
    }
    'ThingStructureGrowLight' = @{}
    'ThingStructureLogicButton' = @{
        'Setting' = 'Button press state (read-only).'
    }
    'ThingStructurePassthroughHeatExchangerGasToLiquid' = @{
        'Setting' = 'Heat exchange target setting.'
    }
    'ThingModularDeviceGauge2x2' = @{
        'Setting' = 'Gauge display value.'
    }
    'ThingModularDeviceFlipSwitch' = @{
        'Setting' = 'Switch position (read-only).'
    }
    'ThingPortableSolarPanel' = @{}
}

function Get-DataType {
    param($logicType, $access)
    
    if ($logicType -match '^Ratio' -or $logicType -in @('Ratio','Temperature','Pressure','Power','RequiredPower','Maximum','Charge','Horizontal','Vertical','CompletionRatio','Volume')) {
        return 'Float'
    }
    elseif ($logicType -in @('On','Open','Combustion','Power','Lock','Activate','Error','Idle','CombustionInput','CombustionOutput','CombustionOutput2')) {
        return 'Boolean'
    }
    elseif ($logicType -in @('Mode','Setting','ClearMemory','ImportCount','ExportCount','StackSize','Reagents','Color','SoundAlert','Quantity')) {
        return 'Integer'
    }
    elseif ($logicType -in @('ReferenceId','PrefabHash','NameHash','RecipeHash')) {
        return 'Hash'
    }
    else {
        return 'Float'
    }
}

function Get-Range {
    param($logicType, $dataType)
    
    switch -Regex ($logicType) {
        '^(On|Open|Power|Combustion.*|Lock|Activate|Error|Idle)$' { return '0-1' }
        '^Ratio' { return '0-1' }
        '^Temperature' { return '0+ K' }
        '^Pressure' { return '0+ kPa' }
        'RequiredPower' { return '0+ W' }
        '(ReferenceId|PrefabHash|NameHash|RecipeHash)' { return 'Hash' }
        'CompletionRatio' { return '0-1' }
        'Horizontal|Vertical' { return '-180 to 180' }
        default { return '0+' }
    }
}

# Load review data
$reviewData = Get-Content 'review6_logic_data.json' -Raw | ConvertFrom-Json

$output = @()

foreach ($device in $reviewData) {
    $deviceKey = $device.device
    $deviceTitle = $device.title
    
    Write-Host "Processing $deviceTitle ($deviceKey)..." -ForegroundColor Cyan
    
    $logicDescs = @()
    
    foreach ($logicType in ($device.logics.PSObject.Properties.Name | Sort-Object)) {
        $access = $device.logics.$logicType.access
        
        # Get description
        if ($deviceSpecific[$deviceKey] -and $deviceSpecific[$deviceKey][$logicType]) {
            $desc = $deviceSpecific[$deviceKey][$logicType]
        }
        elseif ($standardDescs[$logicType]) {
            $desc = $standardDescs[$logicType]
        }
        else {
            $desc = "$logicType value."
        }
        
        $logicDescs += @{
            logicType = $logicType
            dataType = Get-DataType $logicType $access
            range = Get-Range $logicType (Get-DataType $logicType $access)
            description = $desc
            access = $access
        }
    }
    
    # Build slot descriptions if any
    $slotDescs = @()
    if ($device.slots) {
        foreach ($slot in $device.slots) {
            if ($slot.name -and $slot.name -ne 'None') {
                $slotDescs += @{
                    slotName = $slot.name
                    slotType = if ($slot.type) { $slot.type } else { 'Any' }
                    description = "Slot for $($slot.name)."
                }
            }
        }
    }
    
    $entry = @{
        deviceKey = $deviceKey
        displayName = $deviceTitle
        logicDescriptions = $logicDescs
        slotDescriptions = $slotDescs
    }
    
    $output += $entry
}

# Save output
$outputPath = 'C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\coordination\outputs\review-batch-6-complete.json'
$output | ConvertTo-Json -Depth 10 | Out-File $outputPath -Encoding UTF8

Write-Host "`n✓ Generated $($output.Count) complete device entries" -ForegroundColor Green
Write-Host "✓ Output saved to review-batch-6-complete.json" -ForegroundColor Green
Write-Host "`nSummary:" -ForegroundColor Yellow
foreach ($entry in $output) {
    Write-Host "  $($entry.displayName): $($entry.logicDescriptions.Count) logic types" -ForegroundColor White
}
