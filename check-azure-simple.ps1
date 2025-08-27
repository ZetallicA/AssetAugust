# Simple Azure AD App Registration Check
param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$ClientId = "1608d7d8-7c7f-439f-9e1d-629f8691294d"
)

Write-Host "=== CHECKING AZURE AD APP CONFIGURATION ===" -ForegroundColor Cyan

try {
    Connect-MgGraph -Scopes "Application.Read.All" -TenantId $TenantId -NoWelcome
    
    $app = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    if (-not $app) {
        Write-Host "❌ Application not found!" -ForegroundColor Red
        return
    }
    
    Write-Host "✅ Found app: $($app.DisplayName)" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Key Settings:" -ForegroundColor Yellow
    Write-Host "- IsFallbackPublicClient: $($app.IsFallbackPublicClient)" -ForegroundColor White
    Write-Host "- SignInAudience: $($app.SignInAudience)" -ForegroundColor White
    Write-Host "- Client Secrets: $($app.PasswordCredentials.Count)" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Web Platform:" -ForegroundColor Yellow
    if ($app.Web) {
        Write-Host "- Redirect URIs: $($app.Web.RedirectUris.Count)" -ForegroundColor White
        $app.Web.RedirectUris | ForEach-Object { Write-Host "  + $_" -ForegroundColor Gray }
    } else {
        Write-Host "- Not configured" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "Analysis:" -ForegroundColor Magenta
    
    if ($app.IsFallbackPublicClient -and $app.SignInAudience -eq "AzureADMyOrg") {
        Write-Host "✅ Configuration should support PKCE" -ForegroundColor Green
    } else {
        Write-Host "❌ Configuration issues detected:" -ForegroundColor Red
        if (-not $app.IsFallbackPublicClient) {
            Write-Host "  - Public client flows not enabled" -ForegroundColor Red
        }
        if ($app.SignInAudience -ne "AzureADMyOrg") {
            Write-Host "  - SignInAudience should be AzureADMyOrg" -ForegroundColor Red
        }
    }
    
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Disconnect-MgGraph -ErrorAction SilentlyContinue
}