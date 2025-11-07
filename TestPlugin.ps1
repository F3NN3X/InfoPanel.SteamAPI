# Quick test script to validate the built plugin
Write-Host "Testing InfoPanel.SteamAPI Plugin Build" -ForegroundColor Green

$pluginPath = "InfoPanel.SteamAPI\bin\Release\net8.0-windows\InfoPanel.SteamAPI-v1.0.0\InfoPanel.SteamAPI"
$dllPath = Join-Path $pluginPath "InfoPanel.SteamAPI.dll"

Write-Host "Plugin Directory: $pluginPath" -ForegroundColor Cyan
Write-Host "Checking build output..."

if (Test-Path $dllPath) {
    Write-Host "Plugin DLL found" -ForegroundColor Green
    
    # Check file size
    $size = (Get-Item $dllPath).Length
    Write-Host "DLL Size: $([Math]::Round($size/1KB, 2)) KB" -ForegroundColor Cyan
    
    # Check PluginInfo.ini
    $iniPath = Join-Path $pluginPath "PluginInfo.ini"
    if (Test-Path $iniPath) {
        Write-Host "PluginInfo.ini found" -ForegroundColor Green
        Write-Host "Plugin Info:" -ForegroundColor Cyan
        Get-Content $iniPath | ForEach-Object {
            if ($_ -match "^(Name|Description|Version|Author)=(.+)") {
                Write-Host "   $($matches[1]): $($matches[2])" -ForegroundColor White
            }
        }
    } else {
        Write-Host "PluginInfo.ini missing" -ForegroundColor Red
    }
    
    # Check dependencies
    Write-Host "Dependencies:" -ForegroundColor Cyan
    Get-ChildItem $pluginPath -Filter "*.dll" | ForEach-Object {
        Write-Host "   $($_.Name) ($([Math]::Round($_.Length/1KB, 1)) KB)" -ForegroundColor White
    }
    
    # Check for ZIP package
    $zipPath = Join-Path (Split-Path $pluginPath) "InfoPanel.SteamAPI-v1.0.0.zip"
    if (Test-Path $zipPath) {
        $zipSize = (Get-Item $zipPath).Length
        Write-Host "Distribution package: InfoPanel.SteamAPI-v1.0.0.zip ($([Math]::Round($zipSize/1KB, 2)) KB)" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "Build Validation Complete!" -ForegroundColor Green
    Write-Host "Plugin is ready for InfoPanel integration" -ForegroundColor Green
    
} else {
    Write-Host "Plugin DLL not found at: $dllPath" -ForegroundColor Red
    Write-Host "Please run 'dotnet build -c Release' first" -ForegroundColor Yellow
}