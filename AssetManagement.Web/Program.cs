using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Data;
using AssetManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
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
.AddUserManager<UserManager<ApplicationUser>>();

// Authentication with Microsoft.Identity.Web (Authorization Code flow)
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.ResponseType = "code";
    options.UsePkce = true;
    options.SaveTokens = true;
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("User.Read");
    
    // Redirect URIs are configured in appsettings.Development.json
    
    // Add event handler to process user after successful authentication
    options.Events = new OpenIdConnectEvents
    {
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
                    // Don't fail the authentication, just log the warning
                    return;
                }
                
                // Log successful processing
                logger.LogInformation("Successfully processed Azure AD user: {Email}", result.User?.Email);
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Error in OnTokenValidated event");
                // Don't fail the authentication, just log the error
            }
        },
        
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "OpenID Connect authentication failed");
            return Task.CompletedTask;
        },
        
        OnRedirectToIdentityProvider = context =>
        {
            // Force HTTPS redirect URI for Cloudflare
            var redirectUri = context.ProtocolMessage.RedirectUri;
            if (redirectUri != null && redirectUri.StartsWith("http://"))
            {
                context.ProtocolMessage.RedirectUri = redirectUri.Replace("http://", "https://");
            }
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

builder.Services.AddRazorPages().AddMicrosoftIdentityUI();

// Optional: tune the app cookie (works with MIW's cookie handler)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/SignIn";
    options.LogoutPath = "/Account/SignOut";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
});

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Register services
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IAssetSearchService, AssetSearchService>();
builder.Services.AddScoped<AssetManagement.Infrastructure.Services.IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<AssetLifecycleService>();
builder.Services.AddScoped<TransferService>();
builder.Services.AddScoped<SalvageService>();

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
