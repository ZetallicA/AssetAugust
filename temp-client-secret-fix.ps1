# Temporary Client Secret Workaround for AADSTS7000218
# This script creates a client secret as a temporary fix while PKCE configuration propagates

param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$ClientId = "f582b5e5-e241-4d75-951e-de28f78b16ae"
)

Write-Host "=== TEMPORARY CLIENT SECRET WORKAROUND ===" -ForegroundColor Cyan
Write-Host "Creating client secret as temporary fix for AADSTS7000218..." -ForegroundColor Yellow
Write-Host "This is a TEMPORARY solution while PKCE propagates!" -ForegroundColor Red
Write-Host ""

try {
    # Connect to Microsoft Graph
    Write-Host "Connecting to Microsoft Graph..." -ForegroundColor Green
    Connect-MgGraph -Scopes "Application.ReadWrite.All" -TenantId $TenantId -NoWelcome
    
    # Get the application
    Write-Host "Getting application..." -ForegroundColor Green
    $app = Get-MgApplication -Filter "appId eq '$ClientId'"
    
    if (-not $app) {
        throw "Application not found!"
    }
    
    Write-Host "Found app: $($app.DisplayName)" -ForegroundColor White
    
    # Create a temporary client secret
    Write-Host "Creating temporary client secret..." -ForegroundColor Yellow
    $secretName = "TempSecret-$(Get-Date -Format 'yyyyMMdd-HHmm')"
    
    $passwordCredential = @{
        DisplayName = $secretName
        EndDateTime = (Get-Date).AddMonths(1)  # Expires in 1 month
    }
    
    $secret = Add-MgApplicationPassword -ApplicationId $app.Id -PasswordCredential $passwordCredential
    
    Write-Host "✅ Temporary client secret created!" -ForegroundColor Green
    Write-Host "Secret ID: $($secret.KeyId)" -ForegroundColor White
    Write-Host "Secret Value: $($secret.SecretText)" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "WARNING: Copy this secret value now!" -ForegroundColor Red
    Write-Host "Secret Value: $($secret.SecretText)" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Set the client secret in your application:" -ForegroundColor White
    Write-Host "   dotnet user-secrets set \"AzureAd:ClientSecret\" \"$($secret.SecretText)\"" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Update Program.cs to use client secret temporarily:" -ForegroundColor White
    Write-Host "   Remove 'options.ClientSecret = null;' line" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Test authentication at: http://localhost:5147" -ForegroundColor Green
    Write-Host ""
    Write-Host "4. REMEMBER: This is temporary! Remove the secret once PKCE works." -ForegroundColor Red
    
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Disconnect-MgGraph -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "To revert to PKCE later:" -ForegroundColor Cyan
Write-Host "1. Remove the client secret from Azure AD" -ForegroundColor White
Write-Host "2. Add 'options.ClientSecret = null;' back to Program.cs" -ForegroundColor White
Write-Host "3. Remove the user secret: dotnet user-secrets remove \"AzureAd:ClientSecret\"" -ForegroundColor White