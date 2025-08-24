# Cloudflare Public Hostname Setup for Asset Management

## Overview
This guide explains how to run Asset Management with Cloudflare Public Hostname for external access (no tunnel required).

## Current Status
✅ **Application**: Running on `0.0.0.0:5147` (HTTPS) and `0.0.0.0:5148` (HTTP)  
❌ **Cloudflare**: Still pointing to `localhost:5147` (needs update)

## Cloudflare Dashboard Configuration

### Update Your Public Hostname Settings

1. **Go to Cloudflare Dashboard** → **OATH One** → **Public hostnames**
2. **Edit the `assets.oathone.com` hostname**
3. **Update the URL field**:
   - **Current**: `localhost:5147`
   - **Change to**: `0.0.0.0:5147` or your server's IP address

### Recommended Settings

**Service Configuration:**
- **Type**: HTTPS
- **URL**: `0.0.0.0:5147` (or your server IP like `192.168.8.229:5147`)

**TLS Settings:**
- **No TLS Verify**: ON ✅
- **TLS Timeout**: 10 seconds
- **HTTP2 connection**: OFF

**HTTP Settings:**
- **Disable Chunked Encoding**: ON ✅
- **HTTP Host Header**: Leave as "Null"

**Connection Settings:**
- **Connect Timeout**: 30 seconds
- **No Happy Eyeballs**: ON ✅
- **Keep Alive Connections**: 100
- **TCP Keep Alive Interval**: 30 seconds

## Application Configuration

### Current Application Settings
Your application is correctly configured to bind to all interfaces:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http":  { "Url": "http://0.0.0.0:5148" },
      "Https": { "Url": "https://0.0.0.0:5147" }
    }
  }
}
```

### Running the Application
```powershell
# Option 1: Using the script
.\start-for-cloudflare.ps1

# Option 2: Manual command
cd AssetManagement.Web
dotnet run --launch-profile cloudflare
```

## Access URLs

### Local Development
- **HTTPS**: `https://localhost:5147`
- **HTTP**: `http://localhost:5148`

### External Access (via Cloudflare)
- **Public URL**: `https://assets.oathone.com`

## Azure AD Configuration

### Required Redirect URIs
In Azure Portal → App registrations → Asset Management → Authentication:

**Redirect URIs:**
- `https://assets.oathone.com/signin-oidc`
- `https://assets.oathone.com/signout-callback-oidc`
- `https://localhost:5147/signin-oidc` (for local development)
- `https://localhost:5147/signout-callback-oidc` (for local development)

**Front-channel logout URLs:**
- `https://assets.oathone.com/`
- `https://localhost:5147/`

## Troubleshooting

### 502 Bad Gateway Error
**Cause**: Cloudflare can't reach your application  
**Solution**: Update Cloudflare URL from `localhost:5147` to `0.0.0.0:5147`

### Connection Issues
1. **Verify application is running**:
   ```powershell
   netstat -an | findstr :5147
   ```

2. **Check application logs**:
   - Look for "Now listening on: https://0.0.0.0:5147"

3. **Test local access**:
   - Try `https://localhost:5147` first
   - Then try `https://assets.oathone.com`

### Azure AD Authentication Issues
- Ensure all redirect URIs are added in Azure Portal
- Check for typos in domain names
- Verify HTTPS is working

## Security Considerations

### Network Access
- Application binds to `0.0.0.0` (all interfaces)
- Cloudflare provides SSL termination and DDoS protection
- Consider firewall rules to restrict direct access

### Authentication
- Azure AD handles all authentication
- No local user accounts needed
- Session management through cookies

## Quick Fix Steps

1. **Update Cloudflare URL**:
   - Go to Cloudflare Dashboard
   - Edit `assets.oathone.com` hostname
   - Change URL from `localhost:5147` to `0.0.0.0:5147`
   - Save changes

2. **Restart Application** (if needed):
   ```powershell
   taskkill /F /IM dotnet.exe
   .\start-for-cloudflare.ps1
   ```

3. **Test Access**:
   - Local: `https://localhost:5147`
   - External: `https://assets.oathone.com`

## Benefits

- **No Tunnel Required**: Uses Cloudflare's public hostname feature
- **SSL Termination**: Automatic HTTPS
- **DDoS Protection**: Cloudflare protection
- **Simple Setup**: Just update the URL in Cloudflare dashboard
- **Local Development**: Still works locally
- **Azure AD Integration**: Seamless authentication
