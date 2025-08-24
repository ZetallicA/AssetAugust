# Start Asset Management for Cloudflare Tunnel
# This script starts the application bound to 0.0.0.0 for external access

Write-Host "Starting Asset Management for Cloudflare Tunnel..." -ForegroundColor Green

# Kill any existing dotnet processes
Write-Host "Stopping any existing dotnet processes..." -ForegroundColor Yellow
taskkill /F /IM dotnet.exe 2>$null

# Set environment variables for Cloudflare
$env:ASPNETCORE_URLS = "https://0.0.0.0:5147;http://0.0.0.0:5148"
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Navigate to the web project directory
Set-Location "AssetManagement.Web"

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful! Starting application..." -ForegroundColor Green
    Write-Host "Application will be available at:" -ForegroundColor Cyan
    Write-Host "  Local: https://localhost:5147" -ForegroundColor White
    Write-Host "  External: https://assets.oathone.com" -ForegroundColor White
    Write-Host ""
    Write-Host "Press Ctrl+C to stop the application" -ForegroundColor Yellow
    
    # Start the application using the cloudflare profile
    dotnet run --launch-profile cloudflare
} else {
    Write-Host "Build failed! Please fix the errors and try again." -ForegroundColor Red
}

