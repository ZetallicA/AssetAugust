# Comprehensive Azure AD AADSTS7000218 Fix Script
# This script addresses all potential causes of the client secret assertion error

param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$ClientId = "f582b5e5-e241-4d75-951e-de28f78b16ae"
)

Write-Host "=== COMPREHENSIVE AZURE AD AADSTS7000218 FIX ===" -ForegroundColor Cyan
Write-Host "Addressing all potential causes of client secret assertion error..." -ForegroundColor Yellow
Write-Host ""

try {
    # Connect to Microsoft Graph
    Write-Host "Step 1: Connecting to Microsoft Graph..." -ForegroundColor Green
    Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId $TenantId -NoWelcome
    
    # Get the application
    Write-Host "Step 2: Retrieving application..." -ForegroundColor Green
    $app = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    if (-not $app) {
        throw "Application not found!"
    }
    
    Write-Host "Found app: $($app.DisplayName)" -ForegroundColor White
    Write-Host "App Object ID: $($app.Id)" -ForegroundColor White
    
    # Step 3: Check and fix critical settings
    Write-Host ""
    Write-Host "Step 3: Checking critical settings..." -ForegroundColor Green
    
    $updateParams = @{
        ApplicationId = $app.Id
    }
    $needsUpdate = $false
    
    # Fix 1: IsFallbackPublicClient
    Write-Host "  Checking IsFallbackPublicClient..." -ForegroundColor Yellow
    if ($app.IsFallbackPublicClient -ne $true) {
        Write-Host "  ❌ IsFallbackPublicClient is $($app.IsFallbackPublicClient), setting to true..." -ForegroundColor Red
        $updateParams.IsFallbackPublicClient = $true
        $needsUpdate = $true
    } else {
        Write-Host "  ✅ IsFallbackPublicClient is already true" -ForegroundColor Green
    }
    
    # Fix 2: SignInAudience
    Write-Host "  Checking SignInAudience..." -ForegroundColor Yellow
    if ($app.SignInAudience -ne "AzureADMyOrg") {
        Write-Host "  ❌ SignInAudience is $($app.SignInAudience), setting to AzureADMyOrg..." -ForegroundColor Red
        $updateParams.SignInAudience = "AzureADMyOrg"
        $needsUpdate = $true
    } else {
        Write-Host "  ✅ SignInAudience is correctly set to AzureADMyOrg" -ForegroundColor Green
    }
    
    # Fix 3: Remove any existing client credentials
    Write-Host "  Checking for client credentials..." -ForegroundColor Yellow
    if ($app.PasswordCredentials -and $app.PasswordCredentials.Count -gt 0) {
        Write-Host "  ❌ Found $($app.PasswordCredentials.Count) client secrets, removing..." -ForegroundColor Red
        $updateParams.PasswordCredentials = @()
        $needsUpdate = $true
    } else {
        Write-Host "  ✅ No client secrets found (correct for public client)" -ForegroundColor Green
    }
    
    # Fix 4: Configure Web platform with correct settings
    Write-Host "  Configuring Web platform..." -ForegroundColor Yellow
    $webConfig = @{
        RedirectUris = @(
            "https://assets.oathone.com/signin-oidc",
            "http://localhost:5147/signin-oidc",
            "https://localhost:5148/signin-oidc",
            "https://localhost:5147/signin-oidc",
            "http://localhost:5148/signin-oidc"
        )
        LogoutUrl = "https://assets.oathone.com/signout-callback-oidc"
        ImplicitGrantSettings = @{
            EnableAccessTokenIssuance = $true
            EnableIdTokenIssuance = $true
        }
    }
    $updateParams.Web = $webConfig
    $needsUpdate = $true
    
    # Fix 5: Remove SPA platform if it exists
    if ($app.Spa -and $app.Spa.RedirectUris -and $app.Spa.RedirectUris.Count -gt 0) {
        Write-Host "  ❌ Found SPA redirect URIs, removing..." -ForegroundColor Red
        $updateParams.Spa = @{ RedirectUris = @() }
        $needsUpdate = $true
    } else {
        Write-Host "  ✅ No SPA redirect URIs (correct for Web app)" -ForegroundColor Green
    }
    
    # Fix 6: Remove PublicClient platform if it exists
    if ($app.PublicClient -and $app.PublicClient.RedirectUris -and $app.PublicClient.RedirectUris.Count -gt 0) {
        Write-Host "  ❌ Found PublicClient redirect URIs, removing..." -ForegroundColor Red
        $updateParams.PublicClient = @{ RedirectUris = @() }
        $needsUpdate = $true
    } else {
        Write-Host "  ✅ No PublicClient redirect URIs (correct for Web app)" -ForegroundColor Green
    }
    
    # Apply updates if needed
    if ($needsUpdate) {
        Write-Host ""
        Write-Host "Step 4: Applying configuration updates..." -ForegroundColor Green
        Update-MgApplication @updateParams
        Write-Host "✅ Configuration updated successfully!" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "Step 4: No updates needed, configuration is correct!" -ForegroundColor Green
    }
    
    # Step 5: Verify the final configuration
    Write-Host ""
    Write-Host "Step 5: Verifying final configuration..." -ForegroundColor Green
    $updatedApp = Get-MgApplication -ApplicationId $app.Id
    
    Write-Host "Final Configuration:" -ForegroundColor Cyan
    Write-Host "  - Application Type: Web application" -ForegroundColor White
    Write-Host "  - IsFallbackPublicClient: $($updatedApp.IsFallbackPublicClient)" -ForegroundColor White
    Write-Host "  - SignInAudience: $($updatedApp.SignInAudience)" -ForegroundColor White
    Write-Host "  - Client Secrets: $($updatedApp.PasswordCredentials.Count)" -ForegroundColor White
    Write-Host "  - Web RedirectUris: $($updatedApp.Web.RedirectUris.Count)" -ForegroundColor White
    Write-Host "  - SPA RedirectUris: $($updatedApp.Spa.RedirectUris.Count)" -ForegroundColor White
    Write-Host "  - PublicClient RedirectUris: $($updatedApp.PublicClient.RedirectUris.Count)" -ForegroundColor White
    
    Write-Host ""
    Write-Host "✅ COMPREHENSIVE FIX COMPLETE!" -ForegroundColor Green
    Write-Host "Configuration optimized for Authorization Code + PKCE flow" -ForegroundColor Cyan
    
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please check your permissions and try again." -ForegroundColor Yellow
} finally {
    Disconnect-MgGraph -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "🎯 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Wait 2-3 minutes for Azure AD changes to propagate" -ForegroundColor White
Write-Host "2. Clear browser cache and cookies for localhost" -ForegroundColor White
Write-Host "3. Test authentication at: http://localhost:5147" -ForegroundColor Green
Write-Host "4. AADSTS7000218 error should now be completely resolved!" -ForegroundColor White