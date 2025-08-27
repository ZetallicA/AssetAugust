# Add Client Secret to New Azure AD App Registration
param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$ClientId = "1608d7d8-7c7f-439f-9e1d-629f8691294d"
)

Write-Host "=== ADDING CLIENT SECRET TO NEW APP REGISTRATION ===" -ForegroundColor Cyan
Write-Host "App ClientId: $ClientId" -ForegroundColor Yellow
Write-Host ""

try {
    # Connect to Microsoft Graph
    Write-Host "Connecting to Microsoft Graph..." -ForegroundColor Green
    Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId $TenantId -NoWelcome
    
    # Get the application
    Write-Host "Getting application..." -ForegroundColor Green
    $app = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    if (-not $app) {
        throw "Application with ClientId '$ClientId' not found!"
    }
    
    Write-Host "Found app: $($app.DisplayName)" -ForegroundColor White
    
    # Create a new client secret
    Write-Host "Creating new client secret..." -ForegroundColor Yellow
    $secretName = "NewSecret-$(Get-Date -Format 'yyyyMMdd-HHmm')"
    
    $passwordCredential = @{
        DisplayName = $secretName
        EndDateTime = (Get-Date).AddMonths(6)  # Expires in 6 months
    }
    
    $secret = Add-MgApplicationPassword -ApplicationId $app.Id -PasswordCredential $passwordCredential
    
    Write-Host ""
    Write-Host "✅ New client secret created!" -ForegroundColor Green
    Write-Host "Secret ID: $($secret.KeyId)" -ForegroundColor White
    Write-Host "Secret Value: $($secret.SecretText)" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "IMPORTANT: Copy this secret value now!" -ForegroundColor Red
    Write-Host "Secret Value: $($secret.SecretText)" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Updating application configuration..." -ForegroundColor Green
    return $secret.SecretText
    
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    return $null
} finally {
    Disconnect-MgGraph -ErrorAction SilentlyContinue
}