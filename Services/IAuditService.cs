using soft20181_starter.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace soft20181_starter.Services
{
    public interface IAuditService
    {
        Task LogCreateAsync<T>(T entity, string userId, string userName, string userRole, string? ipAddress = null, string? userAgent = null, string? sessionId = null) where T : class;
        Task LogUpdateAsync<T>(T entity, PropertyValues originalValues, PropertyValues currentValues, string userId, string userName, string userRole, string? ipAddress = null, string? userAgent = null, string? sessionId = null) where T : class;
        Task LogDeleteAsync<T>(T entity, string userId, string userName, string userRole, string? ipAddress = null, string? userAgent = null, string? sessionId = null) where T : class;
        Task LogUserActionAsync(string action, string userId, string userName, string userRole, string? details = null, string? ipAddress = null, string? userAgent = null, string? sessionId = null);
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string? entityName = null, string? entityId = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 50);
        Task<int> GetAuditLogsCountAsync(string? entityName = null, string? entityId = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
    }
}
