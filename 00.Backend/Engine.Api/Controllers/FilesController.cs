using Engine.Application.Common.Exceptions;
using Engine.Application.Common.Interfaces;
using Engine.Application.Common.Responses;
using Engine.Domain.Security;
using Engine.Infrastructure.Persistence.Mongo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Engine.Api.Controllers;

/// <summary>파일 업로드/다운로드 API</summary>
[Authorize]
[Route("api/files")]
public class FilesController : BaseController
{
    private readonly IFileStorageService _storage;
    private readonly GroupwareDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogService _audit;

    // 허용 확장자
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".csv", ".zip", ".hwp", ".hwpx"
    };

    private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100MB

    public FilesController(
        IFileStorageService storage,
        GroupwareDbContext db,
        ICurrentUserContext currentUser,
        IAuditLogService audit)
    {
        _storage = storage;
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    /// <summary>파일 업로드 (단일)</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(104857600)] // 100MB
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string? entityType, [FromQuery] string? entityId, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("FILE_EMPTY", "업로드할 파일이 없습니다."));

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(ApiResponse<object>.Fail("FILE_TOO_LARGE", $"파일 크기는 최대 {MaxFileSizeBytes / 1024 / 1024}MB 입니다."));

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(ApiResponse<object>.Fail("FILE_EXTENSION_NOT_ALLOWED", $"허용되지 않는 파일 형식입니다: {ext}"));

        await using var stream = file.OpenReadStream();
        var storagePath = await _storage.UploadAsync(stream, file.FileName, file.ContentType, ct);

        var meta = new FileMetadata
        {
            OriginalName = file.FileName,
            StoredName = Path.GetFileName(storagePath),
            ContentType = file.ContentType,
            FileSize = file.Length,
            StoragePath = storagePath,
            StorageType = "Local",
            UploaderUserId = _currentUser.UserId,
            LinkedEntityType = entityType,
            LinkedEntityId = entityId,
        };
        await _db.Files.InsertOneAsync(meta, cancellationToken: ct);

        return Ok(new
        {
            id = meta.Id,
            originalName = meta.OriginalName,
            contentType = meta.ContentType,
            fileSize = meta.FileSize,
            storagePath = meta.StoragePath,
        });
    }

    /// <summary>파일 다중 업로드</summary>
    [HttpPost("upload/multi")]
    [RequestSizeLimit(524288000)] // 500MB
    public async Task<IActionResult> UploadMulti(List<IFormFile> files, [FromQuery] string? entityType, [FromQuery] string? entityId, CancellationToken ct)
    {
        if (files == null || files.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("FILE_EMPTY", "업로드할 파일이 없습니다."));

        var results = new List<object>();
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(ext) || file.Length > MaxFileSizeBytes)
                continue;

            await using var stream = file.OpenReadStream();
            var storagePath = await _storage.UploadAsync(stream, file.FileName, file.ContentType, ct);

            var meta = new FileMetadata
            {
                OriginalName = file.FileName,
                StoredName = Path.GetFileName(storagePath),
                ContentType = file.ContentType,
                FileSize = file.Length,
                StoragePath = storagePath,
                StorageType = "Local",
                UploaderUserId = _currentUser.UserId,
                LinkedEntityType = entityType,
                LinkedEntityId = entityId,
            };
            await _db.Files.InsertOneAsync(meta, cancellationToken: ct);
            results.Add(new { id = meta.Id, originalName = meta.OriginalName, fileSize = meta.FileSize });
        }

        return Ok(results);
    }

    /// <summary>파일 다운로드</summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(string id, CancellationToken ct)
    {
        var meta = await _db.Files.Find(f => f.Id == id && !f.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("File", id);

        await _audit.LogAsync("FILE_DOWNLOAD", "File", id, ct: ct);

        // 다운로드 카운트 증가
        var update = Builders<FileMetadata>.Update.Inc(f => f.DownloadCount, 1);
        await _db.Files.UpdateOneAsync(f => f.Id == id, update, cancellationToken: ct);

        var stream = await _storage.DownloadAsync(meta.StoragePath, ct);
        return File(stream, meta.ContentType, meta.OriginalName);
    }

    /// <summary>파일 메타데이터 조회</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMeta(string id, CancellationToken ct)
    {
        var meta = await _db.Files.Find(f => f.Id == id && !f.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("File", id);

        return Ok(new
        {
            meta.Id,
            meta.OriginalName,
            meta.ContentType,
            meta.FileSize,
            meta.UploaderUserId,
            meta.LinkedEntityType,
            meta.LinkedEntityId,
            meta.DownloadCount,
            meta.CreatedAt,
        });
    }

    /// <summary>엔티티에 연결된 파일 목록</summary>
    [HttpGet("by-entity")]
    public async Task<IActionResult> GetByEntity([FromQuery] string entityType, [FromQuery] string entityId, CancellationToken ct)
    {
        var files = await _db.Files
            .Find(f => f.LinkedEntityType == entityType && f.LinkedEntityId == entityId && !f.IsDeleted)
            .SortBy(f => f.CreatedAt)
            .ToListAsync(ct);

        return Ok(files.Select(f => new
        {
            f.Id,
            f.OriginalName,
            f.ContentType,
            f.FileSize,
            f.DownloadCount,
            f.CreatedAt,
        }));
    }

    /// <summary>파일 삭제</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var meta = await _db.Files.Find(f => f.Id == id && !f.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("File", id);

        // 업로더 본인 또는 관리자만 삭제 가능
        if (meta.UploaderUserId != _currentUser.UserId && !_currentUser.IsInRole("SystemAdmin"))
            throw new ForbiddenException("파일 삭제 권한이 없습니다.");

        var update = Builders<FileMetadata>.Update
            .Set(f => f.IsDeleted, true)
            .Set(f => f.DeletedAt, DateTime.UtcNow)
            .Set(f => f.DeletedBy, _currentUser.UserId);
        await _db.Files.UpdateOneAsync(f => f.Id == id, update, cancellationToken: ct);
        await _storage.DeleteAsync(meta.StoragePath, ct);

        await _audit.LogAsync("FILE_DELETE", "File", id, ct: ct);
        return Ok();
    }
}
