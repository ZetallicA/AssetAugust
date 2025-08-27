Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3" -NoWelcome

$app = Get-MgApplication -Filter "appId eq 'febf3ebc-aed1-4980-bf45-cad3e96cd763'"

$updateParams = @{
    IsFallbackPublicClient = $true
    SignInAudience = "AzureADMyOrg"
    RequiredResourceAccess = @()
    Web = @{
        ImplicitGrantSettings = @{
            EnableAccessTokenIssuance = $false
            EnableIdTokenIssuance = $true
        }
    }
}

Update-MgApplication -ApplicationId $app.Id @updateParams

Write-Host "SUCCESS: Configured as true public client" -ForegroundColor Green
Write-Host "IsFallbackPublicClient: $($app.IsFallbackPublicClient)" -ForegroundColor Yellow

Disconnect-MgGraph