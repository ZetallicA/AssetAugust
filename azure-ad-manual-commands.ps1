# Manual Azure AD Configuration Commands
# Run these commands one by one to fix the AADSTS7000218 error

Write-Host "=== Manual Azure AD Fix Commands ===" -ForegroundColor Cyan
Write-Host "Run these commands one by one:" -ForegroundColor Yellow
Write-Host ""

Write-Host "1. Install Microsoft Graph PowerShell (if not installed):" -ForegroundColor Green
Write-Host "   Install-Module Microsoft.Graph -Scope CurrentUser -Force" -ForegroundColor White
Write-Host ""

Write-Host "2. Connect to Microsoft Graph:" -ForegroundColor Green
Write-Host "   Connect-MgGraph -Scopes 'Application.ReadWrite.All' -TenantId '10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3'" -ForegroundColor White
Write-Host ""

Write-Host "3. Get your application:" -ForegroundColor Green
Write-Host "   `$app = Get-MgApplication -Filter `"appId eq 'f582b5e5-e241-4d75-951e-de28f78b16ae'`"" -ForegroundColor White
Write-Host ""

Write-Host "4. Check current configuration:" -ForegroundColor Green
Write-Host "   `$app.IsFallbackPublicClient" -ForegroundColor White
Write-Host "   # This should return `$true - if it returns `$false, that's the problem!" -ForegroundColor Gray
Write-Host ""

Write-Host "5. Fix the critical setting (THIS FIXES AADSTS7000218):" -ForegroundColor Red
Write-Host "   Update-MgApplication -ApplicationId `$app.Id -IsFallbackPublicClient:`$true" -ForegroundColor White
Write-Host ""

Write-Host "6. Configure Web platform:" -ForegroundColor Green
Write-Host "   `$webConfig = @{" -ForegroundColor White
Write-Host "       RedirectUris = @(" -ForegroundColor White
Write-Host "           'https://assets.oathone.com/signin-oidc'," -ForegroundColor White
Write-Host "           'http://localhost:5147/signin-oidc'" -ForegroundColor White
Write-Host "       )" -ForegroundColor White
Write-Host "       LogoutUrl = 'https://assets.oathone.com/signout-callback-oidc'" -ForegroundColor White
Write-Host "       ImplicitGrantSettings = @{" -ForegroundColor White
Write-Host "           EnableAccessTokenIssuance = `$true" -ForegroundColor White
Write-Host "           EnableIdTokenIssuance = `$true" -ForegroundColor White
Write-Host "       }" -ForegroundColor White
Write-Host "   }" -ForegroundColor White
Write-Host "   Update-MgApplication -ApplicationId `$app.Id -Web `$webConfig" -ForegroundColor White
Write-Host ""

Write-Host "7. Remove SPA configuration (if it exists):" -ForegroundColor Green
Write-Host "   `$spaConfig = @{ RedirectUris = @() }" -ForegroundColor White
Write-Host "   Update-MgApplication -ApplicationId `$app.Id -Spa `$spaConfig" -ForegroundColor White
Write-Host ""

Write-Host "8. Verify the fix:" -ForegroundColor Green
Write-Host "   `$updatedApp = Get-MgApplication -ApplicationId `$app.Id" -ForegroundColor White
Write-Host "   `$updatedApp.IsFallbackPublicClient" -ForegroundColor White
Write-Host "   # This should now return `$true" -ForegroundColor Gray
Write-Host ""

Write-Host "9. Disconnect:" -ForegroundColor Green
Write-Host "   Disconnect-MgGraph" -ForegroundColor White
Write-Host ""

Write-Host "=== ALTERNATIVE: Use Azure Portal ===" -ForegroundColor Yellow
Write-Host "If PowerShell fails, use the Azure Portal:" -ForegroundColor White
Write-Host "1. Go to: https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Authentication/appId/f582b5e5-e241-4d75-951e-de28f78b16ae" -ForegroundColor Cyan
Write-Host "2. In 'Advanced settings':" -ForegroundColor White
Write-Host "   - Set 'Allow public client flows' to YES" -ForegroundColor Green
Write-Host "   - Keep 'Treat application as a public client' as NO" -ForegroundColor Red
Write-Host "3. Ensure platform is 'Web' (not SPA)" -ForegroundColor White
Write-Host "4. Save changes" -ForegroundColor White
Write-Host ""

Write-Host "The key fix is: IsFallbackPublicClient = true" -ForegroundColor Red
Write-Host "This enables PKCE without client_secret!" -ForegroundColor Green

# Open Azure Portal as backup
try {
    Start-Process "https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Authentication/appId/f582b5e5-e241-4d75-951e-de28f78b16ae"
    Write-Host "`nOpened Azure Portal in browser as backup option." -ForegroundColor Cyan
} catch {
    Write-Host "`nCould not open browser. Please manually navigate to the Azure Portal URL above." -ForegroundColor Yellow
}