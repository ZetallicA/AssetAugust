# Client Secret Setup Instructions - URGENT FIX for AADSTS7000218

## 🚨 **Current Error: AADSTS7000218**
The application is missing the client secret configuration. Follow these steps to fix it:

## Step 1: Create Client Secret in Azure Portal

1. **Go to Azure Portal**: https://entra.microsoft.com
2. **Navigate to**: App registrations > OATH Assets
3. **Click**: "Certificates & secrets" in the left menu
4. **Click**: "+ New client secret"
5. **Fill in**:
   - Description: "Asset Management App"
   - Expires: 12 months (recommended)
6. **Click**: "Add"
7. **⚠️ COPY THE SECRET VALUE IMMEDIATELY** - you won't see it again!

## Step 2: Set Client Secret in User Secrets (Development)

Open PowerShell in the `AssetManagement.Web` directory and run:

```powershell
# Navigate to the web project
cd C:\temp\AssetManagement\AssetManagement.Web

# Set the client secret (replace YOUR_SECRET_HERE with the actual secret)
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET_HERE"

# Verify it was set
dotnet user-secrets list
```

## Step 3: Restart the Application

After setting the client secret:

```powershell
# Stop any running dotnet processes
taskkill /F /IM dotnet.exe

# Set environment URLs
$env:ASPNETCORE_URLS="https://localhost:5147;http://localhost:5148"

# Run the application
dotnet run
```

## Step 4: Test the Sign-In

Visit: `https://localhost:5147/`

**Expected behavior:**
1. Redirects to Microsoft sign-in page
2. After successful sign-in, returns to the Dashboard
3. No more AADSTS7000218 errors

## 🔒 **Security Notes**

- ✅ Client secret is stored in user secrets (not in source code)
- ✅ User secrets are encrypted and stored locally
- ✅ Never commit client secrets to source control
- ✅ For production, use Azure Key Vault or environment variables

## 📋 **Required Azure AD Redirect URIs**

Make sure these are configured in Azure Portal > OATH Assets > Authentication:

**Web platform Redirect URIs:**
- `https://localhost:5147/signin-oidc`
- `https://assets.oathone.com/signin-oidc`

**Front-channel logout URIs:**
- `https://localhost:5147/signout-callback-oidc`
- `https://assets.oathone.com/signout-callback-oidc`

## 🎯 **Quick Fix Summary**

The error is caused by missing client secret. Once you:
1. Create the secret in Azure Portal
2. Set it in user secrets with the command above
3. Restart the application

The AADSTS7000218 error will be resolved and authentication will work properly.
