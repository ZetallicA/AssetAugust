# Azure AD Client Secret Creation Instructions

## Step-by-Step Guide

### 1. Access Azure AD Admin Center
- Go to: https://entra.microsoft.com
- Sign in with your admin account

### 2. Navigate to App Registration
- Click on "App registrations" in the left sidebar
- Find and click on "OATH Assets"

### 3. Go to Certificates & Secrets
- In the left sub-menu, click "Certificates & secrets"
- You should see "No client secrets have been created for this application"

### 4. Create New Client Secret
- Click the "+ New client secret" button
- In the "Add a client secret" dialog:
  - **Description**: Enter "Asset Management App"
  - **Expires**: Select "12 months" (recommended)
- Click "Add"

### 5. Copy the Secret Value
- **IMPORTANT**: You will see the secret value only once!
- Copy the entire secret value (it looks like: `abc123def456ghi789...`)
- Store it securely - you won't be able to see it again

### 6. Update Your appsettings.json
- Open `AssetManagement.Web/appsettings.json`
- Find the "AzureAd" section
- Replace `"ClientSecret": "YOUR_CLIENT_SECRET_HERE"` with your actual secret
- Example:
```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "TenantId": "10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3",
  "ClientId": "1da7eb65-2637-4e54-aa79-b487969fa17e",
  "ClientSecret": "your-actual-secret-value-here",
  "CallbackPath": "/signin-oidc",
  "SignedOutCallbackPath": "/signout-callback-oidc"
}
```

### 7. Restart Your Application
After updating the configuration, restart your application for the changes to take effect.

## Security Notes
- Never commit the client secret to source control
- Consider using Azure Key Vault for production environments
- Rotate the secret periodically (before expiration)
- The secret shown in your current appsettings.json is likely invalid or expired

