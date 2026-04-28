using Engine.Application.Approval;
using Engine.Application.Common.DTOs;
using Engine.Application.Common.Exceptions;
using Engine.Application.Common.Interfaces;
using Engine.Application.Common.Responses;
using Engine.Domain.Approval;
using Engine.Domain.Common.Enums;
using Engine.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace Engine.Infrastructure.Services;

public class ApprovalDocumentServiceImpl : IApprovalDocumentService
{
    private readonly GroupwareDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly INotificationPublisher _notifier;
    private readonly IAuditLogService _audit;

    public ApprovalDocumentServiceImpl(
        GroupwareDbContext db,
        ICurrentUserContext currentUser,
        INotificationPublisher notifier,
        IAuditLogService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
        _audit = audit;
    }

    public async Task<ApprovalDocumentDto> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var doc = await _db.ApprovalDocuments.Find(d => d.Id == id && !d.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("ApprovalDocument", id);

        // 권한 확인: 기안자, 결재선 참여자, 관리자만 조회 가능
        if (!CanView(doc))
            throw new ForbiddenException("결재 문서 조회 권한이 없습니다.");

        await _audit.LogAsync("APPROVAL_VIEW", "ApprovalDocument", id, ct: ct);
        return await ToDetailDtoAsync(doc, ct);
    }

    public async Task<ApprovalDocumentDto> DraftAsync(DraftDocumentRequest request, CancellationToken ct = default)
    {
        var form = await _db.ApprovalForms.Find(f => f.Id == request.FormId && f.IsActive && !f.IsDeleted)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("ApprovalForm", request.FormId);

        var doc = new ApprovalDocument
        {
            FormId = request.FormId,
            FormVersion = form.Version,
            Title = request.Title,
            FormData = request.FormData,
            Drafter = new DrafterInfo
            {
                UserId = _currentUser.UserId,
                Name = _currentUser.Name,
                DepartmentId = _currentUser.DepartmentId,
                DepartmentName = _currentUser.DepartmentName,
                PositionName = _currentUser.PositionName,
            },
            Status = ApprovalDocumentStatus.Draft,
            ApprovalLine = request.ApprovalLine.Select(l => new ApprovalLineItem
            {
                Seq = l.Seq,
                Role = Enum.Parse<ApprovalLineRole>(l.Role),
                UserId = l.UserId,
                IsFinalApprovalAllowed = l.IsFinalApprovalAllowed,
            }).ToList(),
            CreatedBy = _currentUser.UserId,
        };

        // 결재선 사용자 정보 보강
        await EnrichApprovalLineAsync(doc.ApprovalLine, ct);

        // 참조자 설정
        foreach (var uid in request.ReferenceBeforeUserIds)
        {
            var u = await _db.Users.Find(x => x.Id == uid).FirstOrDefaultAsync(ct);
            if (u != null) doc.ReferencesBefore.Add(new ReferenceInfo { UserId = uid, Name = u.Name });
        }
        foreach (var uid in request.ReferenceAfterUserIds)
        {
            var u = await _db.Users.Find(x => x.Id == uid).FirstOrDefaultAsync(ct);
            if (u != null) doc.ReferencesAfter.Add(new ReferenceInfo { UserId = uid, Name = u.Name });
        }

        await _db.ApprovalDocuments.InsertOneAsync(doc, cancellationToken: ct);
        await _audit.LogAsync("APPROVAL_DRAFT", "ApprovalDocument", doc.Id, ct: ct);

        return await ToDetailDtoAsync(doc, ct);
    }

    public async Task<ApprovalDocumentDto> SubmitAsync(string id, CancellationToken ct = default)
    {
        var doc = await GetDocumentForUpdateAsync(id, ct);

        // 문서 번호 생성
        if (string.IsNullOrEmpty(doc.DocumentNo))
            doc.DocumentNo = await GenerateDocumentNoAsync(doc.FormId, ct);

        doc.Submit(_currentUser.UserId);
        doc.UpdatedAt = DateTime.UtcNow;
        doc.UpdatedBy = _currentUser.UserId;

        await SaveAsync(doc, ct);

        // 첫 번째 결재 대기자에게 알림
        var firstApprover = doc.ApprovalLine
            .Where(l => l.Role is ApprovalLineRole.Approval or ApprovalLineRole.Agreement)
            .OrderBy(l => l.Seq).FirstOrDefault();

        if (firstApprover != null)
        {
            await _notifier.PublishAsync(firstApprover.UserId, NotificationType.ApprovalRequested,
                "결재 요청", $"'{doc.Title}' 문서의 결재 요청이 있습니다.",
                "ApprovalDocument", doc.Id, ct);
        }

        // 전참조자 알림
        foreach (var ref_ in doc.ReferencesBefore)
            await _notifier.PublishAsync(ref_.UserId, NotificationType.ApprovalReferenced,
                "결재 참조", $"'{doc.Title}' 문서가 참조로 지정되었습니다.",
                "ApprovalDocument", doc.Id, ct);

        await _audit.LogAsync("APPROVAL_SUBMIT", "ApprovalDocument", id, ct: ct);
        return await ToDetailDtoAsync(doc, ct);
    }

    public async Task<ApprovalDocumentDto> ApproveAsync(string id, ApproveRequest request, CancellationToken ct = default)
    {
        var doc = await GetDocumentForUpdateAsync(id, ct);
        doc.Approve(_currentUser.UserId, request.Comment);
        doc.UpdatedAt = DateTime.UtcNow;

        await SaveAsync(doc, ct);
        await SendNextApproverNotificationAsync(doc, ct);

        if (doc.Status == ApprovalDocumentStatus.Completed)
            await HandleCompletionAsync(doc, ct);

        await _audit.LogAsync("APPROVAL_APPROVE", "ApprovalDocument", id, ct: ct);
        return await ToDetailDtoAsync(doc, ct);
    }

    public async Task<ApprovalDocumentDto> AgreeAsync(string id, ApproveRequest request, CancellationToken ct = default)
    {
        var doc = await GetDocumentForUpdateAsync(id, ct);
        doc.Agree(_currentUser.UserId, request.Comment);
        doc.UpdatedAt = DateTime.UtcNow;

        await SaveAsync(doc, ct);
        await SendNextApproverNotificationAsync(doc, ct);

        if (doc.Status == ApprovalDocumentStatus.Completed)
            await HandleCompletionAsync(doc, ct);

        await _audit.LogAsync("APPROVAL_AGREE", "ApprovalDocument", id, ct: ct);
        return await ToDetailDtoAsync(doc, ct);
    }

    public async Task<ApprovalDocumentDto> ConsultAsync(string id, ConsultRequest request, CancellationToken ct = default)
    {
        var doc = await GetDocumentForUpdateAsync(id, ct);
        doc.Consult(_currentUser.UserId, request.Agree, request.Comment);
        doc.UpdatedAt = DateTime.UtcNow;

        await SaveAsync(doc, ct);
        await _audit.LogAsync("APPROVAL_CONSULT", "ApprovalDocument", id, ct: ct);
        return await ToDetailDtoAsync(doc, ct);
    }

    public async Task<ApprovalDocumentDto> RejectAsync(string id, RejectRequest request, CancellationToken ct = default)
    {
        var doc = await GetDocumentForUpdateAsync(id, ct);
        doc.Reject(_currentUser.UserId, request.Reason);
        doc.UpdatedAt = DateTime.UtcNow;

        await SaveAsync(doc, ct);

        // 기안자에게 반려 알림
        await _notifier.PublishAsync(doc.Drafter.UserId, NotificationType.ApprovalRejected,
            "결재 반려", $"'{doc.Title}' 문서가 반려되었습니다.\n사유: {request.Reason}",
            "ApprovalDocument", doc.Id, ct);

        await _audit.LogAsync("APPROVAL_REJECT", "ApprovalDocument", id, ct: ct);
        return await ToDetailDtoAsync(doc, ct);
    }

    public async Task<ApprovalDocumentDto> RecallAsync(string id, RecallRequest request, CancellationToken ct = default)
    {
        var doc = await GetDocumentForUpdateAsync(id, ct);
        doc.Recall(_currentUser.UserId);
        doc.UpdatedAt = DateTime.UtcNow;

        await SaveAsync(doc, ct);
        await _audit.LogAsync("APPROVAL_RECALL", "ApprovalDocument", id, ct: ct);
        return await ToDetailDtoAsync(doc, ct);
    }

    public async Task<ApprovalDocumentDto> FinalApproveAsync(string id, ApproveRequest request, CancellationToken ct = default)
    {
        var doc = await GetDocumentForUpdateAsync(id, ct);
        doc.FinalApprove(_currentUser.UserId, request.Comment);
        doc.UpdatedAt = DateTime.UtcNow;

        await SaveAsync(doc, ct);
        await HandleCompletionAsync(doc, ct);
        await _audit.LogAsync("APPROVAL_FINAL_APPROVE", "ApprovalDocument", id, ct: ct);
        return await ToDetailDtoAsync(doc, ct);
    }

    public async Task<ApprovalDocumentDto> DelegateApproveAsync(string id, DelegateApproveRequest request, CancellationToken ct = default)
    {
        var doc = await GetDocumentForUpdateAsync(id, ct);
        doc.DelegateApprove(_currentUser.UserId, request.OriginalUserId, request.Comment);
        doc.UpdatedAt = DateTime.UtcNow;

        await SaveAsync(doc, ct);
        await SendNextApproverNotificationAsync(doc, ct);

        if (doc.Status == ApprovalDocumentStatus.Completed)
            await HandleCompletionAsync(doc, ct);

        await _audit.LogAsync("APPROVAL_DELEGATE", "ApprovalDocument", id, ct: ct);
        return await ToDetailDtoAsync(doc, ct);
    }

    // ─── 결재함 조회 ───

    public async Task<PagedResult<ApprovalDocumentSummaryDto>> GetMyDraftsAsync(ApprovalBoxQuery query, CancellationToken ct = default)
    {
        var filter = Builders<ApprovalDocument>.Filter.And(
            Builders<ApprovalDocument>.Filter.Eq("Drafter.UserId", _currentUser.UserId),
            Builders<ApprovalDocument>.Filter.Eq(d => d.IsDeleted, false));

        return await QueryPagedAsync(filter, query, ct);
    }

    public async Task<PagedResult<ApprovalDocumentSummaryDto>> GetMyPendingAsync(ApprovalBoxQuery query, CancellationToken ct = default)
    {
        var filter = Builders<ApprovalDocument>.Filter.And(
            Builders<ApprovalDocument>.Filter.ElemMatch(d => d.ApprovalLine,
                l => l.UserId == _currentUser.UserId && l.Status == ApproverStatus.Pending),
            Builders<ApprovalDocument>.Filter.In(d => d.Status,
                new[] { ApprovalDocumentStatus.InApproval, ApprovalDocumentStatus.InAgreement }),
            Builders<ApprovalDocument>.Filter.Eq(d => d.IsDeleted, false));

        return await QueryPagedAsync(filter, query, ct);
    }

    public async Task<PagedResult<ApprovalDocumentSummaryDto>> GetMyInProgressAsync(ApprovalBoxQuery query, CancellationToken ct = default)
    {
        var filter = Builders<ApprovalDocument>.Filter.And(
            Builders<ApprovalDocument>.Filter.Eq("Drafter.UserId", _currentUser.UserId),
            Builders<ApprovalDocument>.Filter.In(d => d.Status,
                new[] { ApprovalDocumentStatus.Submitted, ApprovalDocumentStatus.InApproval, ApprovalDocumentStatus.InAgreement }),
            Builders<ApprovalDocument>.Filter.Eq(d => d.IsDeleted, false));

        return await QueryPagedAsync(filter, query, ct);
    }

    public async Task<PagedResult<ApprovalDocumentSummaryDto>> GetMyCompletedAsync(ApprovalBoxQuery query, CancellationToken ct = default)
    {
        var filter = Builders<ApprovalDocument>.Filter.And(
            Builders<ApprovalDocument>.Filter.Eq("Drafter.UserId", _currentUser.UserId),
            Builders<ApprovalDocument>.Filter.Eq(d => d.Status, ApprovalDocumentStatus.Completed),
            Builders<ApprovalDocument>.Filter.Eq(d => d.IsDeleted, false));

        return await QueryPagedAsync(filter, query, ct);
    }

    public async Task<PagedResult<ApprovalDocumentSummaryDto>> GetMyRejectedAsync(ApprovalBoxQuery query, CancellationToken ct = default)
    {
        var filter = Builders<ApprovalDocument>.Filter.And(
            Builders<ApprovalDocument>.Filter.Eq("Drafter.UserId", _currentUser.UserId),
            Builders<ApprovalDocument>.Filter.Eq(d => d.Status, ApprovalDocumentStatus.Rejected),
            Builders<ApprovalDocument>.Filter.Eq(d => d.IsDeleted, false));

        return await QueryPagedAsync(filter, query, ct);
    }

    public async Task<PagedResult<ApprovalDocumentSummaryDto>> GetDepartmentReceivedAsync(
        ApprovalBoxQuery query, CancellationToken ct = default)
    {
        var deptId = _currentUser.DepartmentId;
        var filter = Builders<ApprovalDocument>.Filter.And(
            Builders<ApprovalDocument>.Filter.ElemMatch(d => d.Receivers,
                r => r.DepartmentId == deptId),
            Builders<ApprovalDocument>.Filter.Eq(d => d.Status, ApprovalDocumentStatus.Completed),
            Builders<ApprovalDocument>.Filter.Eq(d => d.IsDeleted, false));

        return await QueryPagedAsync(filter, query, ct);
    }

    // ─── 내부 헬퍼 ───

    private bool CanView(ApprovalDocument doc)
    {
        var userId = _currentUser.UserId;
        return doc.Drafter.UserId == userId
            || doc.ApprovalLine.Any(l => l.UserId == userId)
            || doc.ReferencesBefore.Any(r => r.UserId == userId)
            || doc.ReferencesAfter.Any(r => r.UserId == userId)
            || _currentUser.IsInRole("ApprovalAdmin")
            || _currentUser.IsInRole("SystemAdmin");
    }

    private async Task<ApprovalDocument> GetDocumentForUpdateAsync(string id, CancellationToken ct)
    {
        return await _db.ApprovalDocuments.Find(d => d.Id == id && !d.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("ApprovalDocument", id);
    }

    private async Task SaveAsync(ApprovalDocument doc, CancellationToken ct)
    {
        await _db.ApprovalDocuments.ReplaceOneAsync(d => d.Id == doc.Id, doc, cancellationToken: ct);
        doc.ClearDomainEvents();
    }

    private async Task SendNextApproverNotificationAsync(ApprovalDocument doc, CancellationToken ct)
    {
        if (doc.Status is ApprovalDocumentStatus.InApproval or ApprovalDocumentStatus.InAgreement)
        {
            var nextApprover = doc.ApprovalLine
                .Where(l => l.Role is ApprovalLineRole.Approval or ApprovalLineRole.Agreement
                    && l.Status == ApproverStatus.Pending)
                .OrderBy(l => l.Seq).FirstOrDefault();

            if (nextApprover != null)
                await _notifier.PublishAsync(nextApprover.UserId, NotificationType.ApprovalRequested,
                    "결재 요청", $"'{doc.Title}' 문서의 결재 요청이 있습니다.",
                    "ApprovalDocument", doc.Id, ct);
        }
    }

    private async Task HandleCompletionAsync(ApprovalDocument doc, CancellationToken ct)
    {
        // 기안자에게 완료 알림
        await _notifier.PublishAsync(doc.Drafter.UserId, NotificationType.ApprovalCompleted,
            "결재 완료", $"'{doc.Title}' 문서가 최종 결재 완료되었습니다.",
            "ApprovalDocument", doc.Id, ct);

        // 후참조자 알림
        foreach (var ref_ in doc.ReferencesAfter)
            await _notifier.PublishAsync(ref_.UserId, NotificationType.ApprovalReferenced,
                "결재 참조", $"'{doc.Title}' 문서가 완료되어 참조로 지정되었습니다.",
                "ApprovalDocument", doc.Id, ct);
    }

    private async Task EnrichApprovalLineAsync(List<ApprovalLineItem> line, CancellationToken ct)
    {
        foreach (var item in line)
        {
            var user = await _db.Users.Find(u => u.Id == item.UserId).FirstOrDefaultAsync(ct);
            if (user != null)
            {
                item.Name = user.Name;
                item.DepartmentName = user.DepartmentName;
                item.PositionName = user.PositionName;
            }
        }
    }

    private async Task<string> GenerateDocumentNoAsync(string formId, CancellationToken ct)
    {
        var form = await _db.ApprovalForms.Find(f => f.Id == formId).FirstOrDefaultAsync(ct);
        var year = DateTime.UtcNow.Year;
        var count = await _db.ApprovalDocuments.CountDocumentsAsync(
            d => d.FormId == formId && d.CreatedAt.Year == year, cancellationToken: ct);

        return $"{(form?.Code ?? "DOC")}-{year}-{count + 1:D6}";
    }

    private async Task<PagedResult<ApprovalDocumentSummaryDto>> QueryPagedAsync(
        FilterDefinition<ApprovalDocument> filter, ApprovalBoxQuery query, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            filter &= Builders<ApprovalDocument>.Filter.Regex(d => d.Title,
                new MongoDB.Bson.BsonRegularExpression(query.Keyword, "i"));

        if (query.Status.HasValue)
            filter &= Builders<ApprovalDocument>.Filter.Eq(d => d.Status, query.Status.Value);

        var total = await _db.ApprovalDocuments.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _db.ApprovalDocuments.Find(filter)
            .SortByDescending(d => d.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Limit(query.PageSize)
            .ToListAsync(ct);

        var formIds = items.Select(i => i.FormId).Distinct().ToList();
        var forms = await _db.ApprovalForms.Find(f => formIds.Contains(f.Id)).ToListAsync(ct);
        var formMap = forms.ToDictionary(f => f.Id, f => f.Name);

        return new PagedResult<ApprovalDocumentSummaryDto>
        {
            Items = items.Select(d => new ApprovalDocumentSummaryDto
            {
                Id = d.Id,
                DocumentNo = d.DocumentNo,
                Title = d.Title,
                FormName = formMap.GetValueOrDefault(d.FormId, string.Empty),
                DrafterName = d.Drafter.Name,
                DepartmentName = d.Drafter.DepartmentName,
                Status = d.Status,
                StatusLabel = GetStatusLabel(d.Status),
                CreatedAt = d.CreatedAt,
                SubmittedAt = d.SubmittedAt,
                CompletedAt = d.CompletedAt
            }).ToList(),
            TotalCount = (int)total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    private static string GetStatusLabel(ApprovalDocumentStatus status) => status switch
    {
        ApprovalDocumentStatus.Draft => "임시저장",
        ApprovalDocumentStatus.Submitted => "상신",
        ApprovalDocumentStatus.InApproval => "결재진행",
        ApprovalDocumentStatus.InAgreement => "합의진행",
        ApprovalDocumentStatus.InConsultation => "협의진행",
        ApprovalDocumentStatus.Rejected => "반려",
        ApprovalDocumentStatus.Recalled => "회수",
        ApprovalDocumentStatus.Canceled => "취소",
        ApprovalDocumentStatus.Completed => "완료",
        ApprovalDocumentStatus.PostApproved => "후결완료",
        ApprovalDocumentStatus.Archived => "보관",
        _ => status.ToString()
    };

    private async Task<ApprovalDocumentDto> ToDetailDtoAsync(ApprovalDocument doc, CancellationToken ct)
    {
        var form = await _db.ApprovalForms.Find(f => f.Id == doc.FormId).FirstOrDefaultAsync(ct);
        return new ApprovalDocumentDto
        {
            Id = doc.Id,
            FormId = doc.FormId,
            FormName = form?.Name ?? string.Empty,
            DocumentNo = doc.DocumentNo,
            Title = doc.Title,
            Drafter = new DrafterDto
            {
                UserId = doc.Drafter.UserId,
                Name = doc.Drafter.Name,
                DepartmentName = doc.Drafter.DepartmentName,
                PositionName = doc.Drafter.PositionName,
            },
            Status = doc.Status,
            StatusLabel = GetStatusLabel(doc.Status),
            FormData = doc.FormData,
            ApprovalLine = doc.ApprovalLine.Select(l => new ApprovalLineItemDto
            {
                Seq = l.Seq,
                Role = l.Role.ToString(),
                UserId = l.UserId,
                Name = l.Name,
                DepartmentName = l.DepartmentName,
                PositionName = l.PositionName,
                Status = l.Status.ToString(),
                ActedAt = l.ActedAt,
                Comment = l.Comment,
                IsFinalApprovalAllowed = l.IsFinalApprovalAllowed,
            }).ToList(),
            Histories = doc.Histories.Select(h => new ApprovalHistoryDto
            {
                Action = h.Action,
                ActorUserId = h.ActorUserId,
                Remark = h.Remark,
                ActedAt = h.ActedAt,
            }).ToList(),
            CreatedAt = doc.CreatedAt,
            SubmittedAt = doc.SubmittedAt,
            CompletedAt = doc.CompletedAt,
        };
    }
}
