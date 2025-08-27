# Azure AD App Registration Type Fix

## Problem
The current error indicates that the Azure AD app registration is configured as a "Single-Page Application" but should be configured as a "Web" application for ASP.NET Core MVC.

**Error Message:**
```
AADSTS9002327: Tokens issued for the 'Single-Page Application' client-type may only be redeemed via cross-origin requests.
```

## Solution Steps

### 1. Access Azure Portal
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations**
3. Find your app with Client ID: `f582b5e5-e241-4d75-951e-de28f78b16ae`

### 2. Update Authentication Configuration
1. Click on your app registration
2. Go to **Authentication** in the left sidebar
3. **Remove Single-Page Application Platform:**
   - If you see a "Single-page application" platform configuration, click the delete (trash) icon to remove it

### 3. Add Web Platform
1. Click **Add a platform**
2. Select **Web**
3. **Add Redirect URIs:**
   ```
   https://assets.oathone.com/signin-oidc
   http://localhost:5147/signin-oidc
   https://localhost:5148/signin-oidc
   ```
4. **Add Logout URLs:**
   ```
   https://assets.oathone.com/signout-callback-oidc
   http://localhost:5147/signout-callback-oidc
   https://localhost:5148/signout-callback-oidc
   ```
5. **Check these options:**
   - ✅ Access tokens (used for implicit flows)
   - ✅ ID tokens (used for implicit and hybrid flows)

### 4. Configure Advanced Settings
In the **Authentication** section, scroll down to **Advanced settings**:
- **Allow public client flows:** **Yes**
- **Treat application as a public client:** **No** (should be unchecked)

### 5. Verify API Permissions
Go to **API permissions** and ensure you have:
- Microsoft Graph → User.Read (Delegated) ✅ Admin consent granted

### 6. Application Type Summary
After the changes, your app should be configured as:
- **Application type:** Web
- **Authentication flow:** Authorization Code + PKCE
- **Public client flows:** Enabled (for PKCE support)
- **Client secret:** Not required (using PKCE)

## Why This Fix Works

- **ASP.NET Core MVC apps** are server-side applications that need to be registered as "Web" applications in Azure AD
- **Single-Page Applications (SPA)** are client-side JavaScript apps that use different authentication flows
- The Authorization Code flow with PKCE that we're using is designed for web applications, not SPAs
- By changing to "Web" platform, Azure AD will properly handle the authorization code exchange

## After Making Changes

1. Save the changes in Azure AD portal
2. Wait a few minutes for propagation
3. Test the authentication by accessing: https://assets.oathone.com
4. The login should now work without the AADSTS9002327 error

## Current Application Configuration

Your ASP.NET Core app is correctly configured for:
- Authorization Code flow with PKCE
- Public client authentication (no client secret)
- Proper redirect URI handling for Cloudflare
- Token validation and user processing

The issue was purely in the Azure AD portal configuration, not in your application code.