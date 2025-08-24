# 🔄 Redirect Loop Troubleshooting Guide

## 🎯 **Current Status**
The application is running with the latest fixes. If you're still experiencing a redirect loop, follow this troubleshooting guide.

## 🔍 **Step-by-Step Diagnosis**

### **1. Test the Application**
**URL:** `https://localhost:5147/`

**Expected Flow:**
1. Visit URL → Redirected to Microsoft sign-in
2. Sign in with Azure AD → Authentication succeeds
3. Redirected back to Dashboard → **No more loops**

### **2. Check Browser Developer Tools**
1. Open Developer Tools (F12)
2. Go to **Network** tab
3. Clear the network log
4. Visit `https://localhost:5147/`
5. Look for:
   - ✅ **200 OK** responses (success)
   - ❌ **302 Redirect** loops (problem)
   - ❌ **500 Error** responses (server error)

### **3. Check Application Logs**
Look for these key log messages:

**✅ Good Signs:**
```
[INF] Successfully processed Azure AD user: rabi@oathone.com
[INF] AuthenticationScheme: Cookies signed in.
[INF] Request finished HTTP/2 GET https://localhost:5147/ - 200 0 text/html
```

**❌ Problem Signs:**
```
[INF] AuthenticationScheme: Identity.External signed in.
[INF] Authorization failed. These requirements were not met: DenyAnonymousAuthorizationRequirement
[INF] AuthenticationScheme: OpenIdConnect was challenged.
```

## 🛠️ **Quick Fixes to Try**

### **Fix 1: Clear Browser Cache**
1. Open Developer Tools (F12)
2. Right-click the refresh button
3. Select "Empty Cache and Hard Reload"
4. Try again

### **Fix 2: Use Incognito/Private Mode**
1. Open a new incognito/private browser window
2. Navigate to `https://localhost:5147/`
3. Sign in with Azure AD

### **Fix 3: Check Azure AD Configuration**
Verify these settings in Azure Portal:

**App Registration → Authentication:**
- ✅ **Redirect URIs:** `https://localhost:5147/signin-oidc`
- ✅ **Front-channel logout URL:** `https://localhost:5147/signout-callback-oidc`
- ✅ **Supported account types:** Accounts in this organizational directory only

**App Registration → API permissions:**
- ✅ **Microsoft Graph → User.Read** (with admin consent)
- ✅ **Microsoft Graph → email** (with admin consent)
- ✅ **Microsoft Graph → profile** (with admin consent)
- ✅ **Microsoft Graph → openid** (with admin consent)

**App Registration → Token configuration:**
- ✅ **ID tokens → email** (optional)
- ✅ **ID tokens → upn** (optional)
- ✅ **ID tokens → name** (optional)
- ✅ **ID tokens → given_name** (optional)
- ✅ **ID tokens → family_name** (optional)
- ✅ **ID tokens → oid** (optional)
- ✅ **ID tokens → tid** (optional)

### **Fix 4: Check User Secrets**
Verify the client secret is set correctly:

```powershell
dotnet user-secrets list
```

Should show:
```
AzureAd:ClientSecret = [your-secret-here]
```

### **Fix 5: Restart the Application**
If the application is running, restart it:

```powershell
# Stop the application
taskkill /F /IM dotnet.exe

# Rebuild and run
dotnet build
dotnet run
```

## 🔧 **Advanced Troubleshooting**

### **If Still Looping - Check These Files:**

**1. Program.cs Configuration:**
```csharp
// Should be:
options.DefaultScheme = "Cookies";
options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
.AddCookie("Cookies")
options.SignInScheme = "Cookies";
```

**2. appsettings.Development.json:**
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

**3. launchSettings.json:**
```json
{
  "applicationUrl": "https://localhost:5147;http://localhost:5148"
}
```

### **Debug Mode - Enable Detailed Logging**
Add this to `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore.Authentication": "Debug",
      "Microsoft.AspNetCore.Authorization": "Debug"
    }
  }
}
```

## 🚨 **Common Issues & Solutions**

### **Issue 1: "AuthenticationScheme: Identity.External signed in"**
**Solution:** The authentication scheme is wrong. Check that `options.SignInScheme = "Cookies"` is set.

### **Issue 2: "Authorization failed. DenyAnonymousAuthorizationRequirement"**
**Solution:** The user is not being recognized as authenticated. Check the authentication scheme configuration.

### **Issue 3: "AADSTS50011: The reply URL specified in the request does not match"**
**Solution:** The redirect URI in Azure AD doesn't match. Update it to `https://localhost:5147/signin-oidc`.

### **Issue 4: "TaskCanceledException" during authentication**
**Solution:** Network timeout. This should be fixed with `GetClaimsFromUserInfoEndpoint = false`.

### **Issue 5: "User email not found in Azure AD claims"**
**Solution:** Azure AD is not sending email claims. Add optional claims in Azure Portal.

## 📞 **If All Else Fails**

### **Complete Reset Process:**
1. **Stop the application:**
   ```powershell
   taskkill /F /IM dotnet.exe
   ```

2. **Clear browser cache completely**

3. **Verify Azure AD configuration**

4. **Rebuild and restart:**
   ```powershell
   dotnet clean
   dotnet build
   dotnet run
   ```

5. **Test in incognito mode**

### **Contact Information:**
If you're still experiencing issues, please provide:
- Screenshots of the browser network tab
- Application logs showing the authentication flow
- Any error messages displayed

## 🎯 **Expected Final Result**

When working correctly, you should see:
1. ✅ **Single redirect** to Microsoft sign-in
2. ✅ **Successful authentication** with Azure AD
3. ✅ **Direct navigation** to Dashboard
4. ✅ **No more redirects** or loops
5. ✅ **User properly authenticated** and authorized

**Test URL:** `https://localhost:5147/`
