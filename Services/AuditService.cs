using soft20181_starter.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;

namespace soft20181_starter.Services
{
    public class AuditService : IAuditService
    {
        private readonly EventAppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(EventAppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogCreateAsync<T>(T entity, string userId, string userName, string userRole, string? ipAddress = null, string? userAgent = null, string? sessionId = null) where T : class
        {
            var entityName = typeof(T).Name;
            var entityId = GetEntityId(entity);
            var newValues = SerializeEntity(entity);

            var auditLog = new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = "Create",
                UserId = userId,
                UserName = userName,
                UserRole = userRole,
                NewValues = newValues,
                Changes = $"Created new {entityName} with ID: {entityId}",
                TableName = GetTableName<T>(),
                Schema = "dbo",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress ?? GetClientIpAddress(),
                UserAgent = userAgent ?? GetUserAgent(),
                SessionId = sessionId ?? GetSessionId(),
                ControllerName = GetControllerName(),
                ActionName = GetActionName(),
                RequestUrl = GetRequestUrl(),
                IsSuccessful = true
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task LogUpdateAsync<T>(T entity, PropertyValues originalValues, PropertyValues currentValues, string userId, string userName, string userRole, string? ipAddress = null, string? userAgent = null, string? sessionId = null) where T : class
        {
            var entityName = typeof(T).Name;
            var entityId = GetEntityId(entity);
            var changes = new List<string>();
            var affectedColumns = new List<string>();

            var oldValuesDict = new Dictionary<string, object?>();
            var newValuesDict = new Dictionary<string, object?>();

            foreach (var property in originalValues.Properties)
            {
                var originalValue = originalValues[property];
                var currentValue = currentValues[property];

                if (!Equals(originalValue, currentValue))
                {
                    affectedColumns.Add(property.Name);
                    oldValuesDict[property.Name] = originalValue;
                    newValuesDict[property.Name] = currentValue;

                    changes.Add($"{property.Name}: '{originalValue}' → '{currentValue}'");
                }
            }

            if (changes.Any())
            {
                var auditLog = new AuditLog
                {
                    EntityName = entityName,
                    EntityId = entityId,
                    Action = "Update",
                    UserId = userId,
                    UserName = userName,
                    UserRole = userRole,
                    OldValues = JsonSerializer.Serialize(oldValuesDict),
                    NewValues = JsonSerializer.Serialize(newValuesDict),
                    Changes = string.Join("; ", changes),
                    AffectedColumns = string.Join(", ", affectedColumns),
                    TableName = GetTableName<T>(),
                    Schema = "dbo",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress ?? GetClientIpAddress(),
                    UserAgent = userAgent ?? GetUserAgent(),
                    SessionId = sessionId ?? GetSessionId(),
                    ControllerName = GetControllerName(),
                    ActionName = GetActionName(),
                    RequestUrl = GetRequestUrl(),
                    IsSuccessful = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
        }

        public async Task LogDeleteAsync<T>(T entity, string userId, string userName, string userRole, string? ipAddress = null, string? userAgent = null, string? sessionId = null) where T : class
        {
            var entityName = typeof(T).Name;
            var entityId = GetEntityId(entity);
            var oldValues = SerializeEntity(entity);

            var auditLog = new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = "Delete",
                UserId = userId,
                UserName = userName,
                UserRole = userRole,
                OldValues = oldValues,
                Changes = $"Deleted {entityName} with ID: {entityId}",
                TableName = GetTableName<T>(),
                Schema = "dbo",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress ?? GetClientIpAddress(),
                UserAgent = userAgent ?? GetUserAgent(),
                SessionId = sessionId ?? GetSessionId(),
                ControllerName = GetControllerName(),
                ActionName = GetActionName(),
                RequestUrl = GetRequestUrl(),
                IsSuccessful = true
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task LogUserActionAsync(string action, string userId, string userName, string userRole, string? details = null, string? ipAddress = null, string? userAgent = null, string? sessionId = null)
        {
            var auditLog = new AuditLog
            {
                EntityName = "User",
                EntityId = userId,
                Action = action,
                UserId = userId,
                UserName = userName,
                UserRole = userRole,
                Changes = details ?? $"User action: {action}",
                TableName = "AspNetUsers",
                Schema = "dbo",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress ?? GetClientIpAddress(),
                UserAgent = userAgent ?? GetUserAgent(),
                SessionId = sessionId ?? GetSessionId(),
                ControllerName = GetControllerName(),
                ActionName = GetActionName(),
                RequestUrl = GetRequestUrl(),
                IsSuccessful = true
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string? entityName = null, string? entityId = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 50)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(entityName))
                query = query.Where(a => a.EntityName == entityName);

            if (!string.IsNullOrEmpty(entityId))
                query = query.Where(a => a.EntityId == entityId);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.UserId == userId);

            if (fromDate.HasValue)
                query = query.Where(a => a.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Timestamp <= toDate.Value);

            return await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetAuditLogsCountAsync(string? entityName = null, string? entityId = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(entityName))
                query = query.Where(a => a.EntityName == entityName);

            if (!string.IsNullOrEmpty(entityId))
                query = query.Where(a => a.EntityId == entityId);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.UserId == userId);

            if (fromDate.HasValue)
                query = query.Where(a => a.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Timestamp <= toDate.Value);

            return await query.CountAsync();
        }

        private string GetEntityId<T>(T entity) where T : class
        {
            var idProperty = typeof(T).GetProperties()
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

        private string SerializeEntity<T>(T entity) where T : class
        {
            try
            {
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && !p.GetCustomAttributes(typeof(NotMappedAttribute), true).Any())
                    .ToDictionary(p => p.Name, p => p.GetValue(entity));

                return JsonSerializer.Serialize(properties);
            }
            catch
            {
                return "{}";
            }
        }

        private string GetTableName<T>() where T : class
        {
            var tableAttribute = typeof(T).GetCustomAttribute<TableAttribute>();
            return tableAttribute?.Name ?? typeof(T).Name;
        }

        private string? GetClientIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        private string? GetUserAgent()
        {
            return _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();
        }

        private string? GetSessionId()
        {
            return _httpContextAccessor.HttpContext?.Session?.Id;
        }

        private string? GetControllerName()
        {
            return _httpContextAccessor.HttpContext?.GetRouteValue("controller")?.ToString();
        }

        private string? GetActionName()
        {
            return _httpContextAccessor.HttpContext?.GetRouteValue("action")?.ToString();
        }

        private string? GetRequestUrl()
        {
            return _httpContextAccessor.HttpContext?.Request?.Path.Value;
        }
    }
}
