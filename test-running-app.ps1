# Test Running Application
Write-Host "=== Testing Running Application ===" -ForegroundColor Cyan

# Check if application is running
$dotnetProcs = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
if ($dotnetProcs) {
    Write-Host "✅ Application is running!" -ForegroundColor Green
    foreach ($proc in $dotnetProcs) {
        Write-Host "   Process ID: $($proc.Id)" -ForegroundColor Gray
    }
} else {
    Write-Host "❌ No dotnet processes found" -ForegroundColor Red
    exit
}

# Test application endpoints
Write-Host ""
Write-Host "Testing application endpoints..." -ForegroundColor Yellow

# Based on launchSettings.json, the default profile should be using port 5148
$testUrls = @(
    "http://localhost:5148",
    "http://localhost:5148/health"
)

foreach ($url in $testUrls) {
    try {
        $response = Invoke-WebRequest -Uri $url -TimeoutSec 5 -ErrorAction Stop
        Write-Host "✅ $url - Status: $($response.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "❌ $url - Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Opening application for Azure AD testing..." -ForegroundColor Green
Write-Host "This should test the Azure AD configuration we fixed:" -ForegroundColor Yellow
Write-Host "- Allow public client flows: YES" -ForegroundColor White
Write-Host "- Application type: Web" -ForegroundColor White
Write-Host "- PKCE authentication without client_secret" -ForegroundColor White

# Open the application
Start-Process "http://localhost:5148"

Write-Host ""
Write-Host "Instructions:" -ForegroundColor Cyan
Write-Host "1. The browser should open to the application" -ForegroundColor White
Write-Host "2. Try to sign in with Azure AD" -ForegroundColor White
Write-Host "3. Check if AADSTS7000218 error is resolved" -ForegroundColor White
Write-Host "4. If successful, you should reach the dashboard" -ForegroundColor White