# Azure AD Claims Fix Guide 🔧

## 🎯 **Problem: "User email not found in Azure AD claims"**

The error occurs because the Azure AD application is not configured to include the necessary claims (like email) in the tokens issued during authentication.

## ✅ **Solution: Configure Azure AD Token Claims**

### Step 1: Configure Token Claims in Azure Portal

1. **Go to Azure Portal**: https://entra.microsoft.com
2. **Navigate to**: App registrations > OATH Assets
3. **Click**: "Token configuration" in the left menu
4. **Click**: "+ Add optional claim"
5. **Select**: "ID" token
6. **Add these claims**:
   - `email` - User's email address
   - `upn` - User Principal Name
   - `name` - Display name
   - `given_name` - First name
   - `family_name` - Last name
   - `oid` - Object ID
   - `tid` - Tenant ID
7. **Click**: "Add"

### Step 2: Configure API Permissions

1. **In the same app registration**, click "API permissions"
2. **Click**: "+ Add a permission"
3. **Select**: "Microsoft Graph"
4. **Select**: "Delegated permissions"
5. **Add these permissions**:
   - `User.Read` - Read user profile
   - `email` - Read user email
   - `profile` - Read user profile
   - `openid` - Sign in and read user profile
6. **Click**: "Add permissions"
7. **Click**: "Grant admin consent" (if you have admin rights)

### Step 3: Verify Redirect URIs

Based on your screenshot, the redirect URIs are correctly configured:
- ✅ `https://localhost:5147/signin-oidc`
- ✅ `https://localhost:5147/signout-callback-oidc`
- ✅ `https://assets.oathone.com/signin-oidc`
- ✅ `https://assets.oathone.com/signout-callback-oidc`

## 🔧 **Application Code Improvements**

### Enhanced Claim Extraction

The application code has been improved to:

1. **Log all available claims** for debugging
2. **Try multiple claim types** for email extraction:
   - `ClaimTypes.Email`
   - `preferred_username`
   - `upn`
   - `email`
   - `unique_name`

3. **Enhanced error handling** with detailed logging

### OpenID Connect Configuration

The application now requests the necessary scopes:
```csharp
options.Scope.Clear();
options.Scope.Add("openid");
options.Scope.Add("profile");
options.Scope.Add("email");
options.Scope.Add("User.Read");
```

## 🚀 **Testing Steps**

### 1. Configure Azure AD Claims
Follow the steps above to add the required claims to your Azure AD application.

### 2. Test the Application
1. **Visit**: `https://localhost:5147/`
2. **Sign in** with your Azure AD account
3. **Check logs** for available claims
4. **Verify** successful authentication

### 3. Check Application Logs
The application will now log all available claims, making it easier to debug:
```
Available claims: sub: xxx, name: John Doe, email: john@oathone.com, ...
```

## 🔍 **Troubleshooting**

### If Claims Are Still Missing

1. **Check API Permissions**: Ensure `User.Read` permission is granted
2. **Check Admin Consent**: Ensure admin consent is granted for the permissions
3. **Check Token Configuration**: Verify claims are added to ID tokens
4. **Check Application Manifest**: Verify the manifest includes the claims

### Common Issues

1. **"Insufficient privileges"**: Need admin consent for API permissions
2. **"Invalid scope"**: Ensure scopes are correctly configured
3. **"Claims not in token"**: Verify claims are added to ID token configuration

## 📋 **Configuration Summary**

### Azure AD App Registration
- ✅ **Redirect URIs**: Configured for localhost and production
- ✅ **Token Claims**: Need to add email, name, upn, etc.
- ✅ **API Permissions**: Need User.Read, email, profile, openid
- ✅ **Admin Consent**: Required for delegated permissions

### Application Configuration
- ✅ **OpenID Connect**: Configured with proper scopes
- ✅ **Claim Extraction**: Enhanced with multiple fallback options
- ✅ **Error Handling**: Improved with detailed logging
- ✅ **Debugging**: Logs all available claims

## 🎉 **Expected Result**

After completing these steps:
1. **Azure AD** will include email and other claims in tokens
2. **Application** will successfully extract user information
3. **Authentication** will complete without errors
4. **User** will be created/updated in local database
5. **User** will be redirected to dashboard

## 🔒 **Security Notes**

- **Client Secret**: Stored in user secrets (not in source control)
- **HTTPS Required**: All authentication flows use HTTPS
- **PKCE**: Enabled for enhanced security
- **Authorization Code Flow**: Used instead of implicit flow
- **Token Validation**: Properly configured with validation parameters
