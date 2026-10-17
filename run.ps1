if (-not $env:VILLAGECRAFT_ASSETS) { $env:VILLAGECRAFT_ASSETS = $PSScriptRoot }
dotnet run --project (Join-Path $PSScriptRoot "VillageCraft") -c Release -- @args
