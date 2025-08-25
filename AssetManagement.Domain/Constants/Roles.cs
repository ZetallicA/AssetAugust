namespace AssetManagement.Domain.Constants;

public static class Roles
{
    // System Roles
    public const string SuperAdmin = "SuperAdmin";
    public const string ITAdmin = "ITAdmin";
    public const string FacilitiesAdmin = "FacilitiesAdmin";
    public const string UnitManager = "UnitManager";
    public const string Clerk = "Clerk";
    public const string Viewer = "Viewer";
    public const string Procurement = "Procurement";
    public const string ITTechnician = "ITTechnician";
    public const string FacilitiesTechnician = "FacilitiesTechnician";
    
    // Get all roles
    public static readonly string[] AllRoles = {
        SuperAdmin, ITAdmin, FacilitiesAdmin, UnitManager, 
        Clerk, Viewer, Procurement, ITTechnician, FacilitiesTechnician
    };
}

public static class Permissions
{
    // Asset permissions
    public const string AssetsRead = "assets.read";
    public const string AssetsWrite = "assets.write";
    public const string AssetsDelete = "assets.delete";
    public const string AssetsApprove = "assets.approve";
    
    // Building permissions
    public const string BuildingsRead = "buildings.read";
    public const string BuildingsWrite = "buildings.write";
    public const string BuildingsManage = "buildings.manage";
    
    // Desk permissions
    public const string DesksRead = "desks.read";
    public const string DesksAssign = "desks.assign";
    public const string DesksManage = "desks.manage";
    
    // Phone permissions
    public const string PhonesRead = "phones.read";
    public const string PhonesAssign = "phones.assign";
    public const string PhonesManage = "phones.manage";
    
    // License permissions
    public const string LicensesRead = "licenses.read";
    public const string LicensesManage = "licenses.manage";
    
    // Request permissions
    public const string RequestsRead = "requests.read";
    public const string RequestsSubmit = "requests.submit";
    public const string RequestsApprove = "requests.approve";
    
    // Report permissions
    public const string ReportsView = "reports.view";
    public const string ReportsExport = "reports.export";
    
    // User permissions
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    
    // Workflow permissions
    public const string WorkflowsRead = "workflows.read";
    public const string WorkflowsManage = "workflows.manage";
    
    // System permissions
    public const string SystemSettings = "system.settings";
    public const string AuditLogs = "audit.logs";
    
    // Get all permissions
    public static readonly string[] AllPermissions = {
        AssetsRead, AssetsWrite, AssetsDelete, AssetsApprove,
        BuildingsRead, BuildingsWrite, BuildingsManage,
        DesksRead, DesksAssign, DesksManage,
        PhonesRead, PhonesAssign, PhonesManage,
        LicensesRead, LicensesManage,
        RequestsRead, RequestsSubmit, RequestsApprove,
        ReportsView, ReportsExport,
        UsersRead, UsersManage,
        WorkflowsRead, WorkflowsManage,
        SystemSettings, AuditLogs
    };
    
    // Role to permission mappings
    public static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        [Roles.SuperAdmin] = AllPermissions,
        
        [Roles.ITAdmin] = new[] {
            AssetsRead, AssetsWrite, AssetsDelete, AssetsApprove,
            BuildingsRead, BuildingsWrite,
            DesksRead, DesksAssign,
            PhonesRead, PhonesAssign,
            LicensesRead, LicensesManage,
            RequestsRead, RequestsApprove,
            ReportsView, ReportsExport,
            UsersRead,
            WorkflowsRead, WorkflowsManage,
            AuditLogs
        },
        
        [Roles.FacilitiesAdmin] = new[] {
            AssetsRead, AssetsWrite,
            BuildingsRead, BuildingsWrite, BuildingsManage,
            DesksRead, DesksAssign, DesksManage,
            PhonesRead, PhonesAssign, PhonesManage,
            RequestsRead, RequestsApprove,
            ReportsView,
            WorkflowsRead, WorkflowsManage
        },
        
        [Roles.UnitManager] = new[] {
            AssetsRead,
            BuildingsRead,
            DesksRead, DesksAssign,
            PhonesRead, PhonesAssign,
            RequestsRead, RequestsSubmit, RequestsApprove,
            ReportsView,
            WorkflowsRead
        },
        
        [Roles.Clerk] = new[] {
            AssetsRead,
            BuildingsRead,
            DesksRead,
            PhonesRead,
            RequestsRead, RequestsSubmit,
            ReportsView
        },
        
        [Roles.Viewer] = new[] {
            AssetsRead,
            BuildingsRead,
            DesksRead,
            PhonesRead,
            ReportsView
        },
        
        [Roles.Procurement] = new[] {
            AssetsRead, AssetsWrite,
            LicensesRead, LicensesManage,
            RequestsRead, RequestsApprove,
            ReportsView, ReportsExport
        },
        
        [Roles.ITTechnician] = new[] {
            AssetsRead, AssetsWrite,
            BuildingsRead,
            DesksRead, DesksAssign,
            PhonesRead, PhonesAssign,
            RequestsRead, RequestsSubmit,
            ReportsView
        },
        
        [Roles.FacilitiesTechnician] = new[] {
            AssetsRead,
            BuildingsRead, BuildingsWrite,
            DesksRead, DesksAssign,
            PhonesRead, PhonesAssign,
            RequestsRead, RequestsSubmit,
            ReportsView
        }
    };
}
