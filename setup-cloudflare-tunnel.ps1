# Comprehensive Cloudflare Tunnel Setup Script
# This script will install cloudflared and set up the tunnel

Write-Host "=== Cloudflare Tunnel Setup for Asset Management ===" -ForegroundColor Green
Write-Host ""

# Step 1: Download cloudflared
Write-Host "Step 1: Downloading cloudflared..." -ForegroundColor Yellow
$cloudflaredUrl = "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe"
$cloudflaredPath = ".\cloudflared.exe"

if (Test-Path $cloudflaredPath) {
    Write-Host "cloudflared already exists, skipping download..." -ForegroundColor Cyan
} else {
    try {
        Invoke-WebRequest -Uri $cloudflaredUrl -OutFile $cloudflaredPath
        Write-Host "cloudflared downloaded successfully!" -ForegroundColor Green
    } catch {
        Write-Host "Failed to download cloudflared: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Please download manually from: https://github.com/cloudflare/cloudflared/releases" -ForegroundColor Yellow
        exit 1
    }
}

# Step 2: Check if tunnel is already configured
Write-Host ""
Write-Host "Step 2: Checking tunnel configuration..." -ForegroundColor Yellow

$tunnelId = "07b807ec-612a-4885-b614-47b7dc879034"
$credentialsPath = "C:\Users\$env:USERNAME\.cloudflared\$tunnelId.json"

if (Test-Path $credentialsPath) {
    Write-Host "Tunnel credentials found!" -ForegroundColor Green
} else {
    Write-Host "Tunnel credentials not found. You need to authenticate first." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please run the following command manually:" -ForegroundColor Cyan
    Write-Host "  .\cloudflared.exe tunnel login" -ForegroundColor White
    Write-Host ""
    Write-Host "This will open your browser to authenticate with Cloudflare." -ForegroundColor Yellow
    Write-Host "After authentication, run this script again." -ForegroundColor Yellow
    exit 1
}

# Step 3: Check if application is running
Write-Host ""
Write-Host "Step 3: Checking if Asset Management application is running..." -ForegroundColor Yellow

$appProcess = Get-Process -Name "AssetManagement.Web" -ErrorAction SilentlyContinue
if ($appProcess) {
    Write-Host "Asset Management application is running!" -ForegroundColor Green
} else {
    Write-Host "Asset Management application is not running." -ForegroundColor Red
    Write-Host "Please start the application first using:" -ForegroundColor Yellow
    Write-Host "  .\start-for-cloudflare.ps1" -ForegroundColor White
    exit 1
}

# Step 4: Test local connectivity
Write-Host ""
Write-Host "Step 4: Testing local connectivity..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5147/health" -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "Local connectivity test passed!" -ForegroundColor Green
    } else {
        Write-Host "Local connectivity test failed. Status: $($response.StatusCode)" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Local connectivity test failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure the application is running on port 5147" -ForegroundColor Yellow
    exit 1
}

# Step 5: Start the tunnel
Write-Host ""
Write-Host "Step 5: Starting Cloudflare Tunnel..." -ForegroundColor Yellow

Write-Host "Stopping any existing tunnel processes..." -ForegroundColor Cyan
Get-Process -Name "cloudflared" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "Starting tunnel with configuration..." -ForegroundColor Cyan
Write-Host "Tunnel will connect to: http://localhost:5147" -ForegroundColor White
Write-Host "Public URL: https://assets.oathone.com" -ForegroundColor White
Write-Host ""

# Start the tunnel
Start-Process -FilePath ".\cloudflared.exe" -ArgumentList "tunnel", "--config", "cloudflare-tunnel.yml", "run" -NoNewWindow

# Wait a moment for the tunnel to start
Start-Sleep -Seconds 3

# Check if tunnel is running
$tunnelProcess = Get-Process -Name "cloudflared" -ErrorAction SilentlyContinue
if ($tunnelProcess) {
    Write-Host "Cloudflare Tunnel started successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "=== Setup Complete ===" -ForegroundColor Green
    Write-Host "Your Asset Management application is now accessible at:" -ForegroundColor Yellow
    Write-Host "  https://assets.oathone.com" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "To stop the tunnel, run:" -ForegroundColor Yellow
    Write-Host "  Get-Process -Name 'cloudflared' | Stop-Process" -ForegroundColor White
} else {
    Write-Host "Failed to start Cloudflare Tunnel." -ForegroundColor Red
    Write-Host "Please check the configuration and try again." -ForegroundColor Yellow
    exit 1
}

