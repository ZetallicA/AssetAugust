# Port Configuration Test
Write-Host "=== Port Configuration Test ===" -ForegroundColor Cyan

# Based on launchSettings.json
Write-Host "`nPort Configuration:" -ForegroundColor Yellow
Write-Host "HTTP (not secure):  http://localhost:5148" -ForegroundColor Green
Write-Host "HTTPS (secure):     https://localhost:5147" -ForegroundColor Green
Write-Host "HTTP (IP):          http://192.168.8.199:5147" -ForegroundColor Green
Write-Host "HTTPS (IP):         https://192.168.8.199:5148" -ForegroundColor Green

Write-Host "`nTesting endpoints..." -ForegroundColor Yellow

# Test HTTP port 5148
try {
    $http = Invoke-WebRequest -Uri "http://localhost:5148/health" -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ HTTP port 5148: Working (Status: $($http.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ HTTP port 5148: Not responding" -ForegroundColor Red
}

# Test HTTPS port 5147 (skip certificate validation for localhost)
try {
    # Skip SSL verification for localhost testing
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
    $https = Invoke-WebRequest -Uri "https://localhost:5147/health" -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ HTTPS port 5147: Working (Status: $($https.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ HTTPS port 5147: Not responding" -ForegroundColor Red
}

# Test IP-based HTTP
try {
    $ipHttp = Invoke-WebRequest -Uri "http://192.168.8.199:5147/health" -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ HTTP IP 192.168.8.199:5147: Working (Status: $($ipHttp.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ HTTP IP 192.168.8.199:5147: Not responding" -ForegroundColor Red
}

Write-Host "`nRecommended URLs for testing:" -ForegroundColor Cyan
Write-Host "🌐 For HTTP (no SSL): http://localhost:5148" -ForegroundColor White
Write-Host "🔒 For HTTPS (SSL):   https://localhost:5147" -ForegroundColor White
Write-Host "🌍 For Cloudflare:    http://192.168.8.199:5147" -ForegroundColor White

Write-Host "`nOpening the correct HTTP URL..." -ForegroundColor Green
Start-Process "http://localhost:5148"