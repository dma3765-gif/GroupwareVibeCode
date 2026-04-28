using Engine.Application.Approval;
using Engine.Application.Common.DTOs;
using Engine.Application.Common.Exceptions;
using Engine.Application.Common.Interfaces;
using Engine.Application.Common.Responses;
using Engine.Domain.Approval;
using Engine.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace Engine.Infrastructure.Services;

public class ApprovalFormServiceImpl : IApprovalFormService
{
    private readonly GroupwareDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogService _audit;

    public ApprovalFormServiceImpl(GroupwareDbContext db, ICurrentUserContext currentUser, IAuditLogService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResult<ApprovalFormDto>> GetFormsAsync(PagedRequest request, CancellationToken ct = default)
    {
        var filter = Builders<ApprovalForm>.Filter.And(
            Builders<ApprovalForm>.Filter.Eq(f => f.IsActive, true),
            Builders<ApprovalForm>.Filter.Eq(f => f.IsDeleted, false)
        );

        var total = await _db.ApprovalForms.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _db.ApprovalForms.Find(filter)
            .SortBy(f => f.SortOrder).ThenBy(f => f.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ApprovalFormDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = (int)total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<ApprovalFormDto> GetFormByIdAsync(string formId, CancellationToken ct = default)
    {
        var form = await _db.ApprovalForms.Find(f => f.Id == formId && f.IsActive && !f.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("ApprovalForm", formId);
        return ToDto(form);
    }

    public async Task<ApprovalFormDto> CreateFormAsync(CreateApprovalFormRequest request, CancellationToken ct = default)
    {
        var form = new ApprovalForm
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            FormSchema = request.FormSchema,
            DefaultApprovalLines = request.DefaultApprovalLine?.Select(l => new Engine.Domain.Approval.DefaultApprovalLine
            {
                Seq = l.Step,
                Role = Enum.Parse<Engine.Domain.Common.Enums.ApprovalLineRole>(l.Role, true),
            }).ToList() ?? [],
            IsActive = true,
            SortOrder = request.SortOrder,
            CreatedBy = _currentUser.UserId,
        };

        await _db.ApprovalForms.InsertOneAsync(form, cancellationToken: ct);
        await _audit.LogAsync("APPROVAL_FORM_CREATE", "ApprovalForm", form.Id, ct: ct);
        return ToDto(form);
    }

    public async Task<ApprovalFormDto> UpdateFormAsync(string formId, UpdateApprovalFormRequest request, CancellationToken ct = default)
    {
        var form = await _db.ApprovalForms.Find(f => f.Id == formId && !f.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("ApprovalForm", formId);

        form.Name = request.Name;
        form.Description = request.Description;
        form.FormSchema = request.FormSchema;
        form.IsActive = request.IsActive;
        form.SortOrder = request.SortOrder;
        form.UpdatedAt = DateTime.UtcNow;
        form.UpdatedBy = _currentUser.UserId;

        await _db.ApprovalForms.ReplaceOneAsync(f => f.Id == formId, form, cancellationToken: ct);
        return ToDto(form);
    }

    public async Task DeleteFormAsync(string formId, CancellationToken ct = default)
    {
        await _db.ApprovalForms.UpdateOneAsync(f => f.Id == formId,
            Builders<ApprovalForm>.Update
                .Set(f => f.IsDeleted, true)
                .Set(f => f.DeletedAt, DateTime.UtcNow)
                .Set(f => f.DeletedBy, _currentUser.UserId),
            cancellationToken: ct);
    }

    private static ApprovalFormDto ToDto(ApprovalForm f) => new()
    {
        Id = f.Id,
        Code = f.Code,
        Name = f.Name,
        Description = f.Description,
        Category = f.Category,
        FormSchema = f.FormSchema,
        IsActive = f.IsActive,
        SortOrder = f.SortOrder,
        DefaultApprovalLine = f.DefaultApprovalLines?.Select(l => new DefaultApprovalLineDto
        {
            Step = l.Seq,
            Role = l.Role.ToString(),
        }).ToList(),
    };
}
