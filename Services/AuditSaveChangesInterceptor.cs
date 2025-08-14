using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using soft20181_starter.Models;
using soft20181_starter.Services;
using System.Security.Claims;

namespace soft20181_starter.Services
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditSaveChangesInterceptor(IAuditService auditService, IHttpContextAccessor httpContextAccessor)
        {
            _auditService = auditService;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) return result;

            var auditEntries = OnBeforeSaveChanges(context);
            var saveResult = await base.SavingChangesAsync(eventData, result, cancellationToken);
            await OnAfterSaveChanges(context, auditEntries);

            return saveResult;
        }

        private List<AuditEntry> OnBeforeSaveChanges(DbContext context)
        {
            context.ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();
            var entries = context.ChangeTracker.Entries();

            foreach (var entry in entries)
            {
                // Skip entities that are not tracked or are not one of our audited entities
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                // Only audit specific entities
                if (!IsAuditableEntity(entry.Entity))
                    continue;

                var auditEntry = new AuditEntry(entry)
                {
                    TableName = entry.Entity.GetType().Name,
                    Action = entry.State.ToString(),
                    UserId = GetCurrentUserId(),
                    UserName = GetCurrentUserName(),
                    UserRole = GetCurrentUserRole(),
                    Timestamp = DateTime.UtcNow
                };

                auditEntries.Add(auditEntry);

                foreach (var property in entry.Properties)
                {
                    if (property.IsTemporary)
                        continue;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.NewValues[property.Metadata.Name] = property.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            auditEntry.OldValues[property.Metadata.Name] = property.OriginalValue;
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                auditEntry.OldValues[property.Metadata.Name] = property.OriginalValue;
                                auditEntry.NewValues[property.Metadata.Name] = property.CurrentValue;
                            }
                            break;
                    }
                }
            }

            return auditEntries;
        }

        private async Task OnAfterSaveChanges(DbContext context, List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return;

            foreach (var auditEntry in auditEntries)
            {
                try
                {
                    var entityName = auditEntry.TableName;
                    var entityId = GetEntityId(auditEntry.Entry.Entity);
                    var action = auditEntry.Action;

                    switch (action)
                    {
                        case "Added":
                            await _auditService.LogCreateAsync(
                                auditEntry.Entry.Entity,
                                auditEntry.UserId,
                                auditEntry.UserName,
                                auditEntry.UserRole);
                            break;

                        case "Modified":
                            if (auditEntry.OldValues.Any() && auditEntry.NewValues.Any())
                            {
                                await _auditService.LogUpdateAsync(
                                    auditEntry.Entry.Entity,
                                    auditEntry.Entry.OriginalValues,
                                    auditEntry.Entry.CurrentValues,
                                    auditEntry.UserId,
                                    auditEntry.UserName,
                                    auditEntry.UserRole);
                            }
                            break;

                        case "Deleted":
                            await _auditService.LogDeleteAsync(
                                auditEntry.Entry.Entity,
                                auditEntry.UserId,
                                auditEntry.UserName,
                                auditEntry.UserRole);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but don't throw to avoid breaking the main operation
                    Console.WriteLine($"Audit logging failed: {ex.Message}");
                }
            }
        }

        private bool IsAuditableEntity(object entity)
        {
            var entityType = entity.GetType().Name;
            return entityType == "TheEvent" || 
                   entityType == "AppUser" || 
                   entityType == "Contact" || 
                   entityType == "Feedback";
        }

        private string GetEntityId(object entity)
        {
            var idProperty = entity.GetType().GetProperties()
                .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) || 
                                   p.Name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                                   p.Name.Equals("UserId", StringComparison.OrdinalIgnoreCase) ||
                                   p.Name.Equals("UserName", StringComparison.OrdinalIgnoreCase));

            if (idProperty != null)
            {
                var value = idProperty.GetValue(entity);
                return value?.ToString() ?? "unknown";
            }

            return "unknown";
        }

        private string GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        }

        private string GetCurrentUserName()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userName = user?.FindFirst(ClaimTypes.Name)?.Value;
            var email = user?.FindFirst(ClaimTypes.Email)?.Value;
            return userName ?? email ?? "anonymous";
        }

        private string GetCurrentUserRole()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Role)?.Value ?? "anonymous";
        }
    }

    public class AuditEntry
    {
        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }

        public EntityEntry Entry { get; }
        public string TableName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object?> OldValues { get; } = new Dictionary<string, object?>();
        public Dictionary<string, object?> NewValues { get; } = new Dictionary<string, object?>();
    }
}
