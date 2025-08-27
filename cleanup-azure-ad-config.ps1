Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3" -NoWelcome

$app = Get-MgApplication -Filter "appId eq 'febf3ebc-aed1-4980-bf45-cad3e96cd763'"

Write-Host "=== CLEANING UP AZURE AD APP CONFIGURATION ===" -ForegroundColor Cyan
Write-Host "App: $($app.DisplayName)" -ForegroundColor Yellow
Write-Host ""

Write-Host "Current Platform Configuration:" -ForegroundColor Green
Write-Host "  Web Platform Redirect URIs: $($app.Web.RedirectUris.Count)" -ForegroundColor White
Write-Host "  SPA Platform Redirect URIs: $($app.Spa.RedirectUris.Count)" -ForegroundColor White
Write-Host "  PublicClient Platform Redirect URIs: $($app.PublicClient.RedirectUris.Count)" -ForegroundColor White

Write-Host ""
Write-Host "PublicClient Redirect URIs:" -ForegroundColor Green
$app.PublicClient.RedirectUris | ForEach-Object { Write-Host "  + $_" -ForegroundColor White }

Write-Host ""
Write-Host "Cleaning up configuration..." -ForegroundColor Green

# Clean configuration - remove Web and SPA platforms, keep only PublicClient
$cleanConfig = @{
    IsFallbackPublicClient = $true
    SignInAudience = "AzureADMyOrg"
    RequiredResourceAccess = @()
    
    # Keep only PublicClient platform for PKCE
    PublicClient = @{
        RedirectUris = @(
            "https://assets.oathone.com/signin-oidc",
            "http://localhost:5147/signin-oidc",
            "https://localhost:5148/signin-oidc",
            "http://localhost:5555/signin-oidc",
            "http://localhost:5556/signin-oidc"
        )
    }
    
    # Clear Web platform completely
    Web = @{
        RedirectUris = @()
        LogoutUrl = $null
        ImplicitGrantSettings = @{
            EnableAccessTokenIssuance = $false
            EnableIdTokenIssuance = $false
        }
    }
    
    # Clear SPA platform completely
    Spa = @{
        RedirectUris = @()
    }
}

try {
    Update-MgApplication -ApplicationId $app.Id @cleanConfig
    Write-Host "SUCCESS: Configuration cleaned up" -ForegroundColor Green
    
    # Verify the cleanup
    $cleanedApp = Get-MgApplication -Filter "appId eq 'febf3ebc-aed1-4980-bf45-cad3e96cd763'"
    
    Write-Host ""
    Write-Host "CLEANED CONFIGURATION:" -ForegroundColor Green
    Write-Host "  IsFallbackPublicClient: $($cleanedApp.IsFallbackPublicClient)" -ForegroundColor Green
    Write-Host "  SignInAudience: $($cleanedApp.SignInAudience)" -ForegroundColor Green
    Write-Host "  Web Platform Redirect URIs: $($cleanedApp.Web.RedirectUris.Count)" -ForegroundColor Green
    Write-Host "  SPA Platform Redirect URIs: $($cleanedApp.Spa.RedirectUris.Count)" -ForegroundColor Green
    Write-Host "  PublicClient Platform Redirect URIs: $($cleanedApp.PublicClient.RedirectUris.Count)" -ForegroundColor Green
    Write-Host "  Web Implicit Grant Access Token: $($cleanedApp.Web.ImplicitGrantSettings.EnableAccessTokenIssuance)" -ForegroundColor Green
    Write-Host "  Web Implicit Grant ID Token: $($cleanedApp.Web.ImplicitGrantSettings.EnableIdTokenIssuance)" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "Active PublicClient Redirect URIs:" -ForegroundColor Green
    $cleanedApp.PublicClient.RedirectUris | ForEach-Object { Write-Host "  + $_" -ForegroundColor Green }
    
    Write-Host ""
    Write-Host "This should resolve the Azure AD warnings about:" -ForegroundColor Magenta
    Write-Host "  • Implicit grant settings without Web/SPA redirect URIs" -ForegroundColor Yellow
    Write-Host "  • Mobile and desktop application platform (now removed)" -ForegroundColor Yellow
    
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Disconnect-MgGraph