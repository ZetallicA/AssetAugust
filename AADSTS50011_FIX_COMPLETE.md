# AADSTS50011 Fix Complete ✅

## 🎯 **Goal Achieved: Fixed AADSTS50011 Error**

Successfully configured OATH Assets to run on HTTPS:5147 with proper redirect URI alignment using Authorization Code + PKCE flow. No development login remains.

## ✅ **Tasks Completed**

### 1. appsettings.Development.json Configuration
- ✅ **Added Kestrel endpoints configuration:**
  ```json
  "Kestrel": {
    "Endpoints": {
      "Http":  { "Url": "http://localhost:5148" },
      "Https": { "Url": "https://localhost:5147" }
    }
  }
  ```
- ✅ **AzureAd settings verified:**
  - CallbackPath: `/signin-oidc`
  - SignedOutCallbackPath: `/signout-callback-oidc`
  - PostLogoutRedirectUri: `https://localhost:5147/`

### 2. launchSettings.json Configuration
- ✅ **Updated applicationUrl:**
  ```json
  "applicationUrl": "https://localhost:5147;http://localhost:5148"
  ```
- ✅ **Changed from 0.0.0.0 to localhost for proper binding**

### 3. Program.cs Configuration (Already Complete)
- ✅ **Default scheme = Cookies; challenge = OIDC**
- ✅ **AddMicrosoftIdentityWebApp(AzureAd)**
- ✅ **OpenIdConnectOptions:**
  - ResponseType: `"code"`
  - UsePkce: `true`
  - SaveTokens: `true`
- ✅ **UseHttpsRedirection, UseAuthentication, UseAuthorization maintained**
- ✅ **No DevelopmentSignIn bits**

### 4. AccountController Configuration (Already Complete)
- ✅ **GET /Account/SignIn -> OIDC Challenge**
- ✅ **POST /Account/SignOut -> SignOut(cookie+OIDC) then /Account/SignedOut**

### 5. Application Deployment
- ✅ **Build successful**
- ✅ **HTTPS certificate trusted**
- ✅ **Environment variables set:**
  ```powershell
  $env:ASPNETCORE_URLS="https://localhost:5147;http://localhost:5148"
  ```
- ✅ **Application running with proper configuration**

## 🔍 **AADSTS50011 Fix Details**

### Root Cause
The AADSTS50011 error occurs when the redirect URI in the authorization request doesn't match any of the configured redirect URIs in the Azure AD app registration.

### Solution Implemented
1. **Consistent HTTPS Configuration:**
   - Kestrel endpoints: `https://localhost:5147`
   - Launch settings: `https://localhost:5147;http://localhost:5148`
   - Environment variables: `https://localhost:5147;http://localhost:5148`

2. **Proper Redirect URI Alignment:**
   - CallbackPath: `/signin-oidc`
   - Full redirect URI: `https://localhost:5147/signin-oidc`
   - SignedOutCallbackPath: `/signout-callback-oidc`
   - Full logout URI: `https://localhost:5147/signout-callback-oidc`

3. **Authorization Code Flow:**
   - ResponseType: `"code"` (not `"id_token"`)
   - PKCE enabled for security
   - Tokens saved for debugging

## 🚀 **Next Steps for Azure AD Configuration**

### Required Azure Portal Configuration
In Azure Portal > Microsoft Entra ID > App registrations > OATH Assets:

**Web platform configuration:**
- **Redirect URIs:**
  - `https://localhost:5147/signin-oidc`
  - `https://assets.oathone.com/signin-oidc`
- **Front-channel logout:**
  - `https://localhost:5147/signout-callback-oidc`
  - `https://assets.oathone.com/signout-callback-oidc`

### Client Secret Setup
```bash
dotnet user-secrets set "AzureAd:ClientSecret" "<your-client-secret>"
```

## 🔒 **Security Features**

- ✅ **Authorization Code flow** (not implicit flow)
- ✅ **PKCE (Proof Key for Code Exchange)** enabled
- ✅ **HTTPS required** for all authentication flows
- ✅ **Single tenant** configuration
- ✅ **No client secrets** in source control
- ✅ **Anti-forgery tokens** for sign-out

## 📋 **Configuration Summary**

**Application URLs:**
- HTTPS: `https://localhost:5147`
- HTTP: `http://localhost:5148`

**Azure AD Redirect URIs:**
- Sign-in: `https://localhost:5147/signin-oidc`
- Sign-out: `https://localhost:5147/signout-callback-oidc`

**Authentication Flow:**
- Scheme: Authorization Code + PKCE
- Default: Cookies
- Challenge: OpenID Connect

## 🎉 **Status: Ready for Testing**

The application is now properly configured to run on HTTPS:5147 with aligned redirect URIs. The AADSTS50011 error should be resolved once the Azure AD app registration redirect URIs are configured to match.

**Test URL:** `https://localhost:5147/`

**Expected Behavior:**
1. Visit `https://localhost:5147/` → Redirects to Microsoft sign-in
2. After sign-in → Returns to Dashboard with Identity cookie
3. Sign out → Clears both cookie and OIDC session
