# Comprehensive Fix for Azure AD and Cloudflare Tunnel Issues
# This script addresses both the missing client secret and tunnel connectivity

Write-Host "=== Fixing Azure AD and Cloudflare Tunnel Issues ===" -ForegroundColor Green
Write-Host ""

# Step 1: Check current status
Write-Host "Step 1: Checking current status..." -ForegroundColor Yellow

$appProcess = Get-Process -Name "AssetManagement.Web" -ErrorAction SilentlyContinue
$tunnelProcess = Get-Process -Name "cloudflared" -ErrorAction SilentlyContinue

if ($appProcess) {
    Write-Host "✅ Asset Management application is running" -ForegroundColor Green
} else {
    Write-Host "❌ Asset Management application is NOT running" -ForegroundColor Red
}

if ($tunnelProcess) {
    Write-Host "✅ Cloudflare Tunnel is running" -ForegroundColor Green
} else {
    Write-Host "❌ Cloudflare Tunnel is NOT running" -ForegroundColor Red
}

Write-Host ""

# Step 2: Azure AD Client Secret Issue
Write-Host "Step 2: Azure AD Client Secret Issue" -ForegroundColor Yellow
Write-Host "Your Azure AD app registration shows 'No client secrets have been created'" -ForegroundColor Red
Write-Host ""
Write-Host "You need to create a client secret in Azure AD:" -ForegroundColor Cyan
Write-Host "1. Go to: https://entra.microsoft.com" -ForegroundColor White
Write-Host "2. Navigate to: App registrations > OATH Assets > Certificates & secrets" -ForegroundColor White
Write-Host "3. Click '+ New client secret'" -ForegroundColor White
Write-Host "4. Add a description (e.g., 'Asset Management App')" -ForegroundColor White
Write-Host "5. Choose expiration (recommend 12 months)" -ForegroundColor White
Write-Host "6. Click 'Add'" -ForegroundColor White
Write-Host "7. Copy the generated secret value" -ForegroundColor White
Write-Host ""
Write-Host "After creating the secret, update your appsettings.json:" -ForegroundColor Cyan
Write-Host "Replace 'YOUR_CLIENT_SECRET_HERE' with the actual secret value" -ForegroundColor White
Write-Host ""

# Step 3: Fix Cloudflare Tunnel
Write-Host "Step 3: Fixing Cloudflare Tunnel" -ForegroundColor Yellow

# Check if cloudflared exists
if (-not (Test-Path ".\cloudflared.exe")) {
    Write-Host "Downloading cloudflared..." -ForegroundColor Cyan
    try {
        Invoke-WebRequest -Uri "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe" -OutFile "cloudflared.exe"
        Write-Host "✅ cloudflared downloaded successfully" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to download cloudflared: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Please download manually from: https://github.com/cloudflare/cloudflared/releases" -ForegroundColor Yellow
    }
} else {
    Write-Host "✅ cloudflared already exists" -ForegroundColor Green
}

# Check credentials
$tunnelId = "07b807ec-612a-4885-b614-47b7dc879034"
$credentialsPath = "C:\Users\$env:USERNAME\.cloudflared\$tunnelId.json"

if (Test-Path $credentialsPath) {
    Write-Host "✅ Tunnel credentials found" -ForegroundColor Green
} else {
    Write-Host "❌ Tunnel credentials not found" -ForegroundColor Red
    Write-Host ""
    Write-Host "You need to authenticate with Cloudflare:" -ForegroundColor Cyan
    Write-Host "Run: .\cloudflared.exe tunnel login" -ForegroundColor White
    Write-Host "This will open your browser to authenticate" -ForegroundColor White
    Write-Host ""
}

# Step 4: Test local connectivity
Write-Host ""
Write-Host "Step 4: Testing local connectivity..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5147/health" -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Local connectivity test passed" -ForegroundColor Green
    } else {
        Write-Host "❌ Local connectivity test failed. Status: $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Local connectivity test failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure the application is running on port 5147" -ForegroundColor Yellow
}

Write-Host ""

# Step 5: Start tunnel if not running
if (-not $tunnelProcess) {
    Write-Host "Step 5: Starting Cloudflare Tunnel..." -ForegroundColor Yellow
    
    if (Test-Path $credentialsPath) {
        Write-Host "Starting tunnel with configuration..." -ForegroundColor Cyan
        Start-Process -FilePath ".\cloudflared.exe" -ArgumentList "tunnel", "--config", "cloudflare-tunnel.yml", "run" -NoNewWindow
        
        Start-Sleep -Seconds 3
        
        $newTunnelProcess = Get-Process -Name "cloudflared" -ErrorAction SilentlyContinue
        if ($newTunnelProcess) {
            Write-Host "✅ Cloudflare Tunnel started successfully" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to start Cloudflare Tunnel" -ForegroundColor Red
        }
    } else {
        Write-Host "❌ Cannot start tunnel - credentials not found" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Green
Write-Host ""

if (-not $appProcess) {
    Write-Host "❌ ACTION REQUIRED: Start the application" -ForegroundColor Red
    Write-Host "   Run: .\start-for-cloudflare.ps1" -ForegroundColor White
    Write-Host ""
}

Write-Host "❌ ACTION REQUIRED: Create Azure AD Client Secret" -ForegroundColor Red
Write-Host "   Follow the steps above to create a client secret" -ForegroundColor White
Write-Host "   Then update appsettings.json with the secret value" -ForegroundColor White
Write-Host ""

if (-not (Test-Path $credentialsPath)) {
    Write-Host "❌ ACTION REQUIRED: Authenticate with Cloudflare" -ForegroundColor Red
    Write-Host "   Run: .\cloudflared.exe tunnel login" -ForegroundColor White
    Write-Host ""
}

Write-Host "After completing these steps:" -ForegroundColor Cyan
Write-Host "1. Restart the application" -ForegroundColor White
Write-Host "2. Start the tunnel: .\cloudflared.exe tunnel --config cloudflare-tunnel.yml run" -ForegroundColor White
Write-Host "3. Test: https://assets.oathone.com/health" -ForegroundColor White

