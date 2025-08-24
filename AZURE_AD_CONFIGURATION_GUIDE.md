# 🔧 Azure AD Configuration Guide

## 🚨 **Current Issue: AADSTS700054**

The error `AADSTS700054: response_type 'id_token' is not enabled for the application` indicates that Azure AD is trying to use `id_token` response type instead of `code` (Authorization Code flow).

## 🔧 **Azure AD App Registration Configuration**

### **1. Authentication Settings**

**Navigate to:** Azure Portal → App Registrations → Your App → Authentication

**Configure these settings:**

#### **Platform Configuration**
- **Platform type:** Web
- **Redirect URIs:** `https://localhost:5147/signin-oidc`
- **Front-channel logout URL:** `https://localhost:5147/signout-callback-oidc`

#### **Implicit grant and hybrid flows**
- **❌ Access tokens:** Unchecked
- **❌ ID tokens:** Unchecked
- **✅ Authorization Code flow:** This is what we want

#### **Advanced settings**
- **Logout URL:** `https://localhost:5147/Account/SignedOut`
- **Front-channel logout URL:** `https://localhost:5147/signout-callback-oidc`

### **2. API Permissions**

**Navigate to:** App Registrations → Your App → API permissions

**Add these permissions:**
- **Microsoft Graph**
  - ✅ **User.Read** (Delegated)
  - ✅ **email** (Delegated) 
  - ✅ **profile** (Delegated)
  - ✅ **openid** (Delegated)

**Important:** Click **"Grant admin consent"** for your organization.

### **3. Token Configuration (Optional Claims)**

**Navigate to:** App Registrations → Your App → Token configuration

**Add these optional claims to ID tokens:**
- ✅ **email**
- ✅ **upn** (User Principal Name)
- ✅ **name**
- ✅ **given_name**
- ✅ **family_name**
- ✅ **oid** (Object ID)
- ✅ **tid** (Tenant ID)

### **4. Manifest Configuration**

**Navigate to:** App Registrations → Your App → Manifest

**Verify these settings in the manifest:**
```json
{
  "oauth2AllowImplicitFlow": false,
  "oauth2AllowIdTokenImplicitFlow": false,
  "oauth2RequirePostResponse": false,
  "oauth2AllowUrlPathMatching": false,
  "oauth2RequirePostResponse": false
}
```

## 🔧 **Application Configuration**

### **appsettings.Development.json**
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
    "ClientId": "1da7eb65-2637-4e54-aa79-b487969fa17e",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "PostLogoutRedirectUri": "https://localhost:5147/Account/SignedOut"
  }
}
```

### **Program.cs Configuration**
```csharp
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.ResponseType = "code";  // ✅ Explicitly set Authorization Code flow
    options.UsePkce = true;        // ✅ Enable PKCE for security
    options.SaveTokens = true;
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("User.Read");
});
```

## 🧪 **Testing Steps**

### **1. Verify Azure AD Configuration**
1. Check that **Implicit grant** is **disabled** (no checkboxes checked)
2. Verify **Redirect URIs** match exactly: `https://localhost:5147/signin-oidc`
3. Ensure **API permissions** are granted with admin consent

### **2. Test Authentication Flow**
1. **Clear browser cache** completely
2. **Visit:** `https://localhost:5147/`
3. **Expected:** Redirect to Microsoft sign-in
4. **Sign in** with Azure AD account
5. **Expected:** Redirect back to Dashboard (no errors)

### **3. Check Network Tab**
In browser Developer Tools → Network tab, look for:
- **Authorization request:** Should show `response_type=code`
- **Token request:** Should be a POST to `/token` endpoint
- **No errors:** Should not see `id_token` in any requests

## 🚨 **Common Issues & Solutions**

### **Issue 1: Still getting AADSTS700054**
**Solution:** 
1. Double-check that **Implicit grant** is completely disabled in Azure AD
2. Verify the app registration is configured for **Web** platform (not SPA)
3. Ensure **Redirect URIs** are exactly correct

### **Issue 2: Redirect URI mismatch**
**Solution:**
- Azure AD: `https://localhost:5147/signin-oidc`
- App config: `CallbackPath = "/signin-oidc"`

### **Issue 3: Missing permissions**
**Solution:**
- Add required API permissions
- Grant admin consent
- Wait 5-10 minutes for changes to propagate

### **Issue 4: Cached authentication**
**Solution:**
- Clear browser cache completely
- Use incognito/private mode
- Clear Azure AD session cookies

## 📞 **Verification Checklist**

- ✅ **Implicit grant disabled** in Azure AD
- ✅ **Redirect URIs configured** correctly
- ✅ **API permissions granted** with admin consent
- ✅ **ResponseType = "code"** in application code
- ✅ **UsePkce = true** in application code
- ✅ **Client secret configured** in user secrets
- ✅ **HTTPS enabled** for localhost

## 🎯 **Expected Result**

After proper configuration:
1. **Authorization request:** `response_type=code&code_challenge=...`
2. **Successful authentication:** No AADSTS700054 error
3. **Token exchange:** POST request to `/token` endpoint
4. **User authenticated:** Redirected to Dashboard successfully

**Test URL:** `https://localhost:5147/`
