using Engine.Domain.Common.Entities;
using Engine.Domain.Common.Enums;
using Engine.Domain.Common.Events;

namespace Engine.Domain.Approval;

/// <summary>
/// 결재 양식 도메인 엔티티
/// </summary>
public class ApprovalForm : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool IsPublic { get; set; } = true;
    public string? CategoryCode { get; set; }
    /// <summary>관리 편의를 위한 카테고리 이름 (CategoryCode 별칭)</summary>
    public string? Category { get => CategoryCode; set => CategoryCode = value; }
    public int RetentionYears { get; set; } = 5; // 보존연한
    public string DocumentNoRule { get; set; } = "{FORM_CODE}-{YEAR}-{SEQ:6}";
    /// <summary>JSON 형식의 폼 필드 스키마 (동적 폼 렌더링용)</summary>
    public string? FormSchema { get; set; }
    public int SortOrder { get; set; } = 0;
    public List<ApprovalFormField> Fields { get; set; } = new();
    public List<DefaultApprovalLine> DefaultApprovalLines { get; set; } = new();
    /// <summary>기본결재선 (DefaultApprovalLines 별칭)</summary>
    public List<DefaultApprovalLine>? DefaultApprovalLine { get => DefaultApprovalLines; set { if (value != null) DefaultApprovalLines = value; } }
    public string? PostProcessHook { get; set; } // 후처리 훅 식별자
}

public class ApprovalFormField
{
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty; // text, number, date, amount, user, dept, checkbox, table, editor, file
    public string Label { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public string? DefaultValue { get; set; }
    public string? ValidationRule { get; set; }
    public List<ApprovalFormFieldOption> Options { get; set; } = new();
}

public class ApprovalFormFieldOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class DefaultApprovalLine
{
    public int Seq { get; set; }
    public ApprovalLineRole Role { get; set; }
    public string? DepartmentId { get; set; }
    public string? PositionCode { get; set; }
    public bool UseAutoDeptManager { get; set; } = false;
}

/// <summary>
/// 결재 문서 Aggregate - 핵심 도메인
/// </summary>
public class ApprovalDocument : BaseEntity
{
    public string FormId { get; set; } = string.Empty;
    public int FormVersion { get; set; } = 1;
    public string DocumentNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public DrafterInfo Drafter { get; set; } = new();

    public ApprovalDocumentStatus Status { get; set; } = ApprovalDocumentStatus.Draft;

    public Dictionary<string, object?> FormData { get; set; } = new();

    public List<ApprovalLineItem> ApprovalLine { get; set; } = new();

    public List<ReferenceInfo> ReferencesBefore { get; set; } = new(); // 전참조
    public List<ReferenceInfo> ReferencesAfter { get; set; } = new();  // 후참조
    public List<ReceiverInfo> Receivers { get; set; } = new();         // 수신처
    public List<ReviewerInfo> Reviewers { get; set; } = new();         // 공람

    public List<AttachmentInfo> Attachments { get; set; } = new();
    public List<ApprovalHistory> Histories { get; set; } = new();

    public string PostProcessStatus { get; set; } = "Pending"; // Pending, Success, Failed, Skipped
    public string? PostProcessError { get; set; }
    public int PostProcessRetryCount { get; set; } = 0;

    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // 도메인 이벤트 (UoW에서 dispatch)
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    protected void AddDomainEvent(IDomainEvent evt) => _domainEvents.Add(evt);

    // ─── 상태 전이 메서드 ───

    /// <summary>상신 - Draft → Submitted/InApproval</summary>
    public void Submit(string requesterId)
    {
        EnsureStatus(ApprovalDocumentStatus.Draft, ApprovalDocumentStatus.Recalled);
        if (Drafter.UserId != requesterId)
            throw new ApprovalDomainException("기안자만 상신할 수 있습니다.");

        Status = ApprovalDocumentStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;

        ActivateNextApprover();
        AddHistory("SUBMIT", requesterId, "상신");
        AddDomainEvent(new ApprovalDocumentSubmittedEvent(Id, requesterId));
    }

    /// <summary>승인</summary>
    public void Approve(string approverId, string? comment = null)
    {
        var lineItem = GetCurrentPendingApprover(approverId);

        lineItem.Status = ApproverStatus.Approved;
        lineItem.ActedAt = DateTime.UtcNow;
        lineItem.Comment = comment;

        AddHistory("APPROVE", approverId, comment ?? "승인", lineItem.Seq);
        AddDomainEvent(new ApprovalDocumentApprovedEvent(Id, approverId, lineItem.Seq));

        MoveToNextStep();
    }

    /// <summary>합의승인</summary>
    public void Agree(string approverId, string? comment = null)
    {
        var lineItem = GetCurrentPendingApprover(approverId, ApprovalLineRole.Agreement);

        lineItem.Status = ApproverStatus.Agreed;
        lineItem.ActedAt = DateTime.UtcNow;
        lineItem.Comment = comment;

        AddHistory("AGREE", approverId, comment ?? "합의승인", lineItem.Seq);
        MoveToNextStep();
    }

    /// <summary>협의 - 문서 상태에 영향을 주지 않음</summary>
    public void Consult(string consulterId, bool agree, string? comment = null)
    {
        var lineItem = ApprovalLine.FirstOrDefault(l =>
            l.UserId == consulterId &&
            l.Role == ApprovalLineRole.Consultation &&
            l.Status == ApproverStatus.Pending)
            ?? throw new ApprovalDomainException("해당 협의자를 찾을 수 없습니다.");

        lineItem.Status = agree ? ApproverStatus.ConsultedAgree : ApproverStatus.ConsultedObject;
        lineItem.ActedAt = DateTime.UtcNow;
        lineItem.Comment = comment;

        AddHistory(agree ? "CONSULT_AGREE" : "CONSULT_OBJECT", consulterId, comment ?? (agree ? "협의동의" : "협의이의"), lineItem.Seq);
        // 협의는 상태 전이 없음
    }

    /// <summary>반려</summary>
    public void Reject(string approverId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ApprovalDomainException("반려 사유를 입력해야 합니다.");

        var lineItem = ApprovalLine.FirstOrDefault(l =>
            l.UserId == approverId &&
            l.Status == ApproverStatus.Pending &&
            l.Role is ApprovalLineRole.Approval or ApprovalLineRole.Agreement)
            ?? throw new ApprovalDomainException("결재 권한이 없습니다.");

        lineItem.Status = ApproverStatus.Rejected;
        lineItem.ActedAt = DateTime.UtcNow;
        lineItem.Comment = reason;

        Status = ApprovalDocumentStatus.Rejected;
        AddHistory("REJECT", approverId, reason, lineItem.Seq);
        AddDomainEvent(new ApprovalDocumentRejectedEvent(Id, approverId, reason));
    }

    /// <summary>전결승인 - 즉시 완료</summary>
    public void FinalApprove(string approverId, string? comment = null)
    {
        var lineItem = ApprovalLine.FirstOrDefault(l =>
            l.UserId == approverId &&
            l.Status == ApproverStatus.Pending &&
            l.IsFinalApprovalAllowed)
            ?? throw new ApprovalDomainException("전결 권한이 없습니다.");

        lineItem.Status = ApproverStatus.Approved;
        lineItem.ActedAt = DateTime.UtcNow;
        lineItem.Comment = comment ?? "전결";

        // 이후 대기 결재자 모두 Skipped 처리
        foreach (var remaining in ApprovalLine.Where(l => l.Seq > lineItem.Seq && l.Status == ApproverStatus.Pending))
            remaining.Status = ApproverStatus.Skipped;

        Complete("전결승인 완료");
        AddHistory("FINAL_APPROVE", approverId, comment ?? "전결", lineItem.Seq);
    }

    /// <summary>회수 - Submitted 이후 첫 결재 처리 전</summary>
    public void Recall(string requesterId)
    {
        EnsureStatus(ApprovalDocumentStatus.Submitted, ApprovalDocumentStatus.InApproval);

        if (Drafter.UserId != requesterId)
            throw new ApprovalDomainException("기안자만 회수할 수 있습니다.");

        // 이미 처리된 결재자가 있으면 회수 불가
        if (ApprovalLine.Any(l => l.Status != ApproverStatus.Pending && l.Role == ApprovalLineRole.Approval))
            throw new ApprovalDomainException("이미 처리된 결재 문서는 회수할 수 없습니다.");

        Status = ApprovalDocumentStatus.Recalled;
        AddHistory("RECALL", requesterId, "회수");
        AddDomainEvent(new ApprovalDocumentRecalledEvent(Id, requesterId));
    }

    /// <summary>대결처리</summary>
    public void DelegateApprove(string delegateeId, string originalUserId, string? comment = null)
    {
        var lineItem = ApprovalLine.FirstOrDefault(l =>
            l.UserId == originalUserId &&
            l.Status == ApproverStatus.Pending)
            ?? throw new ApprovalDomainException("대결 대상 결재자를 찾을 수 없습니다.");

        lineItem.Status = ApproverStatus.Delegated;
        lineItem.ActedAt = DateTime.UtcNow;
        lineItem.Comment = comment;
        lineItem.DelegatedByUserId = delegateeId;

        AddHistory("DELEGATE_APPROVE", delegateeId, $"대결처리 (원결재자: {originalUserId})", lineItem.Seq);
        MoveToNextStep();
    }

    // ─── 내부 헬퍼 ───

    private void EnsureStatus(params ApprovalDocumentStatus[] allowedStatuses)
    {
        if (!allowedStatuses.Contains(Status))
            throw new ApprovalDomainException($"현재 상태({Status})에서는 해당 작업을 수행할 수 없습니다.");
    }

    private ApprovalLineItem GetCurrentPendingApprover(string userId,
        ApprovalLineRole role = ApprovalLineRole.Approval)
    {
        return ApprovalLine.FirstOrDefault(l =>
               l.UserId == userId &&
               l.Role == role &&
               l.Status == ApproverStatus.Pending)
            ?? throw new ApprovalDomainException("결재 권한이 없거나 이미 처리된 문서입니다.");
    }

    private void ActivateNextApprover()
    {
        // 현재 활성화해야 할 결재/합의 참여자를 찾아 Pending으로 만든다
        var firstPending = ApprovalLine
            .Where(l => l.Role is ApprovalLineRole.Approval or ApprovalLineRole.Agreement)
            .OrderBy(l => l.Seq)
            .FirstOrDefault();

        if (firstPending != null)
        {
            Status = firstPending.Role == ApprovalLineRole.Agreement
                ? ApprovalDocumentStatus.InAgreement
                : ApprovalDocumentStatus.InApproval;
        }
    }

    private void MoveToNextStep()
    {
        var nextPending = ApprovalLine
            .Where(l => l.Role is ApprovalLineRole.Approval or ApprovalLineRole.Agreement
                        && l.Status == ApproverStatus.Pending)
            .OrderBy(l => l.Seq)
            .FirstOrDefault();

        if (nextPending == null)
        {
            Complete("결재 완료");
        }
        else
        {
            Status = nextPending.Role == ApprovalLineRole.Agreement
                ? ApprovalDocumentStatus.InAgreement
                : ApprovalDocumentStatus.InApproval;
        }
    }

    private void Complete(string remark)
    {
        Status = ApprovalDocumentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        AddHistory("COMPLETE", Drafter.UserId, remark);
        AddDomainEvent(new ApprovalDocumentCompletedEvent(Id));
    }

    private void AddHistory(string action, string actorId, string? remark = null, int? seq = null)
    {
        Histories.Add(new ApprovalHistory
        {
            Action = action,
            ActorUserId = actorId,
            Remark = remark,
            ApprovalSeq = seq,
            ActedAt = DateTime.UtcNow
        });
    }
}

public class DrafterInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
}

public class ApprovalLineItem
{
    public int Seq { get; set; }
    public ApprovalLineRole Role { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public ApproverStatus Status { get; set; } = ApproverStatus.Pending;
    public DateTime? ActedAt { get; set; }
    public string? Comment { get; set; }
    public bool IsFinalApprovalAllowed { get; set; } = false; // 전결 허용 여부
    public string? DelegatedByUserId { get; set; }
}

public class ReferenceInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}

public class ReceiverInfo
{
    public string? DepartmentId { get; set; }
    public string? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
}

public class ReviewerInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool HasReviewed { get; set; } = false;
    public DateTime? ReviewedAt { get; set; }
}

public class AttachmentInfo
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public class ApprovalHistory
{
    public string Action { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public int? ApprovalSeq { get; set; }
    public string? Remark { get; set; }
    public DateTime ActedAt { get; set; }
}

public class ApprovalDomainException : Exception
{
    public ApprovalDomainException(string message) : base(message) { }
}
