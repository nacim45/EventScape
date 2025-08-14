using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using soft20181_starter.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages.Admin
{
    [Authorize(Roles = "Admin,Administrator")]
    public class AuditLogsModel : PageModel
    {
        private readonly SimpleAuditService _auditService;
        private readonly ILogger<AuditLogsModel> _logger;

        public AuditLogsModel(SimpleAuditService auditService, ILogger<AuditLogsModel> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? EntityName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Action { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 50;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public IEnumerable<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber;

            // Get audit logs with filtering
            AuditLogs = await _auditService.GetAuditLogsAsync(
                entityName: EntityName,
                action: Action,
                userId: UserId,
                fromDate: FromDate,
                toDate: ToDate,
                page: CurrentPage,
                pageSize: PageSize
            );

            // Get total count for pagination
            TotalCount = await _auditService.GetAuditLogsCountAsync(
                entityName: EntityName,
                action: Action,
                userId: UserId,
                fromDate: FromDate,
                toDate: ToDate
            );

            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
        }

        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            var auditLogs = await _auditService.GetAuditLogsAsync();
            var log = auditLogs.FirstOrDefault(a => a.Id == id);

            if (log == null)
            {
                return NotFound();
            }

            return new PartialViewResult
            {
                ViewName = "_AuditLogDetails",
                ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<AuditLog>(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                {
                    Model = log
                }
            };
        }

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            var auditLogs = await _auditService.GetAuditLogsAsync(
                entityName: EntityName,
                action: Action,
                userId: UserId,
                fromDate: FromDate,
                toDate: ToDate,
                page: 1,
                pageSize: 10000 // Export all records
            );

            var csv = "Timestamp,Entity,EntityId,Action,UserId,UserName,UserRole,Changes,IPAddress,UserAgent,SessionId,ControllerName,ActionName,RequestUrl,IsSuccessful\n";

            foreach (var log in auditLogs)
            {
                csv += $"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",";
                csv += $"\"{log.EntityName}\",";
                csv += $"\"{log.EntityId}\",";
                csv += $"\"{log.Action}\",";
                csv += $"\"{log.UserId}\",";
                csv += $"\"{log.UserName}\",";
                csv += $"\"{log.UserRole}\",";
                csv += $"\"{log.Changes?.Replace("\"", "\"\"")}\",";
                csv += $"\"{log.IpAddress}\",";
                csv += $"\"{log.UserAgent?.Replace("\"", "\"\"")}\",";
                csv += $"\"{log.SessionId}\",";
                csv += $"\"{log.ControllerName}\",";
                csv += $"\"{log.ActionName}\",";
                csv += $"\"{log.RequestUrl}\",";
                csv += $"\"{log.IsSuccessful}\"\n";
            }

            var fileName = $"audit_logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }
    }
}
