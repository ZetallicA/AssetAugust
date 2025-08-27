# Simple Authentication Test
Write-Host "=== Testing Azure AD Authentication ===" -ForegroundColor Cyan

# Start the application
Write-Host "Starting application..." -ForegroundColor Yellow
$process = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "AssetManagement.Web", "--urls=http://localhost:5147" -PassThru -WindowStyle Hidden

# Wait for startup
Write-Host "Waiting for application to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

# Test health endpoint
try {
    $health = Invoke-WebRequest -Uri "http://localhost:5147/health" -TimeoutSec 5
    Write-Host "✅ Application is running (Health: $($health.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ Application health check failed" -ForegroundColor Red
}

# Open browser for testing
Write-Host "Opening browser for authentication test..." -ForegroundColor Green
Start-Process "http://localhost:5147"

Write-Host ""
Write-Host "Manual Test Instructions:" -ForegroundColor Cyan
Write-Host "1. Browser should open to http://localhost:5147" -ForegroundColor White
Write-Host "2. Try to sign in" -ForegroundColor White
Write-Host "3. Check if you still get AADSTS7000218 error" -ForegroundColor White

$input = Read-Host "Press 'q' to stop the application or Enter to keep running"
if ($input -eq 'q') {
    $process | Stop-Process -Force
    Write-Host "Application stopped." -ForegroundColor Yellow
}