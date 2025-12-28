$op = "C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\coordination\outputs"
$descPath = "C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\mod\descriptions.json"

$deviceEntries = @()
for ($i = 1; $i -le 8; $i++) {
    $f = Join-Path $op "batch-$i-output.json"
    $content = [System.IO.File]::ReadAllText($f).Trim()
    $inner = $content.Substring(1, $content.Length - 2).Trim()
    if ($inner.Length -gt 0) { $deviceEntries += $inner }
}

$devicesJson = $deviceEntries -join ",`n  "

$header = @'
{
  "version": "2.0.0",
  "devices": [
  
'@

$footer = @'

  ],
  "genericDescriptions": {
    "logic": {
      "Power": "Returns 1 if device is powered, 0 if not.",
      "On": "Power state. 1 = on, 0 = off.",
      "Error": "Returns 1 if device has an error.",
      "Lock": "Lock state. 1 = locked, 0 = unlocked.",
      "Temperature": "Temperature in Kelvin.",
      "Pressure": "Pressure in kPa.",
      "RequiredPower": "Power draw in watts.",
      "ReferenceId": "Unique device identifier.",
      "Setting": "Configurable device value."
    },
    "slots": {
      "Battery": "Slot for battery cells to power the device.",
      "ProgrammableChip": "IC10 chip slot for automation scripts.",
      "GasCanister": "Slot for gas canisters.",
      "Import": "Input slot for items entering the device.",
      "Export": "Output slot for processed items."
    },
    "versions": {
      "Tier One": "Basic version with standard efficiency.",
      "Tier Two": "Improved version with better efficiency."
    },
    "memory": {
      "StackPointer": "Current stack pointer position in device memory.",
      "ExecuteRecipe": "Triggers execution of a crafting recipe."
    }
  }
}
'@

$finalJson = $header + $devicesJson + $footer
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($descPath, $finalJson, $utf8NoBom)
Write-Host "Created descriptions.json - $((Get-Item $descPath).Length) bytes"
