# Manual Azure AD Configuration Verification Guide
# Use this to manually verify and fix the AADSTS7000218 error

## Current Error Analysis
The AADSTS7000218 error occurs because Azure AD expects either:
- client_assertion (for confidential clients with certificates)
- client_secret (for confidential clients with secrets)

But your ASP.NET Core app is configured as a PUBLIC CLIENT using PKCE, which doesn't need either.

## Step-by-Step Manual Fix

### 1. Open Azure Portal
Navigate to: https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Authentication/appId/f582b5e5-e241-4d75-951e-de28f78b16ae

### 2. Check Platform Configurations
In the "Platform configurations" section, you should see:

#### ❌ WRONG Configuration (causes AADSTS7000218):
- "Single-page application" platform with redirect URIs
- OR "Mobile and desktop applications" platform
- OR No "Web" platform

#### ✅ CORRECT Configuration:
- "Web" platform with these settings:
  - Redirect URIs:
    - https://assets.oathone.com/signin-oidc
    - http://localhost:5147/signin-oidc
  - Logout URL: https://assets.oathone.com/signout-callback-oidc
  - ✅ Access tokens (used for implicit flows)
  - ✅ ID tokens (used for implicit and hybrid flows)

### 3. Check Advanced Settings
Scroll down to "Advanced settings" section:

#### ❌ WRONG Configuration:
- "Allow public client flows": No
- "Treat application as a public client": Yes

#### ✅ CORRECT Configuration:
- "Allow public client flows": **Yes** ← THIS IS CRITICAL
- "Treat application as a public client": **No** ← Keep this as No for web apps

### 4. Required Actions

1. **Remove SPA Platform** (if it exists):
   - Click the trash icon next to any "Single-page application" platform
   - Click "Save"

2. **Add Web Platform** (if missing):
   - Click "Add a platform"
   - Select "Web"
   - Add redirect URIs:
     - https://assets.oathone.com/signin-oidc
     - http://localhost:5147/signin-oidc
   - Add logout URL: https://assets.oathone.com/signout-callback-oidc
   - Check both "Access tokens" and "ID tokens"
   - Click "Configure"

3. **Enable Public Client Flows**:
   - Scroll to "Advanced settings"
   - Set "Allow public client flows" to **Yes**
   - Keep "Treat application as a public client" as **No**
   - Click "Save"

### 5. Verification
After making changes, your configuration should show:
- Platform: Web application
- Public client flows: Enabled
- Authentication method: Authorization Code + PKCE
- No client secret required

### 6. Test the Fix
1. Wait 2-3 minutes for changes to propagate
2. Clear browser cache
3. Try logging into your application
4. The AADSTS7000218 error should be resolved

## Common Mistakes That Cause This Error:

1. **Wrong Application Type**: App registered as SPA instead of Web
2. **Public Client Flows Disabled**: "Allow public client flows" set to No
3. **Wrong Client Treatment**: "Treat as public client" set to Yes for web apps
4. **Missing Web Platform**: No Web platform configuration
5. **Incorrect Redirect URIs**: URIs don't match what the app is sending

## Expected Result:
Your app will use Authorization Code + PKCE flow without requiring client_secret or client_assertion.

## If Issues Persist:
1. Double-check all settings match exactly
2. Wait 5-10 minutes for full propagation
3. Try in incognito/private browser window
4. Check application logs for different error messages