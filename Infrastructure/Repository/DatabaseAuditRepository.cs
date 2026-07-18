using Microsoft.EntityFrameworkCore;
using OptimusPrime.Core.Network;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Interfaces;
using OptimusPrime.Infrastructure.Database;

namespace OptimusPrime.Infrastructure.Repository;

public class DatabaseAuditRepository(
    ILogger<DatabaseAuditRepository> logger,
    AppDbcontext appDBcontext) : IAuditRepository {

    private readonly AppDbcontext _appDBcontext = appDBcontext;
    private readonly ILogger<DatabaseAuditRepository> _logger = logger;

    // ====================================================
    // AuditLog Database Operations: PostgresSQL
    // ====================================================

    // save audit log
    public async Task<Result<bool>> SaveAudit(AuditModel audit) {

        try {
            _appDBcontext.Audit.Add(audit);
            await _appDBcontext.SaveChangesAsync();

            return Result<bool>.Success("AuditLog saved successfully");

        } catch(Exception ex) {
            _logger.LogError("Error saving AuditLog: {Message}", ex.Message);
            return Result<bool>.Failure("Error saving AuditLog", ErrorType.InternalError);
        }
    }

    // retrieve audit logs with filters and pagination
    public async Task<Result<List<AuditModel>>> GetAudits(AuditCommand filters) {

        try {

            var query = _appDBcontext.Audit.AsQueryable();

            if(!string.IsNullOrWhiteSpace(filters.Timestamp)) 
                query = query.Where(a => a.Timestamp.ToString().Contains(filters.Timestamp));
        
            if(!string.IsNullOrWhiteSpace(filters.UserEmail)) 
                query = query.Where(a => a.UserEmail != null && a.UserEmail.ToLower().Contains(filters.UserEmail.ToLower()));

            if(!string.IsNullOrWhiteSpace(filters.ActionType)) 
                query = query.Where(a => a.ActionType != null && a.ActionType.ToLower().Contains(filters.ActionType.ToLower()));

            if(!string.IsNullOrWhiteSpace(filters.EntityName))
                query = query.Where(a => a.EntityName != null && a.EntityName.ToLower().Contains(filters.EntityName.ToLower()));

            if(!string.IsNullOrWhiteSpace(filters.Result)) {
                bool isSuccessTarget = filters.Result.ToLower().Contains("successo");
                query = query.Where(a => a.IsSuccess == isSuccessTarget);
            }

            if(!string.IsNullOrWhiteSpace(filters.Message)) 
                query = query.Where(a => a.Message != null && a.Message.ToLower().Contains(filters.Message.ToLower()));

            var audits = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip(filters.Page)
                .Take(filters.PageSize)
                .ToListAsync();

            return Result<List<AuditModel>>.Success(audits, "AuditLogs retrieved successfully");

        } catch(Exception ex) {
            _logger.LogError("Error retrieving AuditLogs: {Message}", ex.Message);
            return Result<List<AuditModel>>.Failure("Error retrieving AuditLogs", ErrorType.InternalError);
        }
    }
}
