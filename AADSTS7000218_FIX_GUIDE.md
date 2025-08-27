# AADSTS7000218 Error Fix - Azure AD Public Client Configuration

## Error Analysis

**Current Error:**
```
AADSTS7000218: The request body must contain the following parameter: 'client_assertion' or 'client_secret'
```

**Root Cause:**
Azure AD is treating your application as a **confidential client** (requiring client_secret) instead of a **public client** (using PKCE without secrets).

## Required Azure AD Configuration Steps

### Step 1: Navigate to Your App Registration
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations**
3. Find your app with Client ID: `f582b5e5-e241-4d75-951e-de28f78b16ae`

### Step 2: Authentication Configuration
1. Click on your app registration
2. Go to **Authentication** in the left sidebar

#### Remove Incorrect Platform Configurations:
- If you see any **"Single-page application"** configurations, delete them
- If you see any **"Mobile and desktop applications"** configurations, delete them

#### Add Web Platform:
1. Click **"Add a platform"**
2. Select **"Web"**
3. **Redirect URIs:**
   ```
   https://assets.oathone.com/signin-oidc
   http://localhost:5147/signin-oidc
   https://localhost:5148/signin-oidc
   ```
4. **Logout URLs:**
   ```
   https://assets.oathone.com/signout-callback-oidc
   http://localhost:5147/signout-callback-oidc
   https://localhost:5148/signout-callback-oidc
   ```
5. **Token Configuration:**
   - ✅ **Access tokens** (used for implicit flows)
   - ✅ **ID tokens** (used for implicit and hybrid flows)

### Step 3: CRITICAL - Public Client Configuration
In the **Authentication** section, scroll down to **Advanced settings**:

#### Essential Settings:
- **Allow public client flows:** **Yes** ✅ (MUST be enabled)
- **Treat application as a public client:** **No** ❌ (MUST remain disabled)

> **Important:** This combination allows PKCE flow while maintaining Web application registration type.

### Step 4: Verify Application Type
After configuration, verify:
- **Application type:** Web application
- **Authentication method:** Authorization Code + PKCE
- **Client authentication:** Public client flows enabled
- **Client secret:** Not required

### Step 5: API Permissions
Ensure these permissions are configured:
- **Microsoft Graph**
  - User.Read (Delegated) ✅ Admin consent granted

## Configuration Validation

### Expected Result:
- Your app should be registered as a **Web application**
- **Public client flows** should be **enabled**
- **PKCE** should work without requiring client_secret
- **Authorization Code flow** should complete successfully

### Common Mistakes That Cause AADSTS7000218:
1. ❌ App registered as "Single-page application"
2. ❌ "Allow public client flows" set to No
3. ❌ "Treat application as a public client" set to Yes (for Web apps)
4. ❌ Missing proper redirect URIs for Web platform
5. ❌ Confidential client configuration when public client is needed

## Technical Explanation

**Why This Configuration Works:**
- **Web Application + Public Client Flows:** Allows Authorization Code + PKCE without client_secret
- **PKCE (Proof Key for Code Exchange):** Secures the authorization code exchange without requiring client secrets
- **Public Client:** No client_secret needed, security provided by PKCE challenge/verifier

**ASP.NET Core Configuration (Already Correct):**
Your application code is properly configured with:
- `options.UsePkce = true`
- `options.ClientSecret = null`
- `options.ResponseType = "code"`

## After Making Changes

1. **Save** all changes in Azure AD portal
2. **Wait 2-3 minutes** for propagation
3. **Clear browser cache** and cookies for your domain
4. **Test authentication** at: https://assets.oathone.com
5. **Monitor logs** for any remaining issues

## Expected Flow After Fix

1. User clicks login → Redirects to Azure AD
2. User authenticates → Azure AD returns authorization code
3. App exchanges code for tokens using PKCE (no client_secret required)
4. Authentication succeeds ✅

## If Issues Persist

Check these additional settings:
1. **Tenant settings** may restrict public client applications
2. **Conditional Access policies** may interfere
3. **App registration ownership** and permissions
4. **API permissions** admin consent status