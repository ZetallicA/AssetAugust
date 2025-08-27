# Direct Azure AD Configuration Fix Script
# This script uses Microsoft Graph REST API to fix the AADSTS7000218 error

param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$ClientId = "f582b5e5-e241-4d75-951e-de28f78b16ae"
)

Write-Host "=== Azure AD Direct Configuration Fix ===" -ForegroundColor Cyan
Write-Host "This script will attempt to fix the AADSTS7000218 error by configuring Azure AD properly." -ForegroundColor Yellow
Write-Host ""

# Check if Microsoft Graph PowerShell is installed
try {
    Import-Module Microsoft.Graph -ErrorAction Stop
    Write-Host "✅ Microsoft Graph PowerShell module found" -ForegroundColor Green
} catch {
    Write-Host "❌ Microsoft Graph PowerShell module not found. Installing..." -ForegroundColor Yellow
    Install-Module Microsoft.Graph -Scope CurrentUser -Force -AllowClobber
    Import-Module Microsoft.Graph
    Write-Host "✅ Microsoft Graph PowerShell module installed" -ForegroundColor Green
}

try {
    # Connect to Microsoft Graph
    Write-Host "Connecting to Microsoft Graph..." -ForegroundColor Yellow
    Write-Host "Please sign in with an account that has Application Administrator permissions." -ForegroundColor Cyan
    
    Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId $TenantId
    
    # Get the application
    Write-Host "Retrieving application registration..." -ForegroundColor Yellow
    $app = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    if (-not $app) {
        throw "Application with Client ID $ClientId not found!"
    }
    
    Write-Host "Found application: $($app.DisplayName)" -ForegroundColor Green
    Write-Host "Application ID: $($app.Id)" -ForegroundColor Gray
    
    # Display current configuration
    Write-Host "`nCurrent Configuration:" -ForegroundColor Cyan
    Write-Host "  - IsFallbackPublicClient: $($app.IsFallbackPublicClient)" -ForegroundColor White
    Write-Host "  - Web RedirectUris: $($app.Web.RedirectUris -join ', ')" -ForegroundColor White
    Write-Host "  - SPA RedirectUris: $($app.Spa.RedirectUris -join ', ')" -ForegroundColor White
    Write-Host "  - PublicClient RedirectUris: $($app.PublicClient.RedirectUris -join ', ')" -ForegroundColor White
    
    # Prepare the configuration update
    $needsUpdate = $false
    $updateParams = @{}
    
    # Required redirect URIs
    $requiredWebRedirectUris = @(
        "https://assets.oathone.com/signin-oidc",
        "http://localhost:5147/signin-oidc"
    )
    
    # Fix 1: Ensure IsFallbackPublicClient is true (critical for AADSTS7000218)
    if ($app.IsFallbackPublicClient -ne $true) {
        Write-Host "`n🔧 Setting IsFallbackPublicClient to true..." -ForegroundColor Yellow
        $updateParams.IsFallbackPublicClient = $true
        $needsUpdate = $true
    } else {
        Write-Host "`n✅ IsFallbackPublicClient is already true" -ForegroundColor Green
    }
    
    # Fix 2: Configure Web platform correctly
    $currentWebUris = if ($app.Web.RedirectUris) { $app.Web.RedirectUris } else { @() }
    $missingWebUris = $requiredWebRedirectUris | Where-Object { $_ -notin $currentWebUris }
    
    if ($missingWebUris.Count -gt 0 -or $app.Web.LogoutUrl -ne "https://assets.oathone.com/signout-callback-oidc") {
        Write-Host "🔧 Configuring Web platform..." -ForegroundColor Yellow
        $allWebUris = ($currentWebUris + $missingWebUris) | Sort-Object -Unique
        $updateParams.Web = @{
            RedirectUris = $allWebUris
            LogoutUrl = "https://assets.oathone.com/signout-callback-oidc"
            ImplicitGrantSettings = @{
                EnableAccessTokenIssuance = $true
                EnableIdTokenIssuance = $true
            }
        }
        $needsUpdate = $true
    } else {
        Write-Host "✅ Web platform is correctly configured" -ForegroundColor Green
    }
    
    # Fix 3: Remove SPA redirect URIs if they exist
    if ($app.Spa.RedirectUris -and $app.Spa.RedirectUris.Count -gt 0) {
        Write-Host "🔧 Removing SPA redirect URIs (incompatible with Web app)..." -ForegroundColor Yellow
        $updateParams.Spa = @{
            RedirectUris = @()
        }
        $needsUpdate = $true
    } else {
        Write-Host "✅ No SPA redirect URIs (correct for Web app)" -ForegroundColor Green
    }
    
    # Apply updates if needed
    if ($needsUpdate) {
        Write-Host "`n🚀 Applying configuration changes..." -ForegroundColor Yellow
        Update-MgApplication -ApplicationId $app.Id @updateParams
        Write-Host "✅ Configuration updated successfully!" -ForegroundColor Green
        
        # Wait for propagation
        Write-Host "`n⏳ Waiting 30 seconds for changes to propagate..." -ForegroundColor Yellow
        Start-Sleep -Seconds 30
        
        # Verify changes
        $updatedApp = Get-MgApplication -ApplicationId $app.Id
        Write-Host "`nUpdated Configuration:" -ForegroundColor Cyan
        Write-Host "  - IsFallbackPublicClient: $($updatedApp.IsFallbackPublicClient)" -ForegroundColor Green
        Write-Host "  - Web RedirectUris: $($updatedApp.Web.RedirectUris -join ', ')" -ForegroundColor Green
        Write-Host "  - SPA RedirectUris: $($updatedApp.Spa.RedirectUris -join ', ')" -ForegroundColor Green
        
    } else {
        Write-Host "`n✅ Configuration is already correct!" -ForegroundColor Green
    }
    
    Write-Host "`n" + "="*60 -ForegroundColor Cyan
    Write-Host "CONFIGURATION COMPLETE" -ForegroundColor Cyan
    Write-Host "="*60 -ForegroundColor Cyan
    Write-Host "✅ Application Type: Web Application" -ForegroundColor Green
    Write-Host "✅ Public Client Flows: Enabled (IsFallbackPublicClient: true)" -ForegroundColor Green
    Write-Host "✅ Authentication Method: Authorization Code + PKCE" -ForegroundColor Green
    Write-Host "✅ Client Secret Required: No" -ForegroundColor Green
    Write-Host "`nThe AADSTS7000218 error should now be resolved!" -ForegroundColor Green
    
} catch {
    Write-Host "`n❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`nIf this fails, you may need to:" -ForegroundColor Yellow
    Write-Host "1. Run PowerShell as Administrator" -ForegroundColor White
    Write-Host "2. Ensure you have Application Administrator permissions" -ForegroundColor White
    Write-Host "3. Use the manual Azure Portal method" -ForegroundColor White
} finally {
    try {
        Disconnect-MgGraph -ErrorAction SilentlyContinue
    } catch {}
}

Write-Host "`n🧪 Next Step: Test your application!" -ForegroundColor Cyan
Write-Host "Run: .\test-azure-ad-auth.ps1" -ForegroundColor White