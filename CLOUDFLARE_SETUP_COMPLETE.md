# Cloudflare Tunnel Setup for Asset Management

## Overview
This guide explains how to run Asset Management with Cloudflare Tunnel for external access.

## Configuration Changes Made

### 1. Application Binding
- **Updated `appsettings.Development.json`**: Changed Kestrel endpoints to bind to `0.0.0.0` instead of `localhost`
- **Updated `launchSettings.json`**: Added Cloudflare profile with proper URL configuration

### 2. Azure AD Configuration
- **Updated redirect URIs**: Changed post-logout redirect to `https://assets.oathone.com/`
- **Maintained localhost support**: Application still works locally for development

## Running the Application

### Option 1: Using PowerShell Script (Recommended)
```powershell
.\start-for-cloudflare.ps1
```

### Option 2: Manual Command
```powershell
cd AssetManagement.Web
dotnet run --launch-profile cloudflare
```

### Option 3: Environment Variables
```powershell
$env:ASPNETCORE_URLS = "https://0.0.0.0:5147;http://0.0.0.0:5148"
dotnet run
```

## Access URLs

### Local Development
- **HTTPS**: `https://localhost:5147`
- **HTTP**: `http://localhost:5148`

### External Access (via Cloudflare)
- **Public URL**: `https://assets.oathone.com`

## Azure AD Configuration Required

### In Azure Portal (Entra ID)
1. Go to **App registrations** → **Asset Management**
2. Navigate to **Authentication**
3. Add these redirect URIs:
   - `https://assets.oathone.com/signin-oidc`
   - `https://assets.oathone.com/signout-callback-oidc`
   - `https://localhost:5147/signin-oidc` (for local development)
   - `https://localhost:5147/signout-callback-oidc` (for local development)

4. Add these front-channel logout URLs:
   - `https://assets.oathone.com/`
   - `https://localhost:5147/`

## Cloudflare Tunnel Configuration

### Current Tunnel Config (`cloudflare-tunnel.yml`)
```yaml
tunnel: 07b807ec-612a-4885-b614-47b7dc879034
credentials-file: C:\Users\%USERNAME%\.cloudflared\07b807ec-612a-4885-b614-47b7dc879034.json

ingress:
  - hostname: assets.oathone.com
    service: http://localhost:5147
    originRequest:
      noTLSVerify: true
      connectTimeout: 30s
      readTimeout: 30s
      writeTimeout: 30s
  
  - hostname: assetmanagement.yourdomain.com
    service: http://localhost:5147/health
    originRequest:
      noTLSVerify: true
  
  - service: http_status:404
```

### Starting Cloudflare Tunnel
```powershell
# Start the tunnel
cloudflared.exe tunnel --config cloudflare-tunnel.yml run
```

## Security Considerations

### 1. Network Binding
- Application now binds to `0.0.0.0` (all interfaces)
- Only accessible through Cloudflare Tunnel
- Local firewall should block direct access to ports 5147/5148

### 2. HTTPS
- Cloudflare provides SSL termination
- Application uses HTTPS locally for development
- Azure AD requires HTTPS for authentication

### 3. Authentication
- Azure AD handles all authentication
- No local user accounts needed
- Session management through cookies

## Troubleshooting

### Common Issues

1. **Port Already in Use**
   ```powershell
   # Kill existing processes
   taskkill /F /IM dotnet.exe
   ```

2. **Azure AD Redirect URI Mismatch**
   - Ensure all redirect URIs are added in Azure Portal
   - Check for typos in domain names

3. **Cloudflare Tunnel Connection Issues**
   - Verify tunnel is running: `cloudflared.exe tunnel list`
   - Check tunnel logs for errors
   - Ensure local application is running on correct port

4. **Certificate Issues**
   ```powershell
   # Trust development certificate
   dotnet dev-certs https --trust
   ```

### Logs
- Application logs: Check console output
- Cloudflare logs: Check tunnel output
- Azure AD logs: Check Azure Portal → App registrations → Sign-in logs

## Development Workflow

1. **Start Application**: `.\start-for-cloudflare.ps1`
2. **Start Tunnel**: `cloudflared.exe tunnel --config cloudflare-tunnel.yml run`
3. **Access**: `https://assets.oathone.com`
4. **Stop**: Ctrl+C on both processes

## Benefits

- **Secure External Access**: All traffic goes through Cloudflare
- **SSL Termination**: Automatic HTTPS
- **DDoS Protection**: Cloudflare protection
- **Local Development**: Still works locally
- **Azure AD Integration**: Seamless authentication
