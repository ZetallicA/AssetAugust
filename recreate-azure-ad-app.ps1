# Recreate Azure AD App Registration - Complete Solution
# This script deletes the old app and creates a new one with proper PKCE configuration

param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$OldClientId = "f582b5e5-e241-4d75-951e-de28f78b16ae",
    [string]$NewAppDisplayName = "Assets for OATH"
)

Write-Host "=== RECREATING AZURE AD APP REGISTRATION ===" -ForegroundColor Cyan
Write-Host "Old App ClientId: $OldClientId" -ForegroundColor Yellow
Write-Host "New App Name: $NewAppDisplayName" -ForegroundColor Yellow
Write-Host "Tenant: $TenantId" -ForegroundColor Yellow
Write-Host ""

try {
    # Step 1: Connect to Microsoft Graph
    Write-Host "Step 1: Connecting to Microsoft Graph..." -ForegroundColor Green
    Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId $TenantId -NoWelcome
    
    # Step 2: Find and delete the old application
    Write-Host "Step 2: Finding old application..." -ForegroundColor Green
    $oldApp = Get-MgApplication -Filter "appId eq '$OldClientId'"
    
    if ($oldApp) {
        Write-Host "Found old app: $($oldApp.DisplayName)" -ForegroundColor White
        Write-Host "Deleting old application..." -ForegroundColor Yellow
        Remove-MgApplication -ApplicationId $oldApp.Id
        Write-Host "✅ Old application deleted successfully!" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Old application not found, proceeding with new creation..." -ForegroundColor Yellow
    }
    
    # Step 3: Define all redirect URIs for the new application
    Write-Host ""
    Write-Host "Step 3: Preparing new application configuration..." -ForegroundColor Green
    
    $redirectUris = @(
        "https://assets.oathone.com/signin-oidc",           # Production
        "https://192.168.8.199:5148/signin-oidc",          # IP address HTTPS
        "http://192.168.8.199:5148/signin-oidc",           # IP address HTTP
        "https://localhost:5148/signin-oidc",               # Development HTTPS
        "http://localhost:5147/signin-oidc",                # Development HTTP
        "https://localhost:5147/signin-oidc",               # Legacy HTTPS
        "http://localhost:5148/signin-oidc"                 # Legacy HTTP
    )
    
    Write-Host "Redirect URIs to be configured:" -ForegroundColor Yellow
    $redirectUris | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }
    
    # Step 4: Create the new application with proper PKCE settings
    Write-Host ""
    Write-Host "Step 4: Creating new application with PKCE configuration..." -ForegroundColor Green
    
    $appParams = @{
        DisplayName = $NewAppDisplayName
        SignInAudience = "AzureADMyOrg"  # Single tenant for PKCE without client secrets
        IsFallbackPublicClient = $true   # Enable public client flows for PKCE
        Web = @{
            RedirectUris = $redirectUris
            LogoutUrl = "https://assets.oathone.com/signout-callback-oidc"
            ImplicitGrantSettings = @{
                EnableAccessTokenIssuance = $true
                EnableIdTokenIssuance = $true
            }
        }
        RequiredResourceAccess = @(
            @{
                ResourceAppId = "00000003-0000-0000-c000-000000000000"  # Microsoft Graph
                ResourceAccess = @(
                    @{
                        Id = "e1fe6dd8-ba31-4d61-89e7-88639da4683d"    # User.Read
                        Type = "Scope"
                    },
                    @{
                        Id = "37f7f235-527c-4136-accd-4a02d197296e"    # openid
                        Type = "Scope"
                    },
                    @{
                        Id = "14dad69e-099b-42c9-810b-d002981feec1"    # profile
                        Type = "Scope"
                    },
                    @{
                        Id = "64a6cdd6-aab1-4aaf-94b8-3cc8405e90d0"    # email
                        Type = "Scope"
                    }
                )
            }
        )
    }
    
    # Create the application
    $newApp = New-MgApplication @appParams
    
    Write-Host ""
    Write-Host "✅ SUCCESS: New application created!" -ForegroundColor Green
    Write-Host "Application Details:" -ForegroundColor Cyan
    Write-Host "  - Display Name: $($newApp.DisplayName)" -ForegroundColor White
    Write-Host "  - Application ID (ClientId): $($newApp.AppId)" -ForegroundColor Yellow
    Write-Host "  - Object ID: $($newApp.Id)" -ForegroundColor White
    Write-Host "  - Sign-In Audience: $($newApp.SignInAudience)" -ForegroundColor White
    Write-Host "  - Public Client Flows: $($newApp.IsFallbackPublicClient)" -ForegroundColor White
    
    Write-Host ""
    Write-Host "Configured Redirect URIs:" -ForegroundColor Cyan
    $newApp.Web.RedirectUris | ForEach-Object { Write-Host "  + $_" -ForegroundColor Green }
    
    Write-Host ""
    Write-Host "API Permissions Configured:" -ForegroundColor Cyan
    Write-Host "  + Microsoft Graph - User.Read (Delegated)" -ForegroundColor Green
    Write-Host "  + Microsoft Graph - openid (Delegated)" -ForegroundColor Green
    Write-Host "  + Microsoft Graph - profile (Delegated)" -ForegroundColor Green
    Write-Host "  + Microsoft Graph - email (Delegated)" -ForegroundColor Green
    
    # Step 5: Save configuration for reference
    Write-Host ""
    Write-Host "Step 5: Saving configuration..." -ForegroundColor Green
    
    $configFile = "new-azure-ad-config.txt"
    $configContent = @"
NEW Azure AD Application Configuration
=====================================

Application Name: $($newApp.DisplayName)
OLD ClientId: $OldClientId
NEW ClientId: $($newApp.AppId)
TenantId: $TenantId
Object ID: $($newApp.Id)

Configuration Settings:
- Sign-In Audience: $($newApp.SignInAudience)
- Public Client Flows: $($newApp.IsFallbackPublicClient)
- Application Type: Web

Redirect URIs:
$($newApp.Web.RedirectUris -join "`n")

API Permissions:
- Microsoft Graph - User.Read (Delegated)
- Microsoft Graph - openid (Delegated)
- Microsoft Graph - profile (Delegated)
- Microsoft Graph - email (Delegated)

Created: $(Get-Date)

NEXT STEPS:
1. Update appsettings.json ClientId: $($newApp.AppId)
2. Remove client secret from user secrets
3. Uncomment PKCE lines in Program.cs
4. Restart application and test
"@
    
    $configContent | Out-File -FilePath $configFile -Encoding UTF8
    Write-Host "Configuration saved to: $configFile" -ForegroundColor Gray
    
    # Step 6: Provide clear next steps
    Write-Host ""
    Write-Host "🎯 IMPORTANT CONFIGURATION VALUES:" -ForegroundColor Magenta
    Write-Host "OLD ClientId: $OldClientId" -ForegroundColor Red
    Write-Host "NEW ClientId: $($newApp.AppId)" -ForegroundColor Yellow
    Write-Host "TenantId: $TenantId (unchanged)" -ForegroundColor White
    Write-Host ""
    
    # Return the new ClientId for automated configuration update
    return @{
        Success = $true
        NewClientId = $newApp.AppId
        OldClientId = $OldClientId
        AppName = $newApp.DisplayName
    }
    
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please check your permissions and try again." -ForegroundColor Yellow
    return @{
        Success = $false
        Error = $_.Exception.Message
    }
} finally {
    Disconnect-MgGraph -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "🔄 NEXT STEPS:" -ForegroundColor Cyan
Write-Host "1. Update appsettings.json with new ClientId" -ForegroundColor White
Write-Host "2. Remove temporary client secret from user secrets" -ForegroundColor White
Write-Host "3. Uncomment PKCE-only lines in Program.cs" -ForegroundColor White
Write-Host "4. Restart application and test authentication!" -ForegroundColor Green
Write-Host ""
Write-Host "✨ Pure PKCE authentication ready - no more client secrets needed!" -ForegroundColor Magenta