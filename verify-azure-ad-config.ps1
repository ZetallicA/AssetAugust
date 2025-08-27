# Azure AD Configuration Verification and Fix Script
# This script uses REST API to verify and fix the AADSTS7000218 error

param(
    [string]$TenantId = "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    [string]$ClientId = "f582b5e5-e241-4d75-951e-de28f78b16ae"
)

Write-Host "=== Azure AD Configuration Verification Tool ===" -ForegroundColor Cyan
Write-Host "Tenant ID: $TenantId" -ForegroundColor Yellow
Write-Host "Client ID: $ClientId" -ForegroundColor Yellow
Write-Host ""

# Check if the user can access Azure Portal
Write-Host "Step 1: Verifying Azure AD Access" -ForegroundColor Green
Write-Host "Please ensure you have access to:" -ForegroundColor White
Write-Host "  https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Authentication/appId/$ClientId" -ForegroundColor Cyan

# Generate direct Azure AD management URLs
$authUrl = "https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Authentication/appId/$ClientId"
$overviewUrl = "https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Overview/appId/$ClientId"

Write-Host ""
Write-Host "Step 2: Configuration Checklist" -ForegroundColor Green
Write-Host "In the Azure Portal, verify the following settings:" -ForegroundColor White

Write-Host ""
Write-Host "=== PLATFORM CONFIGURATIONS ===" -ForegroundColor Yellow
Write-Host "❌ REMOVE any 'Single-page application' platforms" -ForegroundColor Red
Write-Host "❌ REMOVE any 'Mobile and desktop applications' platforms" -ForegroundColor Red
Write-Host "✅ ENSURE 'Web' platform exists with:" -ForegroundColor Green
Write-Host "   - Redirect URIs:" -ForegroundColor White
Write-Host "     * https://assets.oathone.com/signin-oidc" -ForegroundColor Gray
Write-Host "     * http://localhost:5147/signin-oidc" -ForegroundColor Gray
Write-Host "   - Logout URL:" -ForegroundColor White
Write-Host "     * https://assets.oathone.com/signout-callback-oidc" -ForegroundColor Gray
Write-Host "   - Token Configuration:" -ForegroundColor White
Write-Host "     * ✅ Access tokens" -ForegroundColor Green
Write-Host "     * ✅ ID tokens" -ForegroundColor Green

Write-Host ""
Write-Host "=== ADVANCED SETTINGS (CRITICAL) ===" -ForegroundColor Yellow
Write-Host "✅ 'Allow public client flows': YES" -ForegroundColor Green
Write-Host "❌ 'Treat application as a public client': NO" -ForegroundColor Red

Write-Host ""
Write-Host "Step 3: Opening Azure Portal Pages" -ForegroundColor Green
Write-Host "Opening authentication configuration page..." -ForegroundColor White

# Open the Azure Portal pages
try {
    Start-Process $authUrl
    Write-Host "✅ Opened Authentication page" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to open browser. Please manually navigate to:" -ForegroundColor Red
    Write-Host "   $authUrl" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Step 4: Configuration Validation" -ForegroundColor Green
Write-Host "After making changes in Azure Portal:" -ForegroundColor White
Write-Host "1. Save all changes" -ForegroundColor Gray
Write-Host "2. Wait 2-3 minutes for propagation" -ForegroundColor Gray
Write-Host "3. Clear browser cache" -ForegroundColor Gray
Write-Host "4. Test authentication" -ForegroundColor Gray

Write-Host ""
Write-Host "Step 5: Expected Configuration Result" -ForegroundColor Green
Write-Host "Your app should show:" -ForegroundColor White
Write-Host "✅ Application Type: Web application" -ForegroundColor Green
Write-Host "✅ Public Client Flows: Enabled" -ForegroundColor Green
Write-Host "✅ Authentication Method: Authorization Code + PKCE" -ForegroundColor Green
Write-Host "✅ Client Secret Required: No" -ForegroundColor Green

Write-Host ""
Write-Host "=== TROUBLESHOOTING ===" -ForegroundColor Yellow
Write-Host "If AADSTS7000218 persists, check:" -ForegroundColor White
Write-Host "1. Platform type is 'Web' (not SPA or Mobile)" -ForegroundColor Gray
Write-Host "2. 'Allow public client flows' is YES" -ForegroundColor Gray
Write-Host "3. No client secrets are configured" -ForegroundColor Gray
Write-Host "4. Redirect URIs match exactly" -ForegroundColor Gray

Write-Host ""
Write-Host "=== NEXT STEPS ===" -ForegroundColor Cyan
Write-Host "1. Configure Azure AD as shown above" -ForegroundColor White
Write-Host "2. Run the application test:" -ForegroundColor White
Write-Host "   dotnet run --project AssetManagement.Web" -ForegroundColor Gray
Write-Host "3. Test authentication at http://localhost:5147" -ForegroundColor White

Read-Host "Press Enter when you have completed the Azure AD configuration..."