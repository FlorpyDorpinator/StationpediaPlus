# Build missing-batch-7-complete.json by processing in two halves

$stationpedia = Get-Content "Stationeers\Stationpedia\Stationpedia.json" -Raw | ConvertFrom-Json

$batch7a = @("ThingStructurePumpedLiquidEngine","ThingStructurePurgeValve","ThingStructureRefrigeratedVendingMachine","ThingItemRemoteDetonator","ThingStructureRocketAvionics","ThingStructureRocketCelestialTracker","ThingStructureRocketCircuitHousing","ThingStructureRocketEngineTiny","ThingStructureRocketGasCollector","ThingStructureRocketFiltrationGas","ThingStructureRocketManufactory","ThingStructureRocketMiner","ThingStructureRocketScanner","ThingStructureSDBHopper","ThingStructureSDBHopperAdvanced")

$batch7b = @("ThingStructureSDBSilo","ThingStructureSecurityPrinter","ThingItemSensorLenses","ThingStructureShelfMedium","ThingStructureShortCornerLocker","ThingStructureShortLocker","ThingStructureShower","ThingStructureShowerPowered","ThingStructureSign1x1","ThingStructureSign2x1","ThingStructureSingleBed","ThingStructureSleeper","ThingStructureSleeperLeft","ThingStructureSleeperRight")

$allDevices = $batch7a + $batch7b

$result = @()

foreach ($deviceKey in $allDevices) {
    $device = $stationpedia.pages | Where-Object { $_.Key -eq $deviceKey }
    
    if ($device) {
        $logicDesc = @{}
        
        foreach ($logic in $device.LogicInsert) {
            $logicName = $logic.LogicName -replace '<[^>]+>', ''
            $dataType = "Float"
            $range = "0+"
            $desc = "Device logic value."
            
            # Standard types
            switch ($logicName) {
                "Power" { $desc = "Current power consumption in watts."; $range = "0+ W" }
                "On" { $dataType = "Boolean"; $range = "0-1"; $desc = "Device powered on state." }
                "Open" { $dataType = "Boolean"; $range = "0-1"; $desc = "Whether device door is open." }
                "Lock" { $dataType = "Boolean"; $range = "0-1"; $desc = "Locks device when set to 1." }
                "Error" { $dataType = "Boolean"; $range = "0-1"; $desc = "1 if device has an error." }
                "Activate" { $dataType = "Boolean"; $range = "0-1"; $desc = "Activation state." }
                "Pressure" { $range = "0+ kPa"; $desc = "Internal pressure in kPa." }
                "Temperature" { $range = "0+ K"; $desc = "Internal temperature in Kelvin." }
                "RequiredPower" { $range = "0+ W"; $desc = "Power required to operate." }
                "TotalMoles" { $desc = "Total moles of gas in device." }
                "Maximum" { $desc = "Maximum capacity or value." }
                "Ratio" { $range = "0-1"; $desc = "Ratio or efficiency value." }
                "Setting" { $desc = "Device setting value." }
                "Mode" { $dataType = "Integer"; $desc = "Device operating mode." }
                "Quantity" { $desc = "Quantity of items or resources." }
                "Reagents" { $desc = "Total reagent quantity." }
                "PrefabHash" { $dataType = "Hash"; $range = "CRC32"; $desc = "Device type hash." }
                "ReferenceId" { $dataType = "Integer"; $range = "Any"; $desc = "Unique reference ID for this device instance." }
                "NameHash" { $dataType = "Integer"; $range = "Any"; $desc = "Hash of device's custom name if set." }
                "ClearMemory" { $dataType = "Boolean"; $range = "0-1"; $desc = "Clears device memory when set to 1." }
                "ImportCount" { $dataType = "Integer"; $desc = "Number of items imported." }
                "ExportCount" { $dataType = "Integer"; $desc = "Number of items exported." }
                "RecipeHash" { $dataType = "Hash"; $range = "CRC32"; $desc = "Hash of selected recipe." }
                "CompletionRatio" { $range = "0-1"; $desc = "Manufacturing completion ratio." }
                default {
                    if ($logicName -match "^Ratio") { $range = "0-1"; $desc = "$logicName ratio in atmosphere." }
                }
            }
            
            $logicDesc[$logicName] = @{
                dataType = $dataType
                range = $range
                description = $desc
            }
        }
        
        $result += @{
            deviceKey = $deviceKey
            displayName = $device.Title
            logicDescriptions = $logicDesc
        }
        
        Write-Host "Processed: $deviceKey"
    }
}

$result | ConvertTo-Json -Depth 10 | Out-File "StationpediaPlus\coordination\outputs\missing-batch-7-complete.json"
Write-Host "`nCompleted! Created missing-batch-7-complete.json with $($result.Count) devices"
