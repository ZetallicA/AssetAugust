# Simple Azure AD Configuration Fix Script
param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$ClientId = "f582b5e5-e241-4d75-951e-de28f78b16ae"
)

Write-Host "=== Azure AD Configuration Fix ===" -ForegroundColor Cyan
Write-Host "Fixing AADSTS7000218 error..." -ForegroundColor Yellow

try {
    # Import Microsoft Graph module
    Write-Host "Loading Microsoft Graph module..." -ForegroundColor Yellow
    Import-Module Microsoft.Graph.Applications -Force
    
    # Connect to Microsoft Graph
    Write-Host "Connecting to Microsoft Graph..." -ForegroundColor Yellow
    Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId $TenantId
    
    # Get the application
    Write-Host "Getting application..." -ForegroundColor Yellow
    $app = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    if (-not $app) {
        throw "Application not found!"
    }
    
    Write-Host "Found app: $($app.DisplayName)" -ForegroundColor Green
    Write-Host "Current IsFallbackPublicClient: $($app.IsFallbackPublicClient)" -ForegroundColor White
    
    # Fix the critical setting
    if ($app.IsFallbackPublicClient -ne $true) {
        Write-Host "Setting IsFallbackPublicClient to true..." -ForegroundColor Yellow
        Update-MgApplication -ApplicationId $app.Id -IsFallbackPublicClient:$true
        Write-Host "SUCCESS: IsFallbackPublicClient set to true!" -ForegroundColor Green
    } else {
        Write-Host "IsFallbackPublicClient is already true" -ForegroundColor Green
    }
    
    # Configure Web platform
    $webConfig = @{
        RedirectUris = @(
            "https://assets.oathone.com/signin-oidc",
            "http://localhost:5147/signin-oidc"
        )
        LogoutUrl = "https://assets.oathone.com/signout-callback-oidc"
        ImplicitGrantSettings = @{
            EnableAccessTokenIssuance = $true
            EnableIdTokenIssuance = $true
        }
    }
    
    Write-Host "Configuring Web platform..." -ForegroundColor Yellow
    Update-MgApplication -ApplicationId $app.Id -Web $webConfig
    Write-Host "SUCCESS: Web platform configured!" -ForegroundColor Green
    
    # Remove SPA configuration if it exists
    if ($app.Spa.RedirectUris -and $app.Spa.RedirectUris.Count -gt 0) {
        Write-Host "Removing SPA redirect URIs..." -ForegroundColor Yellow
        $spaConfig = @{ RedirectUris = @() }
        Update-MgApplication -ApplicationId $app.Id -Spa $spaConfig
        Write-Host "SUCCESS: SPA configuration removed!" -ForegroundColor Green
    }
    
    Write-Host "`nConfiguration Complete!" -ForegroundColor Green
    Write-Host "AADSTS7000218 error should now be fixed!" -ForegroundColor Cyan
    
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Disconnect-MgGraph -ErrorAction SilentlyContinue
}

Write-Host "`nNext: Test your application with test-azure-ad-auth.ps1" -ForegroundColor Cyan