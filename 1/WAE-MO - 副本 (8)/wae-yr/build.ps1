#!/usr/bin/env pwsh

$Output = Join-Path $PSScriptRoot Build

dotnet publish src/TSMapEditor/TSMapEditor.csproj --configuration=Release --no-self-contained --output=$Output