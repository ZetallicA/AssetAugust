# Application Startup Troubleshooting
Write-Host "=== Application Startup Troubleshooting ===" -ForegroundColor Cyan

# Check if application is running
Write-Host "1. Checking if application is running..." -ForegroundColor Yellow
$dotnetProcs = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
if ($dotnetProcs) {
    Write-Host "✅ Found $($dotnetProcs.Count) dotnet process(es)" -ForegroundColor Green
    foreach ($proc in $dotnetProcs) {
        Write-Host "   PID: $($proc.Id), CPU: $($proc.CPU)" -ForegroundColor Gray
    }
} else {
    Write-Host "❌ No dotnet processes found" -ForegroundColor Red
}

# Test SQL Server connectivity
Write-Host ""
Write-Host "2. Testing SQL Server connectivity..." -ForegroundColor Yellow
try {
    $sqlConnection = New-Object System.Data.SqlClient.SqlConnection
    $sqlConnection.ConnectionString = "Server=192.168.8.229;Database=AssetManagement;User ID=sa;Password=MSPress#1;TrustServerCertificate=True;Connection Timeout=5"
    $sqlConnection.Open()
    Write-Host "✅ SQL Server connection successful" -ForegroundColor Green
    $sqlConnection.Close()
} catch {
    Write-Host "❌ SQL Server connection failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   This might be causing the application startup failure" -ForegroundColor Yellow
}

# Test application endpoints
Write-Host ""
Write-Host "3. Testing application endpoints..." -ForegroundColor Yellow

$endpoints = @(
    "http://localhost:5148",
    "http://localhost:5148/health",
    "http://192.168.8.199:5147",
    "https://localhost:5147"
)

foreach ($endpoint in $endpoints) {
    try {
        $response = Invoke-WebRequest -Uri $endpoint -TimeoutSec 3 -ErrorAction Stop
        Write-Host "✅ $endpoint - Status: $($response.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "❌ $endpoint - $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Check for log files
Write-Host ""
Write-Host "4. Checking for application logs..." -ForegroundColor Yellow
$logPath = "C:\temp\AssetManagement\AssetManagement.Web\Logs"
if (Test-Path $logPath) {
    $logFiles = Get-ChildItem $logPath -Filter "*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 3
    if ($logFiles) {
        Write-Host "✅ Found log files:" -ForegroundColor Green
        foreach ($log in $logFiles) {
            Write-Host "   $($log.Name) - $($log.LastWriteTime)" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "⚠️ No log directory found" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "5. Recommended actions:" -ForegroundColor Cyan
Write-Host "   - If SQL connection failed: Check if SQL Server is running" -ForegroundColor White
Write-Host "   - If no dotnet processes: Application failed to start" -ForegroundColor White
Write-Host "   - Check application logs for detailed error messages" -ForegroundColor White
Write-Host "   - Try running: dotnet build to check for compilation errors" -ForegroundColor White

Write-Host ""
Write-Host "Opening browser to test..." -ForegroundColor Green
Start-Process "http://localhost:5148"