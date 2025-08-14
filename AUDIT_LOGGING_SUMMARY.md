# EventScape Audit Logging System

## Overview
This document describes the comprehensive audit logging system implemented for the EventScape application. The system tracks all changes made to users, events, contacts, and feedback entities with detailed metadata.

## Features Implemented

### 1. Enhanced AuditLog Model
- **Location**: `Models/AuditLog.cs`
- **Features**:
  - Comprehensive tracking of entity changes (Create, Update, Delete)
  - User information (ID, Name, Role)
  - Detailed change tracking (Old values, New values, Affected columns)
  - Request metadata (IP Address, User Agent, Session ID, Controller, Action, URL)
  - Timestamp and success/failure tracking
  - Helper methods for formatting and validation

### 2. SimpleAuditService
- **Location**: `Services/SimpleAuditService.cs`
- **Features**:
  - Generic audit logging for any entity type
  - Automatic user context detection
  - Detailed change comparison for updates
  - JSON serialization of entity data
  - Query and filtering capabilities
  - CSV export functionality

### 3. Admin Audit Logs Interface
- **Location**: `Pages/Admin/AuditLogs.cshtml` and `Pages/Admin/AuditLogs.cshtml.cs`
- **Features**:
  - Comprehensive filtering (Entity type, Action, User, Date range)
  - Pagination support
  - Detailed view modal for each audit log
  - CSV export functionality
  - Real-time search and filtering

### 4. Integration Points
The audit logging has been integrated into the following areas:

#### Contact Form (`Pages/Contact.cshtml.cs`)
- Logs creation of new contact submissions
- Logs creation of feedback submissions

#### Event Management (`Pages/AddEvent.cshtml.cs`)
- Logs creation of new events

#### User Management (`Pages/Admin/Users/Create.cshtml.cs`)
- Logs creation of new users
- Tracks role assignments

## Database Schema

### AuditLog Table Structure
```sql
CREATE TABLE AuditLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EntityName TEXT NOT NULL,           -- "TheEvent", "AppUser", "Contact", "Feedback"
    EntityId TEXT NOT NULL,             -- ID of the affected entity
    Action TEXT NOT NULL,               -- "Create", "Update", "Delete"
    UserId TEXT,                        -- ID of the user who performed the action
    UserName TEXT,                      -- Display name of the user
    UserRole TEXT,                      -- Role of the user
    OldValues TEXT,                     -- JSON serialized old values (for updates)
    NewValues TEXT,                     -- JSON serialized new values
    Changes TEXT,                       -- Human-readable change description
    AffectedColumns TEXT,               -- Comma-separated list of changed columns
    TableName TEXT,                     -- Database table name
    Schema TEXT,                        -- Database schema
    Timestamp DATETIME NOT NULL,        -- UTC timestamp
    IpAddress TEXT,                     -- Client IP address
    UserAgent TEXT,                     -- Browser user agent
    SessionId TEXT,                     -- Session identifier
    ControllerName TEXT,                -- MVC controller name
    ActionName TEXT,                    -- MVC action name
    RequestUrl TEXT,                    -- Request URL
    IsSuccessful BOOLEAN NOT NULL,      -- Whether the operation succeeded
    ErrorMessage TEXT                   -- Error message if operation failed
);
```

## Usage Examples

### 1. Logging Entity Creation
```csharp
// In any controller or page model
private readonly SimpleAuditService _auditService;

// After successfully creating an entity
await _auditService.LogCreateAsync(entity);
```

### 2. Logging Entity Updates
```csharp
// For updates, you need to capture old and new values
var oldValues = new Dictionary<string, object?> { /* old values */ };
var newValues = new Dictionary<string, object?> { /* new values */ };

await _auditService.LogUpdateAsync(entity, oldValues, newValues);
```

### 3. Logging Entity Deletion
```csharp
// Before deleting an entity
await _auditService.LogDeleteAsync(entity);
```

### 4. Logging User Actions
```csharp
// For custom user actions
await _auditService.LogUserActionAsync("Login", userId, userName, userRole, "User logged in successfully");
```

## Admin Interface Usage

### Accessing Audit Logs
1. Navigate to Admin Dashboard
2. Click on "Audit Logs" panel
3. Use filters to search for specific audit entries
4. Click "View" to see detailed information

### Filtering Options
- **Entity Type**: Filter by TheEvent, AppUser, Contact, or Feedback
- **Action**: Filter by Create, Update, or Delete operations
- **User ID**: Search for actions by specific user
- **Date Range**: Filter by timestamp range
- **Export**: Download filtered results as CSV

### Audit Log Details
Each audit log entry shows:
- Basic information (timestamp, entity, action)
- User information (ID, name, role, IP address)
- Request details (controller, action, URL)
- Change summary (what was changed)
- Detailed old/new values (JSON format)

## Security Features

### Authorization
- Audit logs are only accessible to Admin and Administrator roles
- All audit log pages require proper authorization

### Data Protection
- Sensitive data is properly serialized and stored
- IP addresses and user agents are logged for security tracking
- Session information is captured for audit trail

### Error Handling
- Audit logging failures don't break main application functionality
- Comprehensive error logging for debugging
- Graceful degradation when audit service is unavailable

## Performance Considerations

### Database Indexing
The AuditLog table includes indexes on:
- EntityName
- EntityId
- UserId
- Timestamp
- Action

### Pagination
- Default page size: 50 records
- Configurable pagination for large datasets
- Efficient querying with proper filtering

### Storage Optimization
- JSON serialization for flexible data storage
- Text fields for large content
- Proper field sizing for optimal storage

## Monitoring and Maintenance

### Regular Tasks
1. **Archive old logs**: Consider archiving logs older than 1 year
2. **Monitor storage**: Audit logs can grow large over time
3. **Review access patterns**: Monitor who accesses audit logs
4. **Backup strategy**: Include audit logs in regular backups

### Performance Monitoring
- Monitor query performance on large datasets
- Consider partitioning for very large audit tables
- Implement log rotation if needed

## Future Enhancements

### Potential Improvements
1. **Real-time notifications**: Alert admins of suspicious activities
2. **Advanced analytics**: Dashboard with audit statistics
3. **Automated reporting**: Scheduled audit reports
4. **Integration with SIEM**: Export to security information systems
5. **Data retention policies**: Automated cleanup of old logs

### API Endpoints
Consider adding REST API endpoints for:
- Programmatic access to audit logs
- Integration with external monitoring systems
- Automated audit log analysis

## Troubleshooting

### Common Issues
1. **Missing audit logs**: Check if SimpleAuditService is properly registered
2. **Performance issues**: Verify database indexes are created
3. **Permission errors**: Ensure user has proper admin role
4. **Large log files**: Implement log rotation or archiving

### Debug Information
- Check application logs for audit service errors
- Verify database connection and permissions
- Test audit logging with simple operations first

## Conclusion

The EventScape audit logging system provides comprehensive tracking of all data changes with detailed metadata. It ensures accountability, security, and compliance while maintaining good performance and usability. The system is designed to be extensible and can be enhanced with additional features as needed.
