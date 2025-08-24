# Troubleshooting Cloudflare Tunnel 502 Error

## Current Issue: 502 Bad Gateway

**Error**: `assets.oathone.com` shows "Bad gateway" (Error code 502)

**Root Cause**: Cloudflare Tunnel is not running or not properly connected to your local application.

## Quick Fix Steps:

### 1. ✅ Verify Application is Running
```powershell
# Check if application is running
tasklist | findstr AssetManagement

# Check if it's listening on the correct port
netstat -an | findstr :5147

# Test local connectivity
curl http://localhost:5147/health
```

**Expected Output**: 
- Process should be running
- Port 5147 should show `LISTENING`
- Health check should return `200 OK`

### 2. ✅ Install and Authenticate cloudflared
```powershell
# Download cloudflared
Invoke-WebRequest -Uri "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe" -OutFile "cloudflared.exe"

# Authenticate (this will open your browser)
.\cloudflared.exe tunnel login
```

### 3. ✅ Start the Tunnel
```powershell
# Use the setup script
.\setup-cloudflare-tunnel.ps1

# Or manually start the tunnel
.\cloudflared.exe tunnel --config cloudflare-tunnel.yml run
```

### 4. ✅ Verify Tunnel is Running
```powershell
# Check if cloudflared is running
tasklist | findstr cloudflared

# Should show cloudflared.exe process
```

## Common Issues and Solutions:

### Issue 1: "cloudflared not found"
**Solution**: Download cloudflared manually from https://github.com/cloudflare/cloudflared/releases

### Issue 2: "Authentication failed"
**Solution**: 
1. Run `.\cloudflared.exe tunnel login`
2. Complete the browser authentication
3. Check that credentials file exists: `C:\Users\%USERNAME%\.cloudflared\07b807ec-612a-4885-b614-47b7dc879034.json`

### Issue 3: "Connection refused"
**Solution**: 
1. Ensure application is running: `.\start-for-cloudflare.ps1`
2. Test local access: `http://localhost:5147`
3. Check firewall settings

### Issue 4: "Tunnel not found"
**Solution**: 
1. Verify tunnel ID in `cloudflare-tunnel.yml`
2. Ensure credentials file path is correct
3. Check Cloudflare dashboard for tunnel status

## Complete Setup Commands:

```powershell
# 1. Start the application
.\start-for-cloudflare.ps1

# 2. In a new terminal, run the setup script
.\setup-cloudflare-tunnel.ps1

# 3. Test the connection
curl https://assets.oathone.com/health
```

## Monitoring:

### Check Application Logs:
- Look at the console output when starting the application
- Check for any error messages

### Check Tunnel Logs:
- Look at cloudflared console output
- Check for connection errors or authentication issues

### Check Cloudflare Dashboard:
- Verify tunnel status is "Connected"
- Check for any error messages in the tunnel logs

## Expected Final State:

1. **Application**: Running on `http://localhost:5147`
2. **Tunnel**: Running and connected to Cloudflare
3. **Public URL**: `https://assets.oathone.com` accessible
4. **Health Check**: `https://assets.oathone.com/health` returns "Healthy"

## If Still Not Working:

1. **Restart Everything**:
   ```powershell
   # Stop all processes
   taskkill /F /IM AssetManagement.Web.exe
   taskkill /F /IM cloudflared.exe
   
   # Start fresh
   .\start-for-cloudflare.ps1
   .\setup-cloudflare-tunnel.ps1
   ```

2. **Check Firewall**: Ensure Windows Firewall allows cloudflared

3. **Check Antivirus**: Some antivirus software may block cloudflared

4. **Manual Tunnel Start**: Try starting the tunnel manually to see error messages:
   ```powershell
   .\cloudflared.exe tunnel --config cloudflare-tunnel.yml run
   ```

