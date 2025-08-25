namespace AssetManagement.Domain.Entities;

public class Workflow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Desk Assignment", "Equipment Request"
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // e.g., "assignment", "request", "approval"
    public string Status { get; set; } = string.Empty; // e.g., "draft", "pending", "approved", "rejected"
    public string InitiatorId { get; set; } = string.Empty;
    public string? AssigneeId { get; set; }
    public string? ApproverId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public int Priority { get; set; } = 1; // 1=Low, 2=Medium, 3=High, 4=Critical
    
    // Workflow-specific data (JSON serialized)
    public string? WorkflowData { get; set; }
    
    // Navigation properties
    public ApplicationUser Initiator { get; set; } = null!;
    public ApplicationUser? Assignee { get; set; }
    public ApplicationUser? Approver { get; set; }
    public List<WorkflowStep> Steps { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();
}

public class WorkflowStep
{
    public int Id { get; set; }
    public int WorkflowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Status { get; set; } = string.Empty; // "pending", "completed", "skipped"
    public string? AssignedToId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    
    // Navigation properties
    public Workflow Workflow { get; set; } = null!;
    public ApplicationUser? AssignedTo { get; set; }
}
