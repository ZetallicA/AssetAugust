Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3" -NoWelcome

$app = Get-MgApplication -Filter "appId eq 'febf3ebc-aed1-4980-bf45-cad3e96cd763'"

Write-Host "=== CURRENT AZURE AD APP CONFIGURATION ===" -ForegroundColor Cyan
Write-Host "App ID: $($app.AppId)" -ForegroundColor Yellow
Write-Host "Display Name: $($app.DisplayName)" -ForegroundColor Yellow
Write-Host "IsFallbackPublicClient: $($app.IsFallbackPublicClient)" -ForegroundColor White
Write-Host "SignInAudience: $($app.SignInAudience)" -ForegroundColor White
Write-Host "Password Credentials Count: $($app.PasswordCredentials.Count)" -ForegroundColor White
Write-Host "Required Resource Access Count: $($app.RequiredResourceAccess.Count)" -ForegroundColor White

if ($app.Web) {
    Write-Host "Web Platform:" -ForegroundColor Green
    Write-Host "  Redirect URIs: $($app.Web.RedirectUris.Count)" -ForegroundColor White
    $app.Web.RedirectUris | ForEach-Object { Write-Host "    + $_" -ForegroundColor Gray }
    Write-Host "  Implicit Grant - Access Token: $($app.Web.ImplicitGrantSettings.EnableAccessTokenIssuance)" -ForegroundColor White
    Write-Host "  Implicit Grant - ID Token: $($app.Web.ImplicitGrantSettings.EnableIdTokenIssuance)" -ForegroundColor White
}

if ($app.Spa) {
    Write-Host "SPA Platform:" -ForegroundColor Green
    Write-Host "  Redirect URIs: $($app.Spa.RedirectUris.Count)" -ForegroundColor White
}

if ($app.PublicClient) {
    Write-Host "Public Client Platform:" -ForegroundColor Green  
    Write-Host "  Redirect URIs: $($app.PublicClient.RedirectUris.Count)" -ForegroundColor White
}

Write-Host ""
Write-Host "=== TRYING ALTERNATE PUBLIC CLIENT CONFIGURATION ===" -ForegroundColor Cyan

# Try configuring with explicit public client platform
$updateParams = @{
    IsFallbackPublicClient = $true
    SignInAudience = "AzureADMyOrg"
    RequiredResourceAccess = @()
    
    # Try using PublicClient platform instead of Web
    PublicClient = @{
        RedirectUris = @(
            "http://localhost:5556/signin-oidc",
            "http://localhost:5555/signin-oidc"
        )
    }
    
    # Keep Web platform minimal
    Web = @{
        RedirectUris = @()
        ImplicitGrantSettings = @{
            EnableAccessTokenIssuance = $false
            EnableIdTokenIssuance = $false
        }
    }
    
    # Clear SPA
    Spa = @{
        RedirectUris = @()
    }
}

try {
    Update-MgApplication -ApplicationId $app.Id @updateParams
    Write-Host "SUCCESS: Updated to use PublicClient platform" -ForegroundColor Green
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    
    # Fallback: Try pure Web platform with explicit public client flags
    Write-Host "Trying fallback Web platform configuration..." -ForegroundColor Yellow
    
    $fallbackParams = @{
        IsFallbackPublicClient = $true
        SignInAudience = "AzureADMyOrg"
        RequiredResourceAccess = @()
        
        Web = @{
            RedirectUris = @(
                "http://localhost:5556/signin-oidc",
                "http://localhost:5555/signin-oidc"
            )
            ImplicitGrantSettings = @{
                EnableAccessTokenIssuance = $false
                EnableIdTokenIssuance = $true
            }
        }
        
        PublicClient = @{ RedirectUris = @() }
        Spa = @{ RedirectUris = @() }
    }
    
    Update-MgApplication -ApplicationId $app.Id @fallbackParams
    Write-Host "SUCCESS: Applied fallback configuration" -ForegroundColor Green
}

# Verify final configuration
$finalApp = Get-MgApplication -Filter "appId eq 'febf3ebc-aed1-4980-bf45-cad3e96cd763'"

Write-Host ""
Write-Host "=== FINAL CONFIGURATION ===" -ForegroundColor Green
Write-Host "IsFallbackPublicClient: $($finalApp.IsFallbackPublicClient)" -ForegroundColor White
Write-Host "Public Client URIs: $($finalApp.PublicClient.RedirectUris.Count)" -ForegroundColor White
Write-Host "Web URIs: $($finalApp.Web.RedirectUris.Count)" -ForegroundColor White

Disconnect-MgGraph