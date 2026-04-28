using Engine.Domain.Common.Entities;
using Engine.Domain.Common.Enums;

namespace Engine.Domain.Board;

/// <summary>게시판 메타 정보</summary>
public class Board : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public BoardType BoardType { get; set; } = BoardType.Free;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAnonymous { get; set; } = false;
    public bool AllowComment { get; set; } = true;
    public bool AllowAttachment { get; set; } = true;
    public bool AllowLike { get; set; } = true;
    public string? TargetDepartmentId { get; set; } // 부서 게시판일 경우
    public int SortOrder { get; set; } = 0;
    public List<BoardPermission> Permissions { get; set; } = new();
}

public class BoardPermission
{
    public string? RoleOrUserId { get; set; }
    public bool CanRead { get; set; } = true;
    public bool CanWrite { get; set; } = false;
    public bool CanManage { get; set; } = false;
}

/// <summary>게시글</summary>
public class BoardPost : BaseEntity
{
    public string BoardId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // HTML Sanitized
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorDept { get; set; } = string.Empty;
    public bool IsNotice { get; set; } = false;        // 공지
    public bool IsPinned { get; set; } = false;        // 상단 고정
    public bool IsAnonymous { get; set; } = false;
    public bool IsHidden { get; set; } = false;
    public DateTime? StartDate { get; set; }           // 게시 시작일
    public DateTime? EndDate { get; set; }             // 게시 만료일
    public DateTime? PublishAt { get; set; }           // 예약 게시
    public int ViewCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;
    public int CommentCount { get; set; } = 0;
    public List<string> Tags { get; set; } = new();
    public string? CategoryCode { get; set; }
    public List<AttachmentMeta> Attachments { get; set; } = new();
}

/// <summary>댓글</summary>
public class BoardComment : BaseEntity
{
    public string PostId { get; set; } = string.Empty;
    public string? ParentCommentId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; } = false;
    public bool IsHidden { get; set; } = false;
}

public class AttachmentMeta
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
}
