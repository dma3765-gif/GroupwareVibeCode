using System.Linq.Expressions;

namespace Engine.Application.Common.Interfaces;

/// <summary>기본 Repository 인터페이스</summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<T>> GetAllAsync(CancellationToken ct = default);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default);
    Task InsertAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(string id, T entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<long> CountAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default);
}

/// <summary>Unit of Work 인터페이스</summary>
public interface IUnitOfWork : IDisposable
{
    Task<bool> CommitAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}

/// <summary>현재 사용자 컨텍스트</summary>
public interface ICurrentUserContext
{
    string UserId { get; }
    string Name { get; }
    string DepartmentId { get; }
    string DepartmentName { get; }
    string PositionName { get; }
    string TenantId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool HasPermission(string permission);
}

/// <summary>캐시 서비스</summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}

/// <summary>감사 로그 서비스</summary>
public interface IAuditLogService
{
    Task LogAsync(string action, string resourceType, string? resourceId,
        bool isSuccess = true, string? failureReason = null,
        object? before = null, object? after = null,
        CancellationToken ct = default);
}

/// <summary>알림 발행 서비스</summary>
public interface INotificationPublisher
{
    Task PublishAsync(string recipientUserId, Engine.Domain.Common.Enums.NotificationType type,
        string title, string message, string? resourceType = null, string? resourceId = null,
        CancellationToken ct = default);
}

/// <summary>파일 스토리지 서비스</summary>
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string storagePath, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}

/// <summary>이메일 서비스</summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
