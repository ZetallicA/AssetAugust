using AssetManagement.Domain.Constants;
using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Data;
using AssetManagement.Infrastructure.Services;
using AssetManagement.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IAuthorizationService = AssetManagement.Infrastructure.Services.IAuthorizationService;
using System.Security.Claims;

namespace AssetManagement.Web.Controllers;

[Authorize]
public class MyWorkController : Controller
{
    private readonly AssetManagementDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthorizationService _authorizationService;
    private readonly IAuditService _auditService;
    private readonly ILogger<MyWorkController> _logger;

    public MyWorkController(
        AssetManagementDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService,
        IAuditService auditService,
        ILogger<MyWorkController> logger)
    {
        _context = context;
        _userManager = userManager;
        _authorizationService = authorizationService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // Debug: Log all claims
        _logger.LogInformation("User claims: {Claims}", string.Join(", ", User.Claims.Select(c => $"{c.Type}: {c.Value}")));
        
        // Debug: Log user identity
        _logger.LogInformation("User identity: {Identity}", User.Identity?.Name);
        _logger.LogInformation("User authenticated: {Authenticated}", User.Identity?.IsAuthenticated);
        
        // Try to find user by email from claims first (more reliable with Azure AD)
        var email = User.FindFirstValue(ClaimTypes.Email);
        _logger.LogInformation("Email from claims: {Email}", email);
        
        ApplicationUser? user = null;
        if (!string.IsNullOrEmpty(email))
        {
            user = await _userManager.FindByEmailAsync(email);
            _logger.LogInformation("FindByEmailAsync result: {User}", user?.Id ?? "NULL");
        }
        
        // Fallback to GetUserAsync if email lookup fails
        if (user == null)
        {
            user = await _userManager.GetUserAsync(User);
            _logger.LogInformation("GetUserAsync result: {User}", user?.Id ?? "NULL");
        }
        
        if (user == null)
            return NotFound();

        var userId = user.Id;
        var userRoles = await _authorizationService.GetUserRolesAsync(userId);
        var userPermissions = await _authorizationService.GetUserPermissionsAsync(userId);

        var viewModel = new MyWorkViewModel
        {
            User = user,
            Roles = userRoles,
            Permissions = userPermissions,
            PendingRequests = await GetPendingRequestsAsync(userId, userPermissions),
            MyRequests = await GetMyRequestsAsync(userId),
            PendingApprovals = await GetPendingApprovalsAsync(userId, userPermissions),
            RecentActivity = await GetRecentActivityAsync(userId),
            Workflows = await GetWorkflowsAsync(userId, userPermissions)
        };

        return View(viewModel);
    }

    private async Task<List<AssetRequest>> GetPendingRequestsAsync(string userId, string[] permissions)
    {
        var query = _context.AssetRequests
            .Include(r => r.Requester)
            .Include(r => r.Asset)
            .Where(r => r.Status == "Pending");

        // Filter based on permissions and scope
        if (permissions.Contains(Permissions.RequestsApprove))
        {
            // Approvers can see all pending requests
            return await query.ToListAsync();
        }
        else if (permissions.Contains(Permissions.RequestsRead))
        {
            // Regular users can only see requests in their scope
            return await query.Where(r => r.RequesterId == userId).ToListAsync();
        }

        return new List<AssetRequest>();
    }

    private async Task<List<AssetRequest>> GetMyRequestsAsync(string userId)
    {
        return await _context.AssetRequests
            .Include(r => r.Requester)
            .Include(r => r.Asset)
            .Where(r => r.RequesterId == userId)
            .OrderByDescending(r => r.RequestDate) // Use RequestDate instead of CreatedAt
            .Take(10)
            .ToListAsync();
    }

    private async Task<List<AssetRequest>> GetPendingApprovalsAsync(string userId, string[] permissions)
    {
        if (!permissions.Contains(Permissions.RequestsApprove))
            return new List<AssetRequest>();

        return await _context.AssetRequests
            .Include(r => r.Requester)
            .Include(r => r.Asset)
            .Where(r => r.Status == "Pending" && r.ApproverId == userId)
            .OrderByDescending(r => r.RequestDate) // Use RequestDate instead of CreatedAt
            .Take(10)
            .ToListAsync();
    }

    private async Task<List<AuditLog>> GetRecentActivityAsync(string userId)
    {
        try
        {
            return await _auditService.GetUserAuditLogsAsync(userId, page: 1, pageSize: 10);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve audit logs for user {UserId}. Returning empty list.", userId);
            return new List<AuditLog>();
        }
    }

    private async Task<List<Workflow>> GetWorkflowsAsync(string userId, string[] permissions)
    {
        var query = _context.Workflows
            .Include(w => w.Initiator)
            .Include(w => w.Assignee)
            .Include(w => w.Approver)
            .Include(w => w.Steps)
            .Where(w => w.Status != "Completed");

        // Filter based on permissions
        if (permissions.Contains(Permissions.WorkflowsManage))
        {
            // Managers can see all workflows
            return await query.OrderByDescending(w => w.CreatedAt).Take(10).ToListAsync();
        }
        else if (permissions.Contains(Permissions.WorkflowsRead))
        {
            // Regular users can see workflows they're involved in
            return await query.Where(w => 
                w.InitiatorId == userId || 
                w.AssigneeId == userId || 
                w.ApproverId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        return new List<Workflow>();
    }

    [HttpPost]
    public async Task<IActionResult> ApproveRequest(int requestId, string notes = "")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        var request = await _context.AssetRequests
            .Include(r => r.Requester)
            .Include(r => r.Asset)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            return NotFound();

        // Check if user has permission to approve this request
        if (!await _authorizationService.HasPermissionAsync(user.Id, Permissions.RequestsApprove))
        {
            return Forbid();
        }

        // Update request status
        request.Status = "Approved";
        request.ApprovalDate = DateTime.UtcNow; // Use ApprovalDate instead of ApprovedAt
        request.ApproverId = user.Id;
        request.ApprovalNotes = notes; // Use ApprovalNotes instead of Notes

        await _context.SaveChangesAsync();

        // Log the approval
        await _auditService.LogActionAsync(
            user.Id,
            user.Email!,
            "Approve",
            "AssetRequest",
            requestId.ToString(),
            new { Status = "Pending" },
            new { Status = "Approved", ApproverId = user.Id, ApprovalDate = DateTime.UtcNow },
            $"Request approved by {user.Email}",
            HttpContext.TraceIdentifier,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers["User-Agent"].ToString()
        );

        TempData["Success"] = "Request approved successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RejectRequest(int requestId, string notes = "")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        var request = await _context.AssetRequests
            .Include(r => r.Requester)
            .Include(r => r.Asset)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            return NotFound();

        // Check if user has permission to approve this request
        if (!await _authorizationService.HasPermissionAsync(user.Id, Permissions.RequestsApprove))
        {
            return Forbid();
        }

        // Update request status
        request.Status = "Rejected";
        request.ApprovalDate = DateTime.UtcNow; // Use ApprovalDate instead of ApprovedAt
        request.ApproverId = user.Id;
        request.RejectionReason = notes; // Use RejectionReason instead of Notes

        await _context.SaveChangesAsync();

        // Log the rejection
        await _auditService.LogActionAsync(
            user.Id,
            user.Email!,
            "Reject",
            "AssetRequest",
            requestId.ToString(),
            new { Status = "Pending" },
            new { Status = "Rejected", ApproverId = user.Id, ApprovalDate = DateTime.UtcNow },
            $"Request rejected by {user.Email}",
            HttpContext.TraceIdentifier,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers["User-Agent"].ToString()
        );

        TempData["Success"] = "Request rejected successfully.";
        return RedirectToAction(nameof(Index));
    }
}
