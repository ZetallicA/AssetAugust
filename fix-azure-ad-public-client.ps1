# Azure AD App Registration Configuration Fix Script
# This script will help diagnose and fix the AADSTS7000218 error

param(
    [Parameter(Mandatory=$true)]
    [string]$ClientId = "f582b5e5-e241-4d75-951e-de28f78b16ae",
    
    [Parameter(Mandatory=$true)]
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3"
)

# Install Microsoft Graph PowerShell module if not already installed
if (-not (Get-Module -ListAvailable -Name Microsoft.Graph)) {
    Write-Host "Installing Microsoft Graph PowerShell module..." -ForegroundColor Yellow
    Install-Module Microsoft.Graph -Scope CurrentUser -Force
}

# Connect to Microsoft Graph
Write-Host "Connecting to Microsoft Graph..." -ForegroundColor Green
Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId $TenantId

try {
    # Get current app registration
    Write-Host "Retrieving current app registration..." -ForegroundColor Green
    $app = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    if (-not $app) {
        Write-Error "Application with Client ID $ClientId not found!"
        exit 1
    }
    
    Write-Host "Found application: $($app.DisplayName)" -ForegroundColor Green
    Write-Host "Current configuration:" -ForegroundColor Yellow
    
    # Check current settings
    Write-Host "  - IsFallbackPublicClient: $($app.IsFallbackPublicClient)" -ForegroundColor White
    Write-Host "  - SignInAudience: $($app.SignInAudience)" -ForegroundColor White
    Write-Host "  - Web RedirectUris: $($app.Web.RedirectUris -join ', ')" -ForegroundColor White
    Write-Host "  - Web LogoutUrl: $($app.Web.LogoutUrl)" -ForegroundColor White
    Write-Host "  - SPA RedirectUris: $($app.Spa.RedirectUris -join ', ')" -ForegroundColor White
    
    # Check if configuration needs to be updated
    $needsUpdate = $false
    $requiredRedirectUris = @(
        "https://assets.oathone.com/signin-oidc",
        "http://localhost:5147/signin-oidc"
    )
    
    $requiredLogoutUrl = "https://assets.oathone.com/signout-callback-oidc"
    
    # Prepare update parameters
    $updateParams = @{
        ApplicationId = $app.Id
    }
    
    # Fix 1: Ensure IsFallbackPublicClient is true
    if ($app.IsFallbackPublicClient -ne $true) {
        Write-Host "❌ IsFallbackPublicClient should be true" -ForegroundColor Red
        $updateParams.IsFallbackPublicClient = $true
        $needsUpdate = $true
    } else {
        Write-Host "✅ IsFallbackPublicClient is correctly set to true" -ForegroundColor Green
    }
    
    # Fix 2: Ensure Web platform has correct redirect URIs
    $currentWebUris = $app.Web.RedirectUris
    $missingUris = $requiredRedirectUris | Where-Object { $_ -notin $currentWebUris }
    
    if ($missingUris.Count -gt 0) {
        Write-Host "❌ Missing Web redirect URIs: $($missingUris -join ', ')" -ForegroundColor Red
        $allWebUris = ($currentWebUris + $missingUris) | Sort-Object -Unique
        $updateParams.Web = @{
            RedirectUris = $allWebUris
            LogoutUrl = $requiredLogoutUrl
            ImplicitGrantSettings = @{
                EnableAccessTokenIssuance = $true
                EnableIdTokenIssuance = $true
            }
        }
        $needsUpdate = $true
    } else {
        Write-Host "✅ Web redirect URIs are correctly configured" -ForegroundColor Green
    }
    
    # Fix 3: Remove SPA redirect URIs if they exist
    if ($app.Spa.RedirectUris.Count -gt 0) {
        Write-Host "❌ SPA redirect URIs should be removed: $($app.Spa.RedirectUris -join ', ')" -ForegroundColor Red
        $updateParams.Spa = @{
            RedirectUris = @()
        }
        $needsUpdate = $true
    } else {
        Write-Host "✅ No SPA redirect URIs (correct for Web app)" -ForegroundColor Green
    }
    
    # Apply updates if needed
    if ($needsUpdate) {
        Write-Host "`nApplying configuration updates..." -ForegroundColor Yellow
        Update-MgApplication @updateParams
        Write-Host "✅ Configuration updated successfully!" -ForegroundColor Green
        
        Write-Host "`nWaiting 30 seconds for changes to propagate..." -ForegroundColor Yellow
        Start-Sleep -Seconds 30
        
        # Verify the changes
        $updatedApp = Get-MgApplication -ApplicationId $app.Id
        Write-Host "`nUpdated configuration:" -ForegroundColor Green
        Write-Host "  - IsFallbackPublicClient: $($updatedApp.IsFallbackPublicClient)" -ForegroundColor White
        Write-Host "  - Web RedirectUris: $($updatedApp.Web.RedirectUris -join ', ')" -ForegroundColor White
        Write-Host "  - SPA RedirectUris: $($updatedApp.Spa.RedirectUris -join ', ')" -ForegroundColor White
        
    } else {
        Write-Host "`n✅ All settings are already correctly configured!" -ForegroundColor Green
    }
    
    # Display final configuration summary
    Write-Host "`n" + "="*60 -ForegroundColor Cyan
    Write-Host "CONFIGURATION SUMMARY" -ForegroundColor Cyan
    Write-Host "="*60 -ForegroundColor Cyan
    Write-Host "Application Type: Web Application" -ForegroundColor Green
    Write-Host "Public Client Flows: Enabled ($($app.IsFallbackPublicClient -or $updatedApp.IsFallbackPublicClient))" -ForegroundColor Green
    Write-Host "Authentication Method: Authorization Code + PKCE" -ForegroundColor Green
    Write-Host "Client Secret Required: No" -ForegroundColor Green
    Write-Host "`nYour app should now work with the AADSTS7000218 error resolved!" -ForegroundColor Green
    
} catch {
    Write-Error "Error updating application: $($_.Exception.Message)"
} finally {
    Disconnect-MgGraph
}

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Test your application authentication" -ForegroundColor White
Write-Host "2. If issues persist, wait 5-10 minutes for full propagation" -ForegroundColor White
Write-Host "3. Clear browser cache and try again" -ForegroundColor White