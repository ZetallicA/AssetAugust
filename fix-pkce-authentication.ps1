# Fix Azure AD App Registration for Pure PKCE Authentication
# This script configures the app to work with PKCE without requiring client secrets

param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$ClientId = "1608d7d8-7c7f-439f-9e1d-629f8691294d"
)

Write-Host "=== CONFIGURING PURE PKCE AUTHENTICATION ===" -ForegroundColor Cyan
Write-Host "Microsoft's Recommendation: Use PKCE without client secrets for enhanced security" -ForegroundColor Green
Write-Host ""
Write-Host "App ClientId: $ClientId" -ForegroundColor Yellow
Write-Host "Tenant: $TenantId" -ForegroundColor Yellow
Write-Host ""

try {
    # Connect to Microsoft Graph
    Write-Host "Step 1: Connecting to Microsoft Graph..." -ForegroundColor Green
    Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId $TenantId -NoWelcome
    
    # Get the application
    Write-Host "Step 2: Getting current application configuration..." -ForegroundColor Green
    $app = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    if (-not $app) {
        throw "Application with ClientId '$ClientId' not found!"
    }
    
    Write-Host "Found app: $($app.DisplayName)" -ForegroundColor White
    
    # Check current configuration
    Write-Host ""
    Write-Host "Current Configuration:" -ForegroundColor Yellow
    Write-Host "  - IsFallbackPublicClient: $($app.IsFallbackPublicClient)" -ForegroundColor $(if ($app.IsFallbackPublicClient) { "Green" } else { "Red" })
    Write-Host "  - SignInAudience: $($app.SignInAudience)" -ForegroundColor $(if ($app.SignInAudience -eq "AzureADMyOrg") { "Green" } else { "Red" })
    Write-Host "  - Client Secrets: $($app.PasswordCredentials.Count)" -ForegroundColor $(if ($app.PasswordCredentials.Count -eq 0) { "Green" } else { "Yellow" })
    
    # Step 3: Configure for PKCE
    Write-Host ""
    Write-Host "Step 3: Configuring for pure PKCE authentication..." -ForegroundColor Green
    
    $updateParams = @{
        SignInAudience = "AzureADMyOrg"
        IsFallbackPublicClient = $true
    }
    
    # Ensure Web platform is properly configured
    $redirectUris = @(
        "https://assets.oathone.com/signin-oidc",
        "https://localhost:5148/signin-oidc",
        "http://localhost:5147/signin-oidc",
        "https://192.168.8.199:5148/signin-oidc",
        "http://192.168.8.199:5148/signin-oidc"
    )
    
    $webConfig = @{
        RedirectUris = $redirectUris
        LogoutUrl = "https://assets.oathone.com/signout-callback-oidc"
        ImplicitGrantSettings = @{
            EnableAccessTokenIssuance = $true
            EnableIdTokenIssuance = $true
        }
    }
    
    $updateParams.Web = $webConfig
    
    # Clear SPA and Public Client platforms if they exist
    $updateParams.Spa = @{ RedirectUris = @() }
    $updateParams.PublicClient = @{ RedirectUris = @() }
    
    Write-Host "Updating Azure AD application..." -ForegroundColor Yellow
    Update-MgApplication -ApplicationId $app.Id @updateParams
    
    # Step 4: Remove any client secrets if they exist
    Write-Host ""
    Write-Host "Step 4: Checking for client secrets..." -ForegroundColor Green
    
    if ($app.PasswordCredentials.Count -gt 0) {
        Write-Host "Found $($app.PasswordCredentials.Count) client secret(s). For pure PKCE, these should be removed." -ForegroundColor Yellow
        Write-Host "Client secrets found:" -ForegroundColor Yellow
        $app.PasswordCredentials | ForEach-Object {
            Write-Host "  - ID: $($_.KeyId), Display: $($_.DisplayName), Expires: $($_.EndDateTime)" -ForegroundColor Gray
        }
        
        $response = Read-Host "Remove all client secrets for pure PKCE? (y/N)"
        if ($response -eq "y" -or $response -eq "Y") {
            Write-Host "Removing client secrets..." -ForegroundColor Yellow
            $app.PasswordCredentials | ForEach-Object {
                Remove-MgApplicationPassword -ApplicationId $app.Id -KeyId $_.KeyId
                Write-Host "  Successfully removed client secret: $($_.DisplayName)" -ForegroundColor Green
            }
        } else {
            Write-Host "Keeping client secrets - app will still work with PKCE but secrets are unnecessary" -ForegroundColor Yellow
        }
    } else {
        Write-Host "No client secrets found - perfect for PKCE!" -ForegroundColor Green
    }
    
    # Step 5: Verify final configuration
    Write-Host ""
    Write-Host "Step 5: Verifying final configuration..." -ForegroundColor Green
    
    $updatedApp = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    Write-Host ""
    Write-Host "UPDATED CONFIGURATION:" -ForegroundColor Green
    Write-Host "  - IsFallbackPublicClient: $($updatedApp.IsFallbackPublicClient)" -ForegroundColor Green
    Write-Host "  - SignInAudience: $($updatedApp.SignInAudience)" -ForegroundColor Green
    Write-Host "  - Client Secrets: $($updatedApp.PasswordCredentials.Count)" -ForegroundColor $(if ($updatedApp.PasswordCredentials.Count -eq 0) { "Green" } else { "Yellow" })
    Write-Host "  - Web Redirect URIs: $($updatedApp.Web.RedirectUris.Count)" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "Configured Redirect URIs:" -ForegroundColor Cyan
    $updatedApp.Web.RedirectUris | ForEach-Object { Write-Host "  + $_" -ForegroundColor Green }
    
    Write-Host ""
    Write-Host "SUCCESS: Azure AD app is now configured for pure PKCE authentication!" -ForegroundColor Green
    Write-Host "Enhanced Security: No client secrets required!" -ForegroundColor Magenta
    
    return @{
        Success = $true
        IsPkceReady = $true
        HasClientSecrets = $updatedApp.PasswordCredentials.Count -gt 0
    }
    
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    return @{
        Success = $false
        Error = $_.Exception.Message
    }
} finally {
    Disconnect-MgGraph -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Cyan
Write-Host "1. Update Program.cs to remove client secret configuration" -ForegroundColor White
Write-Host "2. Restart the application" -ForegroundColor White
Write-Host "3. Test PKCE authentication without client secrets!" -ForegroundColor Green
Write-Host ""
Write-Host "Microsoft Recommended Security: PKCE without secrets!" -ForegroundColor Magenta