using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Data;
using AssetManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Entity Framework
builder.Services.AddDbContext<AssetManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Configure Identity for user management (without authentication)
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AssetManagementDbContext>()
.AddDefaultTokenProviders()
.AddSignInManager<SignInManager<ApplicationUser>>()
.AddUserManager<UserManager<ApplicationUser>>()
.AddClaimsPrincipalFactory<UserClaimsPrincipalFactory>();

// Authentication with pure OpenID Connect (PKCE without client secrets)
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        // Basic Azure AD configuration
        options.Authority = $"{builder.Configuration["AzureAd:Instance"]}{builder.Configuration["AzureAd:TenantId"]}/v2.0";
        options.ClientId = builder.Configuration["AzureAd:ClientId"];
        
        // Essential PKCE configuration for public client
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
        
        // Token validation
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
            
            OnTokenValidated = async context =>
            {
                try
                {
                    var authService = context.HttpContext.RequestServices.GetRequiredService<AssetManagement.Infrastructure.Services.IAuthenticationService>();
                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                    var userAgent = context.HttpContext.Request.Headers["User-Agent"].ToString();
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    
                    var result = await authService.ProcessAzureAdUserAsync(context.Principal!, ipAddress, userAgent);
                    
                    if (!result.IsSuccess)
                    {
                        logger.LogWarning("Authentication processing failed: {Error}", result.ErrorMessage);
                        return;
                    }
                    
                    logger.LogInformation("Successfully processed Azure AD user: {Email}", result.User?.Email);
                }
                catch (Exception ex)
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error in OnTokenValidated event");
                }
            },
            
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "OpenID Connect authentication failed");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("RequireManagerRole", policy =>
        policy.RequireRole("Admin", "Manager"));
    
    options.AddPolicy("RequireActiveUser", policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddRazorPages();

// Optional: tune the app cookie (works with MIW's cookie handler)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/SignIn";
    options.LogoutPath = "/Account/SignOut";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Allow HTTP in development
    options.SlidingExpiration = true;
});

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Register services
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IAssetSearchService, AssetSearchService>();
builder.Services.AddScoped<AssetManagement.Infrastructure.Services.IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<AssetManagement.Infrastructure.Services.IAuthorizationService, AssetManagement.Infrastructure.Services.AuthorizationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<AssetLifecycleService>();
builder.Services.AddScoped<TransferService>();
builder.Services.AddScoped<SalvageService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Disable HTTPS redirection for Cloudflare tunnel compatibility
// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Configure Health Check endpoint
app.MapHealthChecks("/health");

// Temporary debug endpoint
app.MapGet("/whoami", (HttpContext ctx) =>
{
    var auth = ctx.User?.Identity?.IsAuthenticated ?? false;
    var claims = auth ? ctx.User.Claims.Select(c => $"{c.Type}: {c.Value}") : [];
    return Results.Json(new { Authenticated = auth, Name = ctx.User?.Identity?.Name, Claims = claims });
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.MapRazorPages();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AssetManagementDbContext>();
    await context.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();
