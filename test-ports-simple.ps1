# Port Configuration Test
Write-Host "=== Port Configuration Test ===" -ForegroundColor Cyan

Write-Host ""
Write-Host "Port Configuration:" -ForegroundColor Yellow
Write-Host "HTTP (not secure):  http://localhost:5148" -ForegroundColor Green
Write-Host "HTTPS (secure):     https://localhost:5147" -ForegroundColor Green
Write-Host "HTTP (IP):          http://192.168.8.199:5147" -ForegroundColor Green
Write-Host "HTTPS (IP):         https://192.168.8.199:5148" -ForegroundColor Green

Write-Host ""
Write-Host "Testing endpoints..." -ForegroundColor Yellow

# Test HTTP port 5148
try {
    $http = Invoke-WebRequest -Uri "http://localhost:5148/health" -TimeoutSec 5 -ErrorAction Stop
    Write-Host "HTTP port 5148: Working (Status: $($http.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "HTTP port 5148: Not responding" -ForegroundColor Red
}

# Test IP-based HTTP
try {
    $ipHttp = Invoke-WebRequest -Uri "http://192.168.8.199:5147/health" -TimeoutSec 5 -ErrorAction Stop
    Write-Host "HTTP IP 192.168.8.199:5147: Working (Status: $($ipHttp.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "HTTP IP 192.168.8.199:5147: Not responding" -ForegroundColor Red
}

Write-Host ""
Write-Host "Recommended URLs for testing:" -ForegroundColor Cyan
Write-Host "For HTTP (no SSL): http://localhost:5148" -ForegroundColor White
Write-Host "For HTTPS (SSL):   https://localhost:5147" -ForegroundColor White
Write-Host "For Cloudflare:    http://192.168.8.199:5147" -ForegroundColor White

Write-Host ""
Write-Host "Opening the correct HTTP URL..." -ForegroundColor Green
Start-Process "http://localhost:5148"