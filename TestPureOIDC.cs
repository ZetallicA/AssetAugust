using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    // Basic OIDC Configuration
    options.Authority = "https://login.microsoftonline.com/10a3e7df-a6b1-4ea4-997d-cebfd54c2fa3/v2.0";
    options.ClientId = "febf3ebc-aed1-4980-bf45-cad3e96cd763";
    
    // Essential for pure OIDC authentication without client secrets
    options.ResponseType = "code";
    options.UsePkce = true;
    options.SaveTokens = false; // Don't save tokens since we're not calling APIs
    
    // Only request OIDC scopes - no API access
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    
    // Essential: Do not use client secret for public client
    options.ClientSecret = null;
    
    // Callback paths
    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";
    
    // Token validation parameters
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromMinutes(5)
    };
    
    // Events to ensure pure public client
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            // Explicitly remove client_secret parameter to force PKCE
            context.ProtocolMessage.Parameters.Remove("client_secret");
            return Task.CompletedTask;
        },
        
        OnAuthorizationCodeReceived = context =>
        {
            // Log the authorization code flow
            Console.WriteLine($"Authorization code received: {context.ProtocolMessage.Code?.Substring(0, 20)}...");
            return Task.CompletedTask;
        },
        
        OnTokenValidated = context =>
        {
            // Log successful authentication
            var userIdentity = context.Principal?.Identity;
            Console.WriteLine($"User authenticated successfully: {userIdentity?.Name}");
            return Task.CompletedTask;
        },
        
        OnAuthenticationFailed = context =>
        {
            // Log authentication failures
            Console.WriteLine($"Authentication failed: {context.Exception?.Message}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Simple authenticated endpoint
app.MapGet("/", (HttpContext context) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var claims = context.User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
        return Results.Json(new 
        { 
            Message = "Pure OIDC Authentication Successful!",
            User = context.User.Identity.Name,
            Email = context.User.FindFirst("email")?.Value,
            Claims = claims
        });
    }
    else
    {
        return Results.Challenge(properties: new AuthenticationProperties { RedirectUri = "/" });
    }
}).RequireAuthorization();

// Sign out endpoint
app.MapGet("/signout", () => Results.SignOut(
    new AuthenticationProperties { RedirectUri = "/" },
    CookieAuthenticationDefaults.AuthenticationScheme,
    OpenIdConnectDefaults.AuthenticationScheme));

Console.WriteLine("Pure OIDC Test App starting...");
Console.WriteLine("Navigate to: http://localhost:5555");

app.Run("http://localhost:5555");