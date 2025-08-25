using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using AssetManagement.Domain.Entities;
using System.Security.Claims;

namespace AssetManagement.Infrastructure.Services;

public class UserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    public UserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        
        // Add user roles to claims
        var roles = await UserManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        // Add additional user information to claims
        if (!string.IsNullOrEmpty(user.FirstName))
        {
            identity.AddClaim(new Claim("FirstName", user.FirstName));
        }
        
        if (!string.IsNullOrEmpty(user.LastName))
        {
            identity.AddClaim(new Claim("LastName", user.LastName));
        }
        
        if (!string.IsNullOrEmpty(user.Department))
        {
            identity.AddClaim(new Claim("Department", user.Department));
        }

        return identity;
    }
}
