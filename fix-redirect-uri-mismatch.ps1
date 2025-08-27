# Fix Azure AD Redirect URI Mismatch
# Add correct redirect URIs for current port configuration

param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$ClientId = "f582b5e5-e241-4d75-951e-de28f78b16ae"
)

Write-Host "=== Fixing Azure AD Redirect URI Mismatch ===" -ForegroundColor Cyan
Write-Host "Current error: Redirect URI 'https://localhost:5147/signin-oidc' not configured" -ForegroundColor Red
Write-Host "Application ports: HTTP 5147, HTTPS 5148" -ForegroundColor Yellow
Write-Host ""

try {
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
    Write-Host "Current Web redirect URIs:" -ForegroundColor Yellow
    $app.Web.RedirectUris | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }
    
    # Define required redirect URIs for current port configuration
    $requiredRedirectUris = @(
        "https://assets.oathone.com/signin-oidc",           # Production
        "http://localhost:5147/signin-oidc",                # Development HTTP (current port)
        "https://localhost:5148/signin-oidc",               # Development HTTPS (current port)
        "https://localhost:5147/signin-oidc",               # Legacy HTTPS (for compatibility)
        "http://localhost:5148/signin-oidc"                 # Legacy HTTP (for compatibility)
    )
    
    # Combine existing and new URIs
    $allRedirectUris = ($app.Web.RedirectUris + $requiredRedirectUris) | Sort-Object -Unique
    
    Write-Host ""
    Write-Host "Adding missing redirect URIs..." -ForegroundColor Yellow
    Write-Host "Complete redirect URI list:" -ForegroundColor Green
    $allRedirectUris | ForEach-Object { Write-Host "  + $_" -ForegroundColor Green }
    
    # Update the application
    $webConfig = @{
        RedirectUris = $allRedirectUris
        LogoutUrl = "https://assets.oathone.com/signout-callback-oidc"
        ImplicitGrantSettings = @{
            EnableAccessTokenIssuance = $true
            EnableIdTokenIssuance = $true
        }
    }
    
    Write-Host ""
    Write-Host "Updating Azure AD application..." -ForegroundColor Yellow
    Update-MgApplication -ApplicationId $app.Id -Web $webConfig
    
    Write-Host ""
    Write-Host "SUCCESS: Redirect URIs updated!" -ForegroundColor Green
    Write-Host "The following redirect URIs are now configured:" -ForegroundColor Cyan
    $allRedirectUris | ForEach-Object { Write-Host "  + $_" -ForegroundColor Green }
    
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Disconnect-MgGraph -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Wait 1-2 minutes for Azure AD changes to propagate" -ForegroundColor White
Write-Host "2. Try accessing your application again:" -ForegroundColor White
Write-Host "   - HTTP: http://localhost:5147" -ForegroundColor Green
Write-Host "   - HTTPS: https://localhost:5148" -ForegroundColor Green
Write-Host "3. Azure AD authentication should now work!" -ForegroundColor White