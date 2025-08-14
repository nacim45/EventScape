using soft20181_starter.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace soft20181_starter.Services
{
    public class SimpleAuditService
    {
        private readonly EventAppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SimpleAuditService> _logger;

        public SimpleAuditService(EventAppDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<SimpleAuditService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogCreateAsync<T>(T entity, string? userId = null, string? userName = null, string? userRole = null) where T : class
        {
            try
            {
                _logger.LogInformation("Starting LogCreateAsync for entity type: {EntityType}", typeof(T).Name);
                
                var entityName = typeof(T).Name;
                var entityId = GetEntityId(entity);
                var newValues = SerializeEntity(entity);

                _logger.LogInformation("Entity details - Name: {EntityName}, ID: {EntityId}", entityName, entityId);

                var auditLog = new AuditLog
                {
                    EntityName = entityName,
                    EntityId = entityId,
                    Action = "Create",
                    UserId = userId ?? GetCurrentUserId(),
                    UserName = userName ?? GetCurrentUserName(),
                    UserRole = userRole ?? GetCurrentUserRole(),
                    NewValues = newValues,
                    Changes = $"Created new {entityName} with ID: {entityId}",
                    TableName = GetTableName<T>(),
                    Schema = "dbo",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    SessionId = GetSessionId(),
                    ControllerName = GetControllerName(),
                    ActionName = GetActionName(),
                    RequestUrl = GetRequestUrl(),
                    IsSuccessful = true
                };

                _logger.LogInformation("Created audit log object: {AuditLogDetails}", 
                    $"Entity: {auditLog.EntityName}, Action: {auditLog.Action}, User: {auditLog.UserName}");

                _context.AuditLogs.Add(auditLog);
                var result = await _context.SaveChangesAsync();
                
                _logger.LogInformation("Audit log saved successfully. Rows affected: {RowsAffected}", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LogCreateAsync for entity type: {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task LogUpdateAsync<T>(T entity, Dictionary<string, object?> oldValues, Dictionary<string, object?> newValues, string? userId = null, string? userName = null, string? userRole = null) where T : class
        {
            try
            {
                _logger.LogInformation("Starting LogUpdateAsync for entity type: {EntityType}", typeof(T).Name);
                
                var entityName = typeof(T).Name;
                var entityId = GetEntityId(entity);
                var changes = new List<string>();
                var affectedColumns = new List<string>();

                foreach (var kvp in newValues)
                {
                    if (oldValues.ContainsKey(kvp.Key) && !Equals(oldValues[kvp.Key], kvp.Value))
                    {
                        affectedColumns.Add(kvp.Key);
                        changes.Add($"{kvp.Key}: '{oldValues[kvp.Key]}' → '{kvp.Value}'");
                    }
                }

                if (changes.Any())
                {
                    var auditLog = new AuditLog
                    {
                        EntityName = entityName,
                        EntityId = entityId,
                        Action = "Update",
                        UserId = userId ?? GetCurrentUserId(),
                        UserName = userName ?? GetCurrentUserName(),
                        UserRole = userRole ?? GetCurrentUserRole(),
                        OldValues = JsonSerializer.Serialize(oldValues),
                        NewValues = JsonSerializer.Serialize(newValues),
                        Changes = string.Join("; ", changes),
                        AffectedColumns = string.Join(", ", affectedColumns),
                        TableName = GetTableName<T>(),
                        Schema = "dbo",
                        Timestamp = DateTime.UtcNow,
                        IpAddress = GetClientIpAddress(),
                        UserAgent = GetUserAgent(),
                        SessionId = GetSessionId(),
                        ControllerName = GetControllerName(),
                        ActionName = GetActionName(),
                        RequestUrl = GetRequestUrl(),
                        IsSuccessful = true
                    };

                    _context.AuditLogs.Add(auditLog);
                    var result = await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Update audit log saved successfully. Rows affected: {RowsAffected}", result);
                }
                else
                {
                    _logger.LogInformation("No changes detected for entity {EntityName} with ID {EntityId}", entityName, entityId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LogUpdateAsync for entity type: {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task LogDeleteAsync<T>(T entity, string? userId = null, string? userName = null, string? userRole = null) where T : class
        {
            try
            {
                _logger.LogInformation("Starting LogDeleteAsync for entity type: {EntityType}", typeof(T).Name);
                
                var entityName = typeof(T).Name;
                var entityId = GetEntityId(entity);
                var oldValues = SerializeEntity(entity);

                var auditLog = new AuditLog
                {
                    EntityName = entityName,
                    EntityId = entityId,
                    Action = "Delete",
                    UserId = userId ?? GetCurrentUserId(),
                    UserName = userName ?? GetCurrentUserName(),
                    UserRole = userRole ?? GetCurrentUserRole(),
                    OldValues = oldValues,
                    Changes = $"Deleted {entityName} with ID: {entityId}",
                    TableName = GetTableName<T>(),
                    Schema = "dbo",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    SessionId = GetSessionId(),
                    ControllerName = GetControllerName(),
                    ActionName = GetActionName(),
                    RequestUrl = GetRequestUrl(),
                    IsSuccessful = true
                };

                _context.AuditLogs.Add(auditLog);
                var result = await _context.SaveChangesAsync();
                
                _logger.LogInformation("Delete audit log saved successfully. Rows affected: {RowsAffected}", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LogDeleteAsync for entity type: {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task LogUserActionAsync(string action, string details, string? userId = null, string? userName = null, string? userRole = null)
        {
            try
            {
                _logger.LogInformation("Starting LogUserActionAsync: {Action}", action);
                
                var auditLog = new AuditLog
                {
                    EntityName = "UserAction",
                    EntityId = "0",
                    Action = action,
                    UserId = userId ?? GetCurrentUserId(),
                    UserName = userName ?? GetCurrentUserName(),
                    UserRole = userRole ?? GetCurrentUserRole(),
                    Changes = details,
                    TableName = "UserActions",
                    Schema = "dbo",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    SessionId = GetSessionId(),
                    ControllerName = GetControllerName(),
                    ActionName = GetActionName(),
                    RequestUrl = GetRequestUrl(),
                    IsSuccessful = true
                };

                _context.AuditLogs.Add(auditLog);
                var result = await _context.SaveChangesAsync();
                
                _logger.LogInformation("User action audit log saved successfully. Rows affected: {RowsAffected}", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LogUserActionAsync: {Action}", action);
                throw;
            }
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync(
            string? entityName = null,
            string? action = null,
            string? userId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 50)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();

                if (!string.IsNullOrEmpty(entityName))
                    query = query.Where(a => a.EntityName == entityName);

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(a => a.Action == action);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAuditLogsAsync");
                throw;
            }
        }

        public async Task<int> GetAuditLogsCountAsync(
            string? entityName = null,
            string? action = null,
            string? userId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();

                if (!string.IsNullOrEmpty(entityName))
                    query = query.Where(a => a.EntityName == entityName);

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(a => a.Action == action);

                if (!string.IsNullOrEmpty(userId))
                    query = query.Where(a => a.UserId == userId);

                if (fromDate.HasValue)
                    query = query.Where(a => a.Timestamp >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(a => a.Timestamp <= toDate.Value);

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAuditLogsCountAsync");
                throw;
            }
        }

        private string GetEntityId<T>(T entity) where T : class
        {
            try
            {
                var idProperty = typeof(T).GetProperties()
                    .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) || 
                                       p.Name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                                       p.Name.Equals("UserId", StringComparison.OrdinalIgnoreCase) ||
                                       p.Name.Equals("UserName", StringComparison.OrdinalIgnoreCase));

                if (idProperty != null)
                {
                    var value = idProperty.GetValue(entity);
                    var result = value?.ToString() ?? "unknown";
                    _logger.LogDebug("Found entity ID: {EntityId} for property {PropertyName}", result, idProperty.Name);
                    return result;
                }

                _logger.LogWarning("No ID property found for entity type: {EntityType}", typeof(T).Name);
                return "unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity ID for type: {EntityType}", typeof(T).Name);
                return "error";
            }
        }

        private string SerializeEntity<T>(T entity) where T : class
        {
            try
            {
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead)
                    .ToDictionary(p => p.Name, p => p.GetValue(entity));

                return JsonSerializer.Serialize(properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serializing entity of type: {EntityType}", typeof(T).Name);
                return "{}";
            }
        }

        private string GetTableName<T>() where T : class
        {
            return typeof(T).Name;
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
}
