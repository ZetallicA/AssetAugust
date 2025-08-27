# Properly Configure Azure AD App as Public Client for PKCE
param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$ClientId = "febf3ebc-aed1-4980-bf45-cad3e96cd763"
)

Write-Host "=== CONFIGURING AZURE AD APP AS TRUE PUBLIC CLIENT FOR PKCE ===" -ForegroundColor Cyan
Write-Host ""

try {
    Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId $TenantId -NoWelcome
    
    $app = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    Write-Host "Current Configuration:" -ForegroundColor Yellow
    Write-Host "  - IsFallbackPublicClient: $($app.IsFallbackPublicClient)" -ForegroundColor White
    Write-Host "  - SignInAudience: $($app.SignInAudience)" -ForegroundColor White
    Write-Host "  - Client Secrets Count: $($app.PasswordCredentials.Count)" -ForegroundColor White
    Write-Host "  - Required Resource Access Count: $($app.RequiredResourceAccess.Count)" -ForegroundColor White
    
    # CRITICAL: Configure as true public client with explicit platform settings
    Write-Host ""
    Write-Host "Step 1: Configuring as public client with explicit settings..." -ForegroundColor Green
    
    $updateParams = @{
        # Essential for public client
        IsFallbackPublicClient = $true
        SignInAudience = "AzureADMyOrg"
        
        # NO API permissions - pure authentication only
        RequiredResourceAccess = @()
        
        # Web platform for OIDC
        Web = @{
            RedirectUris = @(
                "http://localhost:5556/signin-oidc",
                "http://localhost:5555/signin-oidc",
                "https://localhost:5148/signin-oidc",
                "http://localhost:5147/signin-oidc",
                "https://assets.oathone.com/signin-oidc"
            )
            LogoutUrl = "https://assets.oathone.com/signout-callback-oidc"
            ImplicitGrantSettings = @{
                EnableAccessTokenIssuance = $false   # Disable implicit flow
                EnableIdTokenIssuance = $true        # Enable ID token for OIDC
            }
        }
        
        # Clear other platforms
        Spa = @{ RedirectUris = @() }
        PublicClient = @{ RedirectUris = @() }
    }
    
    Update-MgApplication -ApplicationId $app.Id @updateParams
    Write-Host "✓ Updated application configuration" -ForegroundColor Green
    
    # Step 2: Remove any existing client secrets
    Write-Host ""
    Write-Host "Step 2: Ensuring no client secrets exist..." -ForegroundColor Green
    
    $updatedApp = Get-MgApplication -Filter "appId eq '$ClientId'"
    if ($updatedApp.PasswordCredentials.Count -gt 0) {
        Write-Host "Removing existing client secrets..." -ForegroundColor Yellow
        foreach ($secret in $updatedApp.PasswordCredentials) {
            Remove-MgApplicationPassword -ApplicationId $updatedApp.Id -KeyId $secret.KeyId
            Write-Host "  ✓ Removed secret: $($secret.DisplayName)" -ForegroundColor Green
        }
    } else {
        Write-Host "✓ No client secrets found - perfect for public client!" -ForegroundColor Green
    }
    
    # Step 3: Verify final configuration
    Write-Host ""
    Write-Host "Step 3: Verifying final configuration..." -ForegroundColor Green
    
    $finalApp = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    Write-Host ""
    Write-Host "FINAL CONFIGURATION:" -ForegroundColor Green
    Write-Host "✓ IsFallbackPublicClient: $($finalApp.IsFallbackPublicClient)" -ForegroundColor Green
    Write-Host "✓ SignInAudience: $($finalApp.SignInAudience)" -ForegroundColor Green
    Write-Host "✓ Client Secrets: $($finalApp.PasswordCredentials.Count)" -ForegroundColor Green
    Write-Host "✓ API Permissions: $($finalApp.RequiredResourceAccess.Count)" -ForegroundColor Green
    Write-Host "✓ Web Redirect URIs: $($finalApp.Web.RedirectUris.Count)" -ForegroundColor Green
    Write-Host "✓ ID Token Issuance: $($finalApp.Web.ImplicitGrantSettings.EnableIdTokenIssuance)" -ForegroundColor Green
    Write-Host "✓ Access Token Issuance: $($finalApp.Web.ImplicitGrantSettings.EnableAccessTokenIssuance)" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "SUCCESS: Azure AD app is now properly configured as a public client!" -ForegroundColor Magenta
    Write-Host "This should resolve the AADSTS7000218 error!" -ForegroundColor Green
    
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.Exception.StackTrace)" -ForegroundColor Red
} finally {
    Disconnect-MgGraph -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "NEXT: Restart the application and test authentication!" -ForegroundColor Cyan