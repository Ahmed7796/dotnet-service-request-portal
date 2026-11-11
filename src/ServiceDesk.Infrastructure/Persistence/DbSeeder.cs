using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Infrastructure.Persistence;

public static class DbSeeder
{
    public const string DemoPassword = "ServiceDesk!2026";

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Departments.AnyAsync(ct))
            return;

        var hasher = new PasswordHasher<User>();

        // The default PasswordHasher ignores the user argument, and the factories need the hash before the user exists.
        // Called once per user so each gets its own salt.
        string Hash() => hasher.HashPassword(null!, DemoPassword);

        var operations = new Department("Operations");
        var finance = new Department("Finance");
        var it = new Department("IT");
        var humanResources = new Department("Human Resources");

        var users = new[]
        {
            User.CreateRequester("requester@servicedesk.local", "1001", Hash(), "Omar Requester", operations.Id),
            User.CreateApprover("ops.head@servicedesk.local", "2001", Hash(), "Layla Ops Head", operations.Id, ApproverPosition.DepartmentHead),
            User.CreateApprover("finance.head@servicedesk.local", "2002", Hash(), "Bruce Finance Head", finance.Id, ApproverPosition.DepartmentHead),
            User.CreateApprover("it.manager@servicedesk.local", "3001", Hash(), "Sara IT Manager", it.Id, ApproverPosition.ItManager),
            User.CreateApprover("hr.manager@servicedesk.local", "3002", Hash(), "Hana HR Manager", humanResources.Id, ApproverPosition.HrManager),
            User.CreateApprover("finance.manager@servicedesk.local", "3003", Hash(), "Karim Finance Manager", finance.Id, ApproverPosition.FinanceManager),
            User.CreateApprover("gm@servicedesk.local", "4001", Hash(), "Yousef General Manager", operations.Id, ApproverPosition.GeneralManager),
            User.CreateAdmin("admin@servicedesk.local", "9001", Hash(), "Admin", it.Id),
        };

        var workflows = new[]
        {
            new WorkflowTemplate(RequestType.Equipment, "Equipment request", [(ApproverPosition.DepartmentHead, ApprovalScope.RequesterDepartment), (ApproverPosition.ItManager, ApprovalScope.Global)]),
            new WorkflowTemplate(RequestType.SoftwareAccess, "Software access request", [(ApproverPosition.ItManager, ApprovalScope.Global)]),
            new WorkflowTemplate(RequestType.Leave, "Leave request", [(ApproverPosition.HrManager, ApprovalScope.Global)]),
            new WorkflowTemplate(RequestType.Purchase, "Purchase request", [(ApproverPosition.DepartmentHead, ApprovalScope.RequesterDepartment), (ApproverPosition.FinanceManager, ApprovalScope.Global)]),
        };

        db.Departments.AddRange(operations, finance, it, humanResources);
        db.Users.AddRange(users);
        db.WorkflowTemplates.AddRange(workflows);

        await db.SaveChangesAsync(ct);
    }
}
