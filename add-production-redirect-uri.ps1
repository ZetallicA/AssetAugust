Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3" -NoWelcome

$app = Get-MgApplication -Filter "appId eq 'febf3ebc-aed1-4980-bf45-cad3e96cd763'"

Write-Host "=== ADDING PRODUCTION REDIRECT URI ===" -ForegroundColor Cyan
Write-Host "App: $($app.DisplayName)" -ForegroundColor Yellow
Write-Host ""

Write-Host "Current PublicClient Redirect URIs:" -ForegroundColor Green
$app.PublicClient.RedirectUris | ForEach-Object { Write-Host "  + $_" -ForegroundColor White }

Write-Host ""
Write-Host "Adding production redirect URI..." -ForegroundColor Green

# Add the production redirect URI to the existing list
$newRedirectUris = @(
    "http://localhost:5556/signin-oidc",
    "http://localhost:5555/signin-oidc", 
    "https://localhost:5148/signin-oidc",
    "http://localhost:5147/signin-oidc",
    "https://assets.oathone.com/signin-oidc"
)

$updateParams = @{
    PublicClient = @{
        RedirectUris = $newRedirectUris
    }
}

Update-MgApplication -ApplicationId $app.Id @updateParams

Write-Host "SUCCESS: Added production redirect URI" -ForegroundColor Green

# Verify the update
$updatedApp = Get-MgApplication -Filter "appId eq 'febf3ebc-aed1-4980-bf45-cad3e96cd763'"

Write-Host ""
Write-Host "Updated PublicClient Redirect URIs:" -ForegroundColor Green
$updatedApp.PublicClient.RedirectUris | ForEach-Object { Write-Host "  + $_" -ForegroundColor Green }

Disconnect-MgGraph