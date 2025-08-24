# Microsoft Entra ID (Azure AD) Authentication Setup Guide

This guide provides step-by-step instructions for setting up Microsoft Entra ID (Azure AD) authentication in your Asset Management System.

## Prerequisites

- Azure subscription with Microsoft Entra ID (Azure AD) tenant
- .NET 9.0 application
- Visual Studio 2022 or later (recommended)

## Step 1: Azure AD App Registration

### 1.1 Create App Registration

1. Sign in to the [Azure Portal](https://portal.azure.com)
2. Navigate to **Microsoft Entra ID** > **App registrations**
3. Click **New registration**
4. Fill in the registration details:
   - **Name**: `Asset Management System`
   - **Supported account types**: `Accounts in this organizational directory only`
   - **Redirect URI**: 
     - Type: `Web`
     - URI: `https://localhost:7001/signin-oidc` (for development)
     - URI: `https://yourdomain.com/signin-oidc` (for production)

### 1.2 Configure Authentication

1. In your app registration, go to **Authentication**
2. Under **Platform configurations**, click **Add a platform** > **Web**
3. Add these redirect URIs:
   - `https://localhost:7001/signin-oidc`
   - `https://localhost:7001/signout-callback-oidc`
   - `https://yourdomain.com/signin-oidc` (production)
   - `https://yourdomain.com/signout-callback-oidc` (production)
4. Under **Implicit grant and hybrid flows**, enable:
   - ✅ **ID tokens**
5. Click **Save**

### 1.3 Create Client Secret

1. Go to **Certificates & secrets**
2. Click **New client secret**
3. Add a description (e.g., "Asset Management App Secret")
4. Choose expiration (recommend 12 months for production)
5. Click **Add**
6. **IMPORTANT**: Copy the secret value immediately - you won't be able to see it again!

### 1.4 Configure API Permissions

1. Go to **API permissions**
2. Click **Add a permission**
3. Select **Microsoft Graph**
4. Choose **Delegated permissions**
5. Add these permissions:
   - `User.Read` (read user profile)
   - `User.ReadBasic.All` (read basic user info)
   - `Directory.Read.All` (read directory data - optional)
6. Click **Add permissions**
7. Click **Grant admin consent** (if you have admin rights)

### 1.5 Get Application Details

1. Go to **Overview**
2. Copy these values:
   - **Application (client) ID**
   - **Directory (tenant) ID**
   - **Client Secret** (from step 1.3)

## Step 2: Update Configuration

### 2.1 Update appsettings.json

Replace the Azure AD configuration in your `appsettings.json` with your actual values:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id-here",
    "ClientId": "your-client-id-here",
    "ClientSecret": "your-client-secret-here",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

### 2.2 Environment-Specific Configuration

For production, use Azure Key Vault or environment variables:

```bash
# Environment variables
setx AzureAd__TenantId "your-tenant-id"
setx AzureAd__ClientId "your-client-id"
setx AzureAd__ClientSecret "your-client-secret"
```

## Step 3: Database Setup

### 3.1 Create Default Roles

Run these SQL commands to create default roles:

```sql
-- Create default roles
IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Admin')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) 
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());

IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Manager')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) 
    VALUES (NEWID(), 'Manager', 'MANAGER', NEWID());

IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'User')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) 
    VALUES (NEWID(), 'User', 'USER', NEWID());
```

### 3.2 Create Admin User

Create an admin user in the database seeder:

```csharp
// In DatabaseSeeder.cs
if (!await _userManager.Users.AnyAsync())
{
    var adminUser = new ApplicationUser
    {
        UserName = "admin@yourcompany.com",
        Email = "admin@yourcompany.com",
        FirstName = "System",
        LastName = "Administrator",
        EmailConfirmed = true,
        IsActive = true
    };

    var result = await _userManager.CreateAsync(adminUser, "TempPassword123!");
    if (result.Succeeded)
    {
        await _userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
```

## Step 4: Testing the Authentication

### 4.1 Local Testing

1. Run your application: `dotnet run`
2. Navigate to `https://localhost:7001`
3. Click **Sign In**
4. You should be redirected to Microsoft login
5. Sign in with your Azure AD credentials
6. You should be redirected back to your application

### 4.2 Verify User Creation

1. Check the database to see if the user was created
2. Verify the user has the correct roles assigned
3. Check the application logs for authentication events

## Step 5: Production Deployment

### 5.1 Update Redirect URIs

1. In Azure Portal, update the redirect URIs to your production domain
2. Remove localhost URIs for production
3. Update your `appsettings.json` with production values

### 5.2 Security Considerations

1. **Use Azure Key Vault** for storing secrets in production
2. **Enable HTTPS** on your application
3. **Configure proper CORS** if needed
4. **Set up monitoring** for authentication events
5. **Regular secret rotation** (every 12 months)

### 5.3 Environment Variables

For production, use environment variables:

```bash
# Production environment variables
AzureAd__Instance=https://login.microsoftonline.com/
AzureAd__TenantId=your-tenant-id
AzureAd__ClientId=your-client-id
AzureAd__ClientSecret=your-client-secret
AzureAd__CallbackPath=/signin-oidc
AzureAd__SignedOutCallbackPath=/signout-callback-oidc
```

## Troubleshooting

### Common Issues

#### 1. "AADSTS50011: The reply URL specified in the request does not match the reply URLs configured for the application"

**Solution**: 
- Check that the redirect URI in your app registration matches exactly
- Ensure no trailing slashes
- Verify HTTPS vs HTTP

#### 2. "AADSTS70002: The request body must contain the following parameter: 'client_secret'"

**Solution**:
- Verify your client secret is correct
- Check that the secret hasn't expired
- Ensure the secret is properly configured in appsettings.json

#### 3. "AADSTS65001: The user or administrator has not consented to use the application"

**Solution**:
- Grant admin consent in Azure Portal
- Go to API permissions > Grant admin consent
- Or have users consent individually

#### 4. User not created in local database

**Solution**:
- Check the authentication service logs
- Verify the user email is being extracted correctly
- Ensure the database connection is working

### Debugging Tips

1. **Enable detailed logging**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore.Authentication": "Debug"
    }
  }
}
```

2. **Check browser network tab** for redirect issues

3. **Verify claims** in the authentication callback

4. **Test with different browsers** to rule out browser-specific issues

## Security Best Practices

1. **Never commit secrets** to source control
2. **Use Azure Key Vault** for production secrets
3. **Enable MFA** for admin accounts
4. **Regular security audits** of your Azure AD configuration
5. **Monitor authentication logs** for suspicious activity
6. **Implement proper session management**
7. **Use HTTPS everywhere**

## Monitoring and Logging

### Azure AD Sign-in Logs

1. Go to **Microsoft Entra ID** > **Sign-in logs**
2. Filter by your application
3. Monitor for:
   - Failed sign-ins
   - Unusual locations
   - Multiple failed attempts

### Application Logs

The application logs authentication events to:
- Console (development)
- File logs (configured in Serilog)
- Database (if implemented)

## Support

For additional help:
- [Microsoft Entra ID Documentation](https://docs.microsoft.com/en-us/azure/active-directory/)
- [Microsoft Identity Web Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [OpenID Connect Documentation](https://openid.net/connect/)

## Next Steps

After successful authentication setup:

1. **Implement role-based authorization** for different features
2. **Add department-based access control**
3. **Set up user provisioning** workflows
4. **Implement audit logging** for sensitive operations
5. **Configure automatic user synchronization** with Azure AD
