# Configure Azure AD App for Pure OIDC Authentication (No API Access)
# This removes Microsoft Graph permissions to enable pure authentication without client credentials

param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$ClientId = "1608d7d8-7c7f-439f-9e1d-629f8691294d"
)

Write-Host "=== CONFIGURING PURE OIDC AUTHENTICATION ===" -ForegroundColor Cyan
Write-Host "Removing API permissions to enable pure authentication without client credentials" -ForegroundColor Green
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
    
    # Check current API permissions
    Write-Host ""
    Write-Host "Current API Permissions:" -ForegroundColor Yellow
    if ($app.RequiredResourceAccess -and $app.RequiredResourceAccess.Count -gt 0) {
        $app.RequiredResourceAccess | ForEach-Object {
            $resourceAppId = $_.ResourceAppId
            $resourceName = switch ($resourceAppId) {
                "00000003-0000-0000-c000-000000000000" { "Microsoft Graph" }
                default { $resourceAppId }
            }
            Write-Host "  - Resource: $resourceName" -ForegroundColor Gray
            $_.ResourceAccess | ForEach-Object {
                $permissionType = if ($_.Type -eq "Scope") { "Delegated" } else { "Application" }
                Write-Host "    + Permission: $($_.Id) ($permissionType)" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "  - No API permissions configured" -ForegroundColor Green
    }
    
    # Configure for pure OIDC authentication
    Write-Host ""
    Write-Host "Step 3: Configuring for pure OIDC authentication..." -ForegroundColor Green
    
    $updateParams = @{
        SignInAudience = "AzureADMyOrg"
        IsFallbackPublicClient = $true
        RequiredResourceAccess = @()  # Remove all API permissions
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
    
    # Clear SPA and Public Client platforms
    $updateParams.Spa = @{ RedirectUris = @() }
    $updateParams.PublicClient = @{ RedirectUris = @() }
    
    Write-Host "Updating Azure AD application..." -ForegroundColor Yellow
    Update-MgApplication -ApplicationId $app.Id @updateParams
    
    # Verify final configuration
    Write-Host ""
    Write-Host "Step 4: Verifying final configuration..." -ForegroundColor Green
    
    $updatedApp = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    Write-Host ""
    Write-Host "UPDATED CONFIGURATION:" -ForegroundColor Green
    Write-Host "  - IsFallbackPublicClient: $($updatedApp.IsFallbackPublicClient)" -ForegroundColor Green
    Write-Host "  - SignInAudience: $($updatedApp.SignInAudience)" -ForegroundColor Green
    Write-Host "  - Client Secrets: $($updatedApp.PasswordCredentials.Count)" -ForegroundColor $(if ($updatedApp.PasswordCredentials.Count -eq 0) { "Green" } else { "Yellow" })
    Write-Host "  - API Permissions: $($updatedApp.RequiredResourceAccess.Count)" -ForegroundColor Green
    Write-Host "  - Web Redirect URIs: $($updatedApp.Web.RedirectUris.Count)" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "Configured Redirect URIs:" -ForegroundColor Cyan
    $updatedApp.Web.RedirectUris | ForEach-Object { Write-Host "  + $_" -ForegroundColor Green }
    
    Write-Host ""
    Write-Host "SUCCESS: Azure AD app configured for pure OIDC authentication!" -ForegroundColor Green
    Write-Host "No API permissions = No client credentials required!" -ForegroundColor Magenta
    
    return @{
        Success = $true
        IsPureOIDC = $true
        HasApiPermissions = $updatedApp.RequiredResourceAccess.Count -gt 0
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
Write-Host "1. Restart the application" -ForegroundColor White
Write-Host "2. Test pure OIDC authentication (no API access)!" -ForegroundColor Green
Write-Host ""
Write-Host "Pure OpenID Connect: Authentication only, no API permissions!" -ForegroundColor Magenta