# Cloudflare Tunnel Setup for Asset Management

## Overview
This guide explains how to set up Cloudflare Tunnel to connect to your local Asset Management application via HTTP, avoiding SSL certificate issues.

## Configuration Files

### 1. Cloudflare Tunnel Config (`cloudflare-tunnel.yml`)

```yaml
# Cloudflare Tunnel Configuration for Asset Management
# This tunnel connects to the local server via HTTP to avoid SSL certificate issues

tunnel: 07b807ec-612a-4885
credentials-file: /etc/cloudflared/07b807ec-612a-4885.json

ingress:
  # Asset Management Web Application - HTTP connection
  - hostname: assets.oathone.com
    service: http://192.168.8.199:5147
    originRequest:
      noTLSVerify: true
      connectTimeout: 30s
      readTimeout: 30s
      writeTimeout: 30s
      disableChunkedEncoding: true
  
  # Health check endpoint
  - hostname: assets.oathone.com
    path: /health
    service: http://192.168.8.199:5147/health
    originRequest:
      noTLSVerify: true
      disableChunkedEncoding: true
  
  # Catch-all rule (must be last)
  - service: http_status:404
```

### 2. Application Configuration

Your application is configured to bind to your specific IP address:

**`appsettings.Development.json`:**
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http":  { "Url": "http://192.168.8.199:5148" },
      "Https": { "Url": "https://192.168.8.199:5147" }
    }
  }
}
```

**`launchSettings.json`:**
```json
{
  "cloudflare": {
    "applicationUrl": "https://192.168.8.199:5147;http://192.168.8.199:5148"
  }
}
```

## Setup Steps

### 1. Start Your Application

```powershell
# Option 1: Using the script
.\start-for-cloudflare.ps1

# Option 2: Manual command
cd AssetManagement.Web
dotnet run --launch-profile cloudflare
```

### 2. Start Cloudflare Tunnel

```bash
# Start the tunnel with your config
cloudflared tunnel --config cloudflare-tunnel.yml run

# Or if you want to run it as a service
cloudflared tunnel --config cloudflare-tunnel.yml service install
```

### 3. Verify Tunnel Status

```bash
# Check tunnel status
cloudflared tunnel list

# Check tunnel info
cloudflared tunnel info 07b807ec-612a-4885
```

## Key Configuration Points

### Why HTTP Instead of HTTPS?

1. **Self-Signed Certificate Issue**: Your local server uses a self-signed SSL certificate
2. **Tunnel Security**: Cloudflare Tunnel provides end-to-end encryption regardless
3. **Simplified Setup**: No need to manage certificates locally

### Important Settings

- **`service: http://192.168.8.199:5147`**: Connects via HTTP to your local server
- **`noTLSVerify: true`**: Disables TLS verification (not needed for HTTP)
- **`disableChunkedEncoding: true`**: Helps with compatibility
- **`connectTimeout: 30s`**: Gives enough time for connection

## Access URLs

### Local Development
- **HTTPS**: `https://192.168.8.199:5147`
- **HTTP**: `http://192.168.8.199:5148`

### External Access (via Cloudflare Tunnel)
- **Public URL**: `https://assets.oathone.com`

## Troubleshooting

### Common Issues

1. **"Unable to reach the origin service"**
   - Verify your application is running on `192.168.8.199:5147`
   - Check firewall settings
   - Ensure tunnel is running

2. **Connection Timeout**
   - Increase `connectTimeout` in the config
   - Check network connectivity

3. **Tunnel Not Starting**
   - Verify credentials file path: `/etc/cloudflared/07b807ec-612a-4885.json`
   - Check tunnel ID matches your actual tunnel

### Debugging Commands

```bash
# Test local connectivity
curl http://192.168.8.199:5147

# Check tunnel logs
cloudflared tunnel --config cloudflare-tunnel.yml run --loglevel debug

# Verify tunnel is running
cloudflared tunnel list
```

## Security Considerations

### Network Security
- **Tunnel Encryption**: All traffic is encrypted end-to-end
- **No Public Ports**: No need to expose ports to the internet
- **Cloudflare Protection**: DDoS protection and SSL termination

### Application Security
- **Azure AD Authentication**: All authentication handled by Azure AD
- **HTTPS for Users**: Users always access via HTTPS
- **Local HTTP**: Only the tunnel-to-application connection uses HTTP

## Azure AD Configuration

### Required Redirect URIs
In Azure Portal → App registrations → Asset Management → Authentication:

**Redirect URIs:**
- `https://assets.oathone.com/signin-oidc`
- `https://assets.oathone.com/signout-callback-oidc`
- `https://192.168.8.199:5147/signin-oidc` (for local development)
- `https://192.168.8.199:5147/signout-callback-oidc` (for local development)

**Front-channel logout URLs:**
- `https://assets.oathone.com/`
- `https://192.168.8.199:5147/`

## Benefits of This Setup

1. **No SSL Certificate Management**: No need to manage local certificates
2. **Secure External Access**: All external traffic is encrypted
3. **Simple Configuration**: Minimal setup required
4. **Reliable Connection**: HTTP is more reliable than HTTPS with self-signed certs
5. **Cloudflare Features**: Automatic SSL, DDoS protection, caching

## Development Workflow

1. **Start Application**: `dotnet run --launch-profile cloudflare`
2. **Start Tunnel**: `cloudflared tunnel --config cloudflare-tunnel.yml run`
3. **Access**: `https://assets.oathone.com`
4. **Stop**: Ctrl+C on both processes

This setup provides secure external access to your Asset Management application without the complexity of managing SSL certificates locally.

