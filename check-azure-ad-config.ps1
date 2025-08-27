# Check Azure AD App Registration Configuration
# This script inspects the current app registration settings

param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$ClientId = "1608d7d8-7c7f-439f-9e1d-629f8691294d"
)

Write-Host "=== CHECKING AZURE AD APP REGISTRATION CONFIGURATION ===" -ForegroundColor Cyan
Write-Host "App ClientId: $ClientId" -ForegroundColor Yellow
Write-Host "Tenant: $TenantId" -ForegroundColor Yellow
Write-Host ""

try {
    # Connect to Microsoft Graph
    Write-Host "Connecting to Microsoft Graph..." -ForegroundColor Green
    Connect-MgGraph -Scopes "Application.Read.All" -TenantId $TenantId -NoWelcome
    
    # Get the application
    Write-Host "Getting application configuration..." -ForegroundColor Green
    $app = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    if (-not $app) {
        throw "Application with ClientId '$ClientId' not found!"
    }
    
    Write-Host "✅ Found app: $($app.DisplayName)" -ForegroundColor Green
    Write-Host ""
    
    # Check critical configuration settings
    Write-Host "=== CRITICAL CONFIGURATION ANALYSIS ===" -ForegroundColor Magenta
    
    Write-Host "Application Details:" -ForegroundColor Cyan
    Write-Host "  - Display Name: $($app.DisplayName)" -ForegroundColor White
    Write-Host "  - Application ID: $($app.AppId)" -ForegroundColor White
    Write-Host "  - Object ID: $($app.Id)" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Public Client Configuration:" -ForegroundColor Cyan
    Write-Host "  - IsFallbackPublicClient: $($app.IsFallbackPublicClient)" -ForegroundColor $(if ($app.IsFallbackPublicClient) { "Green" } else { "Red" })
    Write-Host "  - SignInAudience: $($app.SignInAudience)" -ForegroundColor $(if ($app.SignInAudience -eq "AzureADMyOrg") { "Green" } else { "Red" })
    Write-Host ""
    
    Write-Host "Platform Configurations:" -ForegroundColor Cyan
    
    # Check Web platform
    if ($app.Web) {
        Write-Host "  📱 Web Platform:" -ForegroundColor Yellow
        Write-Host "    - Redirect URIs: $($app.Web.RedirectUris.Count)" -ForegroundColor White
        $app.Web.RedirectUris | ForEach-Object { Write-Host "      + $_" -ForegroundColor Gray }
        Write-Host "    - Logout URL: $($app.Web.LogoutUrl)" -ForegroundColor White
        Write-Host "    - Access Tokens: $($app.Web.ImplicitGrantSettings.EnableAccessTokenIssuance)" -ForegroundColor White
        Write-Host "    - ID Tokens: $($app.Web.ImplicitGrantSettings.EnableIdTokenIssuance)" -ForegroundColor White
    } else {
        Write-Host "  ❌ Web Platform: Not configured" -ForegroundColor Red
    }
    
    # Check SPA platform
    if ($app.Spa -and $app.Spa.RedirectUris -and $app.Spa.RedirectUris.Count -gt 0) {
        Write-Host "  📱 SPA Platform:" -ForegroundColor Yellow
        Write-Host "    - Redirect URIs: $($app.Spa.RedirectUris.Count)" -ForegroundColor Red
        $app.Spa.RedirectUris | ForEach-Object { Write-Host "      + $_" -ForegroundColor Red }
        Write-Host "    ⚠️ WARNING: SPA platform should be empty for Web apps!" -ForegroundColor Red
    } else {
        Write-Host "  ✅ SPA Platform: Correctly empty" -ForegroundColor Green
    }
    
    # Check Public Client platform
    if ($app.PublicClient -and $app.PublicClient.RedirectUris -and $app.PublicClient.RedirectUris.Count -gt 0) {
        Write-Host "  📱 Public Client Platform:" -ForegroundColor Yellow
        Write-Host "    - Redirect URIs: $($app.PublicClient.RedirectUris.Count)" -ForegroundColor Red
        $app.PublicClient.RedirectUris | ForEach-Object { Write-Host "      + $_" -ForegroundColor Red }
        Write-Host "    ⚠️ WARNING: Public Client platform should be empty for Web apps!" -ForegroundColor Red
    } else {
        Write-Host "  ✅ Public Client Platform: Correctly empty" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "Client Credentials:" -ForegroundColor Cyan
    Write-Host "  - Client Secrets: $($app.PasswordCredentials.Count)" -ForegroundColor $(if ($app.PasswordCredentials.Count -eq 0) { "Green" } else { "Yellow" })
    Write-Host "  - Client Certificates: $($app.KeyCredentials.Count)" -ForegroundColor White
    
    Write-Host ""
    Write-Host "=== CONFIGURATION DIAGNOSIS ===" -ForegroundColor Magenta
    
    $issues = @()
    $warnings = @()
    
    # Check for PKCE compatibility
    if (-not $app.IsFallbackPublicClient) {
        $issues += "❌ IsFallbackPublicClient is not enabled - required for PKCE"
    }
    
    if ($app.SignInAudience -ne "AzureADMyOrg") {
        $issues += "❌ SignInAudience is '$($app.SignInAudience)' - should be 'AzureADMyOrg' for PKCE without secrets"
    }
    
    if (-not $app.Web -or $app.Web.RedirectUris.Count -eq 0) {
        $issues += "❌ Web platform not properly configured"
    }
    
    if ($app.Spa -and $app.Spa.RedirectUris -and $app.Spa.RedirectUris.Count -gt 0) {
        $warnings += "⚠️ SPA platform configured - may conflict with Web platform"
    }
    
    if ($app.PublicClient -and $app.PublicClient.RedirectUris -and $app.PublicClient.RedirectUris.Count -gt 0) {
        $warnings += "⚠️ Public Client platform configured - may conflict with Web platform"
    }
    
    if ($issues.Count -eq 0) {
        Write-Host "✅ Configuration appears correct for PKCE!" -ForegroundColor Green
        if ($warnings.Count -gt 0) {
            Write-Host ""
            Write-Host "Warnings:" -ForegroundColor Yellow
            $warnings | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
        }
    } else {
        Write-Host "Issues Found:" -ForegroundColor Red
        $issues | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        
        if ($warnings.Count -gt 0) {
            Write-Host ""
            Write-Host "Warnings:" -ForegroundColor Yellow
            $warnings | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
        }
    }
    
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Disconnect-MgGraph -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "=== RECOMMENDED ACTIONS ===" -ForegroundColor Cyan
Write-Host "If issues were found:" -ForegroundColor White
Write-Host "1. Enable 'Allow public client flows' in Azure Portal" -ForegroundColor Gray
Write-Host "2. Set SignInAudience to 'AzureADMyOrg'" -ForegroundColor Gray
Write-Host "3. Remove any SPA or Public Client platform configurations" -ForegroundColor Gray
Write-Host "4. Ensure Web platform is properly configured" -ForegroundColor Gray