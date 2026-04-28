using Engine.Application.Common.DTOs;
using Engine.Application.Common.Responses;
using Engine.Domain.Common.Enums;

namespace Engine.Application.Approval;

// ─── Form DTOs ───

public class ApprovalFormDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? FormSchema { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public List<DefaultApprovalLineDto>? DefaultApprovalLine { get; set; }
}

public class DefaultApprovalLineDto
{
    public int Step { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
}

public class CreateApprovalFormRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? FormSchema { get; set; }
    public int SortOrder { get; set; }
    public List<DefaultApprovalLineDto>? DefaultApprovalLine { get; set; }
}

public class UpdateApprovalFormRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FormSchema { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

// ─── Document DTOs ───

public class ApprovalDocumentDto
{
    public string Id { get; set; } = string.Empty;
    public string FormId { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public string DocumentNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DrafterDto Drafter { get; set; } = new();
    public ApprovalDocumentStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public Dictionary<string, object?> FormData { get; set; } = new();
    public List<ApprovalLineItemDto> ApprovalLine { get; set; } = new();
    public List<ApprovalHistoryDto> Histories { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class DrafterDto
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
}

public class ApprovalLineItemDto
{
    public int Seq { get; set; }
    public string Role { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ActedAt { get; set; }
    public string? Comment { get; set; }
    public bool IsFinalApprovalAllowed { get; set; }
}

public class ApprovalHistoryDto
{
    public string Action { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime ActedAt { get; set; }
}

public class ApprovalDocumentSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string DocumentNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public string DrafterName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public ApprovalDocumentStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

// ─── Request DTOs ───

public class DraftDocumentRequest
{
    public string FormId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Dictionary<string, object?> FormData { get; set; } = new();
    public List<ApprovalLineRequest> ApprovalLine { get; set; } = new();
    public List<string> ReferenceBeforeUserIds { get; set; } = new();
    public List<string> ReferenceAfterUserIds { get; set; } = new();
}

public class ApprovalLineRequest
{
    public int Seq { get; set; }
    public string Role { get; set; } = "Approval"; // Approval, Agreement, Consultation
    public string UserId { get; set; } = string.Empty;
    public bool IsFinalApprovalAllowed { get; set; } = false;
}

public class ApproveRequest
{
    public string? Comment { get; set; }
}

public class RejectRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class RecallRequest
{
    public string? Reason { get; set; }
}

public class ConsultRequest
{
    public bool Agree { get; set; } = true;
    public string? Comment { get; set; }
}

public class ApprovalBoxQuery : PagedRequest
{
    public ApprovalDocumentStatus? Status { get; set; }
    public string? FormId { get; set; }
    public string? DrafterId { get; set; }
}

// ─── Service Interface ───

public interface IApprovalFormService
{
    Task<PagedResult<ApprovalFormDto>> GetFormsAsync(PagedRequest request, CancellationToken ct = default);
    Task<ApprovalFormDto> GetFormByIdAsync(string id, CancellationToken ct = default);
    Task<ApprovalFormDto> CreateFormAsync(CreateApprovalFormRequest request, CancellationToken ct = default);
    Task<ApprovalFormDto> UpdateFormAsync(string id, UpdateApprovalFormRequest request, CancellationToken ct = default);
    Task DeleteFormAsync(string id, CancellationToken ct = default);
}

public interface IApprovalDocumentService
{
    Task<ApprovalDocumentDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ApprovalDocumentDto> DraftAsync(DraftDocumentRequest request, CancellationToken ct = default);
    Task<ApprovalDocumentDto> SubmitAsync(string id, CancellationToken ct = default);
    Task<ApprovalDocumentDto> ApproveAsync(string id, ApproveRequest request, CancellationToken ct = default);
    Task<ApprovalDocumentDto> AgreeAsync(string id, ApproveRequest request, CancellationToken ct = default);
    Task<ApprovalDocumentDto> ConsultAsync(string id, ConsultRequest request, CancellationToken ct = default);
    Task<ApprovalDocumentDto> RejectAsync(string id, RejectRequest request, CancellationToken ct = default);
    Task<ApprovalDocumentDto> RecallAsync(string id, RecallRequest request, CancellationToken ct = default);
    Task<ApprovalDocumentDto> FinalApproveAsync(string id, ApproveRequest request, CancellationToken ct = default);

    // 결재함 조회
    Task<PagedResult<ApprovalDocumentSummaryDto>> GetMyDraftsAsync(ApprovalBoxQuery query, CancellationToken ct = default);
    Task<PagedResult<ApprovalDocumentSummaryDto>> GetMyPendingAsync(ApprovalBoxQuery query, CancellationToken ct = default);
    Task<PagedResult<ApprovalDocumentSummaryDto>> GetMyInProgressAsync(ApprovalBoxQuery query, CancellationToken ct = default);
    Task<PagedResult<ApprovalDocumentSummaryDto>> GetMyCompletedAsync(ApprovalBoxQuery query, CancellationToken ct = default);
    Task<PagedResult<ApprovalDocumentSummaryDto>> GetMyRejectedAsync(ApprovalBoxQuery query, CancellationToken ct = default);
    Task<PagedResult<ApprovalDocumentSummaryDto>> GetDepartmentReceivedAsync(ApprovalBoxQuery query, CancellationToken ct = default);
}

