# Fix Redirect URIs for PKCE App
param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$ClientId = "febf3ebc-aed1-4980-bf45-cad3e96cd763"
)

Write-Host "Fixing redirect URIs for PKCE authentication..." -ForegroundColor Green

try {
    Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId $TenantId -NoWelcome
    
    $app = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    $redirectUris = @(
        "http://localhost:5556/signin-oidc",
        "http://localhost:5555/signin-oidc",
        "https://localhost:5148/signin-oidc", 
        "http://localhost:5147/signin-oidc",
        "https://assets.oathone.com/signin-oidc"
    )
    
    $updateParams = @{
        Web = @{
            RedirectUris = $redirectUris
            LogoutUrl = "https://assets.oathone.com/signout-callback-oidc"
            ImplicitGrantSettings = @{
                EnableAccessTokenIssuance = $true
                EnableIdTokenIssuance = $true
            }
        }
        IsFallbackPublicClient = $true
        SignInAudience = "AzureADMyOrg"
        RequiredResourceAccess = @()
    }
    
    Update-MgApplication -ApplicationId $app.Id @updateParams
    
    Write-Host "SUCCESS: Fixed redirect URIs!" -ForegroundColor Green
    Write-Host "Redirect URIs:" -ForegroundColor Yellow
    $redirectUris | ForEach-Object { Write-Host "  + $_" -ForegroundColor Green }
    
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Disconnect-MgGraph -ErrorAction SilentlyContinue
}