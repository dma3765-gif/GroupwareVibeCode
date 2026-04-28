using Engine.Domain.Common.Enums;

namespace Engine.Domain.Common.Events;

/// <summary>도메인 이벤트 기반 인터페이스</summary>
public interface IDomainEvent
{
    string EventId { get; }
    DateTime OccurredAt { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

// ─── 전자결재 도메인 이벤트 ───
public record ApprovalDocumentSubmittedEvent(string DocumentId, string DrafterId) : DomainEvent;
public record ApprovalDocumentApprovedEvent(string DocumentId, string ApproverId, int Seq) : DomainEvent;
public record ApprovalDocumentRejectedEvent(string DocumentId, string ApproverId, string Reason) : DomainEvent;
public record ApprovalDocumentCompletedEvent(string DocumentId) : DomainEvent;
public record ApprovalDocumentRecalledEvent(string DocumentId, string DrafterId) : DomainEvent;

// ─── 알림 도메인 이벤트 ───
public record NotificationCreatedEvent(string UserId, NotificationType Type, string Message, string? ResourceId) : DomainEvent;
