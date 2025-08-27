# Create New Azure AD App Registration for Pure PKCE Authentication
# This creates a fresh app registration with proper PKCE configuration from the start

param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$AppDisplayName = "AssetManagement-PKCE-Pure"
)

Write-Host "=== CREATING NEW AZURE AD APP FOR PURE PKCE ===" -ForegroundColor Cyan
Write-Host "This will create a fresh app registration with proper PKCE configuration" -ForegroundColor Green
Write-Host ""
Write-Host "App Name: $AppDisplayName" -ForegroundColor Yellow
Write-Host "Tenant: $TenantId" -ForegroundColor Yellow
Write-Host ""

try {
    # Connect to Microsoft Graph
    Write-Host "Step 1: Connecting to Microsoft Graph..." -ForegroundColor Green
    Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId $TenantId -NoWelcome
    
    # Define redirect URIs
    Write-Host "Step 2: Preparing PKCE application configuration..." -ForegroundColor Green
    
    $redirectUris = @(
        "https://assets.oathone.com/signin-oidc",
        "https://localhost:5148/signin-oidc",
        "http://localhost:5147/signin-oidc",
        "https://192.168.8.199:5148/signin-oidc",
        "http://192.168.8.199:5148/signin-oidc"
    )
    
    Write-Host "Redirect URIs:" -ForegroundColor Yellow
    $redirectUris | ForEach-Object { Write-Host "  + $_" -ForegroundColor Gray }
    
    # Create the application with PROPER PKCE settings from the start
    Write-Host ""
    Write-Host "Step 3: Creating new application with pure PKCE configuration..." -ForegroundColor Green
    
    $appParams = @{
        DisplayName = $AppDisplayName
        SignInAudience = "AzureADMyOrg"  # Single tenant for PKCE without client secrets
        IsFallbackPublicClient = $true   # Enable public client flows for PKCE
        RequiredResourceAccess = @()     # NO API permissions for pure authentication
        Web = @{
            RedirectUris = $redirectUris
            LogoutUrl = "https://assets.oathone.com/signout-callback-oidc"
            ImplicitGrantSettings = @{
                EnableAccessTokenIssuance = $true
                EnableIdTokenIssuance = $true
            }
        }
        # Explicitly ensure no SPA or Public Client platform configurations
        Spa = @{ RedirectUris = @() }
        PublicClient = @{ RedirectUris = @() }
    }
    
    # Create the application
    $newApp = New-MgApplication @appParams
    
    Write-Host ""
    Write-Host "SUCCESS: New PKCE application created!" -ForegroundColor Green
    Write-Host "Application Details:" -ForegroundColor Cyan
    Write-Host "  - Display Name: $($newApp.DisplayName)" -ForegroundColor White
    Write-Host "  - Application ID (ClientId): $($newApp.AppId)" -ForegroundColor Yellow
    Write-Host "  - Object ID: $($newApp.Id)" -ForegroundColor White
    Write-Host "  - Sign-In Audience: $($newApp.SignInAudience)" -ForegroundColor White
    Write-Host "  - Public Client Flows: $($newApp.IsFallbackPublicClient)" -ForegroundColor White
    Write-Host "  - API Permissions: $($newApp.RequiredResourceAccess.Count)" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "Configured Redirect URIs:" -ForegroundColor Cyan
    $newApp.Web.RedirectUris | ForEach-Object { Write-Host "  + $_" -ForegroundColor Green }
    
    # Save configuration for reference
    Write-Host ""
    Write-Host "Step 4: Saving configuration..." -ForegroundColor Green
    
    $configFile = "new-pkce-app-config.txt"
    $configContent = @"
NEW AZURE AD PKCE APPLICATION CONFIGURATION
===========================================

Application Name: $($newApp.DisplayName)
OLD ClientId: 1608d7d8-7c7f-439f-9e1d-629f8691294d
NEW ClientId: $($newApp.AppId)
TenantId: $TenantId
Object ID: $($newApp.Id)

Configuration Settings:
- Sign-In Audience: $($newApp.SignInAudience)
- Public Client Flows: $($newApp.IsFallbackPublicClient)
- Application Type: Web (with public client flows)
- Client Secrets: 0 (not required for PKCE)
- API Permissions: 0 (pure authentication only)

Redirect URIs:
$($newApp.Web.RedirectUris -join "`n")

Created: $(Get-Date)

NEXT STEPS:
1. Update appsettings.json with NEW ClientId: $($newApp.AppId)
2. Update appsettings.Development.json with NEW ClientId: $($newApp.AppId)
3. Restart application and test
"@
    
    $configContent | Out-File -FilePath $configFile -Encoding UTF8
    Write-Host "Configuration saved to: $configFile" -ForegroundColor Gray
    
    Write-Host ""
    Write-Host "IMPORTANT - UPDATE THESE FILES:" -ForegroundColor Magenta
    Write-Host "OLD ClientId: 1608d7d8-7c7f-439f-9e1d-629f8691294d" -ForegroundColor Red
    Write-Host "NEW ClientId: $($newApp.AppId)" -ForegroundColor Yellow
    
    return @{
        Success = $true
        NewClientId = $newApp.AppId
        OldClientId = "1608d7d8-7c7f-439f-9e1d-629f8691294d"
        AppName = $newApp.DisplayName
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
Write-Host "1. Update both appsettings files with the NEW ClientId" -ForegroundColor White
Write-Host "2. Restart the application" -ForegroundColor White
Write-Host "3. Test pure PKCE authentication!" -ForegroundColor Green
Write-Host ""
Write-Host "Pure PKCE: No secrets, no API permissions, just authentication!" -ForegroundColor Magenta