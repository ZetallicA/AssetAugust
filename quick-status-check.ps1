# Quick Application Status Check
Write-Host "=== Application Status Check ===" -ForegroundColor Cyan

Write-Host "Testing application endpoints..." -ForegroundColor Yellow

# Test HTTP port 5148 (should be working)
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5148/health" -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ Application is running on http://localhost:5148" -ForegroundColor Green
    Write-Host "   Health status: $($response.StatusCode)" -ForegroundColor Gray
    
    Write-Host "Opening application in browser..." -ForegroundColor Green
    Start-Process "http://localhost:5148"
    
} catch {
    Write-Host "❌ Application not responding on http://localhost:5148" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
    
    Write-Host "Checking if application is starting..." -ForegroundColor Yellow
    # Check if dotnet process is running
    $dotnetProcs = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    if ($dotnetProcs) {
        Write-Host "✅ Dotnet process is running, application may still be starting..." -ForegroundColor Yellow
        Write-Host "   Please wait a moment and try again." -ForegroundColor Gray
    } else {
        Write-Host "❌ No dotnet process found. Application is not running." -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "If application is not responding, try:" -ForegroundColor Cyan
Write-Host "1. Wait 30 seconds for startup to complete" -ForegroundColor White
Write-Host "2. Check for build errors in the console" -ForegroundColor White
Write-Host "3. Verify SQL Server is running and accessible" -ForegroundColor White