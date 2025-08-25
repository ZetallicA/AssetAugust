# Asset Management RBAC System Setup Guide

## 🎯 **Overview**

Your Asset Management system now has a comprehensive **Role-Based Access Control (RBAC)** system that integrates with your existing Azure AD users. This system provides granular permissions and role-based access across all system features.

## 📋 **Available Roles**

### **1. Admin** 
- **Full system access** - All permissions across all areas
- **User management** - Can assign roles and permissions to other users
- **System administration** - Complete control over the system

### **2. ITAdmin**
- **IT-focused permissions** - Asset management, user management (no delete), system audit
- **Full asset lifecycle** - Create, update, delete, assign, transfer, salvage assets
- **Request management** - Full request approval workflow
- **Building/Floor management** - Complete location management
- **Reports and workflows** - Full access to reporting and workflow systems
- **Import/Export** - Can import and export all data types

### **3. FacilitiesAdmin**
- **Facilities-focused permissions** - Asset management (no delete), building management
- **Request approval** - Can approve and manage asset requests
- **Location management** - Full building and floor management
- **Reporting** - View and export reports
- **Asset import/export** - Can import and export asset data

### **4. UnitManager**
- **Unit-level management** - Asset assignment, transfer, request approval
- **Limited asset management** - View, update, assign, transfer (no create/delete)
- **Request workflow** - Full request management capabilities
- **Reporting** - View and export reports
- **Asset export** - Can export asset data

### **5. AssetManager**
- **Asset-focused role** - Complete asset lifecycle management
- **Full asset permissions** - Create, update, delete, assign, transfer, salvage
- **Request management** - Full request workflow
- **Advanced reporting** - Create custom reports
- **Workflow management** - Full workflow capabilities
- **Asset import/export** - Complete data import/export

### **6. ReportViewer**
- **Read-only access** - View assets, requests, buildings, reports
- **Report export** - Can export reports
- **Audit access** - View audit logs
- **Workflow viewing** - Can view workflows

### **7. User**
- **Basic access** - View assets, create/update own requests
- **Limited reporting** - View basic reports
- **Workflow viewing** - Can view workflows

## 🚀 **How to Assign Roles to Your 23 Users**

### **Step 1: Access User Management**
1. Go to: `https://assets.oathone.com/Admin/UserManagement`
2. You'll see all your Azure AD users listed with their current roles and permissions

### **Step 2: Individual Role Assignment**
1. Click the **"Assign Roles"** button (user-tag icon) next to any user
2. Select the appropriate role(s) from the available options
3. Click **"Save"** to apply the changes

### **Step 3: Bulk Role Assignment (Recommended)**
1. Click **"Bulk Role Assignment"** button at the top
2. Select multiple users using checkboxes
3. Choose the role to assign (Add or Remove)
4. Click **"Apply"** to assign roles to all selected users

### **Step 4: Custom Permission Assignment**
1. Click the **"Assign Permissions"** button (key icon) next to any user
2. Select specific permissions with optional scope (Global, Building, Floor)
3. Set expiration dates if needed
4. Click **"Save"** to apply

## 📊 **Recommended Role Assignments for Your Organization**

Based on your 23 users, here are recommended assignments:

### **IT Team (4-6 users)**
- **Role**: `ITAdmin`
- **Users**: IT staff, system administrators, technical support

### **Facilities Team (3-4 users)**
- **Role**: `FacilitiesAdmin`
- **Users**: Facilities managers, building maintenance staff

### **Management Team (2-3 users)**
- **Role**: `UnitManager`
- **Users**: Department managers, team leads

### **Asset Management Team (2-3 users)**
- **Role**: `AssetManager`
- **Users**: Asset coordinators, inventory specialists

### **Reporting Team (2-3 users)**
- **Role**: `ReportViewer`
- **Users**: Analysts, reporting specialists

### **General Users (8-10 users)**
- **Role**: `User`
- **Users**: Regular employees who need basic access

## 🔐 **Permission Categories**

### **Asset Management (7 permissions)**
- `assets.read` - View assets
- `assets.create` - Create new assets
- `assets.update` - Update existing assets
- `assets.delete` - Delete assets
- `assets.assign` - Assign assets to users
- `assets.transfer` - Transfer assets between locations
- `assets.salvage` - Salvage/retire assets

### **Request Management (5 permissions)**
- `requests.read` - View requests
- `requests.create` - Create requests
- `requests.update` - Update requests
- `requests.approve` - Approve requests
- `requests.reject` - Reject requests

### **Building & Location (8 permissions)**
- `buildings.read/create/update/delete` - Building management
- `floors.read/create/update/delete` - Floor management

### **Reports (3 permissions)**
- `reports.view` - View reports
- `reports.export` - Export reports
- `reports.create` - Create custom reports

### **User Management (6 permissions)**
- `users.read/create/update/delete` - User management
- `users.assign_roles` - Assign roles to users
- `users.assign_permissions` - Assign permissions to users

### **System Administration (4 permissions)**
- `system.settings` - Manage system settings
- `system.audit` - View audit logs
- `system.backup/restore` - System backup/restore

### **Workflows (5 permissions)**
- `workflows.read/create/update/delete/approve` - Workflow management

### **Import/Export (4 permissions)**
- `import.assets/users` - Import data
- `export.assets/users` - Export data

## 🎯 **Scope-Based Permissions**

You can assign permissions with different scopes:

### **Global Scope**
- Permission applies to all buildings/locations
- Example: `assets.read` with Global scope = can view all assets

### **Building Scope**
- Permission applies to a specific building only
- Example: `assets.update` with Building scope = can only update assets in that building

### **Floor Scope**
- Permission applies to a specific floor only
- Example: `assets.assign` with Floor scope = can only assign assets on that floor

## 📈 **Quick Setup for Reports Access**

To give your users access to reports:

1. **For Report Viewers**: Assign `ReportViewer` role
2. **For Report Creators**: Assign `AssetManager` or `ITAdmin` role
3. **For Custom Permissions**: Use the permission assignment interface

## 🔄 **Managing Changes**

### **Adding New Users**
1. Users are automatically created when they first sign in via Azure AD
2. They get the `User` role by default
3. Use User Management to assign appropriate roles

### **Role Updates**
1. Changes take effect immediately
2. Users need to sign out and back in for role changes to apply
3. Permission changes are applied instantly

### **Audit Trail**
- All role and permission changes are logged in the audit system
- View audit logs through the system administration interface

## 🆘 **Troubleshooting**

### **User Can't Access Features**
1. Check their assigned roles in User Management
2. Verify they have the required permissions
3. Check if permissions are scoped correctly

### **Role Not Showing**
1. Ensure the role exists in the database
2. Check if the user is properly assigned to the role
3. Verify the user has signed out and back in

### **Permission Issues**
1. Check both role-based and direct permission assignments
2. Verify permission scope (Global vs Building vs Floor)
3. Check if permissions have expired

## 📞 **Support**

For technical support with the RBAC system:
1. Check the audit logs for permission issues
2. Use the User Management interface to troubleshoot
3. Contact your system administrator

---

**Your RBAC system is now ready!** 🎉

Access the User Management interface at: `https://assets.oathone.com/Admin/UserManagement`
