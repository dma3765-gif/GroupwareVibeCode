using Engine.Application.Common.DTOs;
using Engine.Application.Common.Responses;
using Engine.Domain.Common.Enums;

namespace Engine.Application.Board;

// ─── Board DTOs ───

public class BoardDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BoardType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool AllowComment { get; set; }
    public bool AllowAttachment { get; set; }
}

public class BoardPostDto
{
    public string Id { get; set; } = string.Empty;
    public string BoardId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorDept { get; set; } = string.Empty;
    public bool IsNotice { get; set; }
    public bool IsPinned { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<AttachmentDto> Attachments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class BoardPostSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorDept { get; set; } = string.Empty;
    public bool IsNotice { get; set; }
    public bool IsPinned { get; set; }
    public int ViewCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AttachmentDto
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

public class BoardCommentDto
{
    public string Id { get; set; } = string.Empty;
    public string? ParentCommentId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<BoardCommentDto> Replies { get; set; } = new();
}

// ─── Request DTOs ───

public class CreateBoardPostRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsNotice { get; set; } = false;
    public bool IsPinned { get; set; } = false;
    public List<string> Tags { get; set; } = new();
    public string? CategoryCode { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? PublishAt { get; set; }
}

public class UpdateBoardPostRequest : CreateBoardPostRequest { }

public class CreateCommentRequest
{
    public string? ParentCommentId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class BoardPostSearchRequest : PagedRequest
{
    public string? CategoryCode { get; set; }
    public bool? IsNotice { get; set; }
}

// ─── Service Interface ───

public interface IBoardService
{
    Task<PagedResult<BoardPostSummaryDto>> GetPostsAsync(string boardId, BoardPostSearchRequest request, CancellationToken ct = default);
    Task<BoardPostDto> GetPostByIdAsync(string boardId, string postId, CancellationToken ct = default);
    Task<BoardPostDto> CreatePostAsync(string boardId, CreateBoardPostRequest request, CancellationToken ct = default);
    Task<BoardPostDto> UpdatePostAsync(string boardId, string postId, UpdateBoardPostRequest request, CancellationToken ct = default);
    Task DeletePostAsync(string boardId, string postId, CancellationToken ct = default);
    Task<List<BoardCommentDto>> GetCommentsAsync(string postId, CancellationToken ct = default);
    Task<BoardCommentDto> CreateCommentAsync(string postId, CreateCommentRequest request, CancellationToken ct = default);
    Task DeleteCommentAsync(string postId, string commentId, CancellationToken ct = default);
    Task<List<BoardDto>> GetAccessibleBoardsAsync(CancellationToken ct = default);
}
