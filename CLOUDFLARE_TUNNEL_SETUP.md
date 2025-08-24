# Cloudflare Tunnel Setup for Asset Management

This guide will help you set up Cloudflare Tunnel to securely expose your Asset Management application to the internet.

## Prerequisites

1. **Cloudflare Account**: You need a Cloudflare account with a domain
2. **cloudflared**: Install the Cloudflare Tunnel client
3. **Asset Management Application**: Running on your local machine

## Step 1: Install cloudflared

### Windows (PowerShell)
```powershell
# Download and install cloudflared
Invoke-WebRequest -Uri "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe" -OutFile "cloudflared.exe"
```

### Or download from: https://github.com/cloudflare/cloudflared/releases

## Step 2: Authenticate with Cloudflare

```bash
cloudflared tunnel login
```

This will:
- Open your browser to authenticate with Cloudflare
- Download a credentials file to your machine
- Note the path to the credentials file

## Step 3: Create a Tunnel

```bash
cloudflared tunnel create asset-management
```

This will create a tunnel and give you a tunnel ID. Copy this ID.

## Step 4: Configure the Tunnel

1. **Update the configuration file**:
   - Open `cloudflare-tunnel.yml`
   - Replace `your-tunnel-id-here` with your actual tunnel ID
   - Replace `yourdomain.com` with your actual domain
   - Update the `credentials-file` path to point to your credentials file

2. **Example configuration**:
```yaml
tunnel: abc12345-6789-def0-1234-567890abcdef
credentials-file: C:\Users\YourUsername\.cloudflared\abc12345-6789-def0-1234-567890abcdef.json

ingress:
  - hostname: assetmanagement.yourdomain.com
    service: http://localhost:5147
    originRequest:
      noTLSVerify: true
      connectTimeout: 30s
      readTimeout: 30s
      writeTimeout: 30s
  
  - service: http_status:404
```

## Step 5: Start the Asset Management Application

### Option A: Use the PowerShell Script
```powershell
.\start-for-cloudflare.ps1
```

### Option B: Manual Start
```bash
dotnet run --project AssetManagement.Web --launch-profile cloudflare
```

The application will start and be accessible on:
- **HTTP**: http://0.0.0.0:5147
- **For Cloudflare Tunnel**: http://localhost:5147

## Step 6: Start the Cloudflare Tunnel

```bash
cloudflared tunnel --config cloudflare-tunnel.yml run
```

## Step 7: Configure DNS

1. Go to your Cloudflare dashboard
2. Navigate to your domain's DNS settings
3. Add a CNAME record:
   - **Name**: `assetmanagement` (or your preferred subdomain)
   - **Target**: `{your-tunnel-id}.cfargotunnel.com`
   - **Proxy status**: Proxied (orange cloud)

## Step 8: Test the Setup

1. **Local Access**: http://localhost:5147
2. **Remote Access**: https://assetmanagement.yourdomain.com

## Security Considerations

### For Production Use:

1. **Update Azure AD Configuration**:
   - Add your domain to the Azure AD app registration
   - Update redirect URIs to include your domain

2. **Environment Variables**:
   - Set `ASPNETCORE_ENVIRONMENT` to `Production`
   - Configure proper connection strings
   - Set up proper logging

3. **Firewall**:
   - Ensure port 5147 is not exposed directly to the internet
   - Only allow Cloudflare Tunnel traffic

## Troubleshooting

### Common Issues:

1. **"Connection refused"**:
   - Ensure the application is running on port 5147
   - Check if the application is bound to 0.0.0.0

2. **"Tunnel not found"**:
   - Verify the tunnel ID in your configuration
   - Ensure the credentials file path is correct

3. **"Hostname not found"**:
   - Check your DNS configuration
   - Ensure the CNAME record is properly set

### Logs:

- **Application Logs**: Check the console output when starting the app
- **Tunnel Logs**: Check cloudflared output for connection issues

## Advanced Configuration

### Custom Domain with SSL:
The tunnel automatically provides SSL certificates for your domain.

### Multiple Services:
You can add multiple services to the same tunnel:

```yaml
ingress:
  - hostname: assetmanagement.yourdomain.com
    service: http://localhost:5147
  - hostname: api.yourdomain.com
    service: http://localhost:5000
  - service: http_status:404
```

### Load Balancing:
For high availability, you can run multiple instances and use Cloudflare's load balancing.

## Support

If you encounter issues:
1. Check the application logs
2. Check the tunnel logs
3. Verify your configuration
4. Ensure all prerequisites are met

