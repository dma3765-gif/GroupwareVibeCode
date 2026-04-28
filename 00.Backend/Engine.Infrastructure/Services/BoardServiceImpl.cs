using Engine.Application.Board;
using Engine.Application.Common.DTOs;
using Engine.Application.Common.Exceptions;
using Engine.Application.Common.Interfaces;
using Engine.Application.Common.Responses;
using Engine.Domain.Board;
using Engine.Infrastructure.Persistence.Mongo;
using Ganss.Xss;
using MongoDB.Driver;

namespace Engine.Infrastructure.Services;

public class BoardServiceImpl : IBoardService
{
    private readonly GroupwareDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogService _audit;
    private readonly HtmlSanitizer _sanitizer;

    public BoardServiceImpl(GroupwareDbContext db, ICurrentUserContext currentUser, IAuditLogService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Add("div");
        _sanitizer.AllowedTags.Add("table");
        _sanitizer.AllowedTags.Add("tr");
        _sanitizer.AllowedTags.Add("td");
        _sanitizer.AllowedTags.Add("th");
        _sanitizer.AllowedTags.Add("img");
    }

    public async Task<PagedResult<BoardPostSummaryDto>> GetPostsAsync(string boardId, BoardPostSearchRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<BoardPost>.Filter.And(
            Builders<BoardPost>.Filter.Eq(p => p.BoardId, boardId),
            Builders<BoardPost>.Filter.Eq(p => p.IsDeleted, false),
            Builders<BoardPost>.Filter.Eq(p => p.IsHidden, false),
            Builders<BoardPost>.Filter.Or(
                Builders<BoardPost>.Filter.Eq(p => p.PublishAt, null),
                Builders<BoardPost>.Filter.Lte(p => p.PublishAt, (DateTime?)now)
            )
        );

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            filter &= Builders<BoardPost>.Filter.Or(
                Builders<BoardPost>.Filter.Regex(p => p.Title, new MongoDB.Bson.BsonRegularExpression(request.Keyword, "i")),
                Builders<BoardPost>.Filter.Regex(p => p.Content, new MongoDB.Bson.BsonRegularExpression(request.Keyword, "i"))
            );
        }

        if (request.IsNotice.HasValue)
            filter &= Builders<BoardPost>.Filter.Eq(p => p.IsNotice, request.IsNotice.Value);

        var total = await _db.BoardPosts.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _db.BoardPosts.Find(filter)
            .SortByDescending(p => p.IsPinned)
            .SortByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<BoardPostSummaryDto>
        {
            Items = items.Select(ToSummaryDto).ToList(),
            TotalCount = (int)total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<BoardPostDto> GetPostByIdAsync(string boardId, string postId, CancellationToken ct = default)
    {
        var post = await _db.BoardPosts
            .Find(p => p.Id == postId && p.BoardId == boardId && !p.IsDeleted)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("BoardPost", postId);

        // 조회수 증가
        await _db.BoardPosts.UpdateOneAsync(
            p => p.Id == postId,
            Builders<BoardPost>.Update.Inc(p => p.ViewCount, 1),
            cancellationToken: ct);

        await _audit.LogAsync("BOARD_POST_VIEW", "BoardPost", postId, ct: ct);
        return ToDetailDto(post);
    }

    public async Task<BoardPostDto> CreatePostAsync(string boardId, CreateBoardPostRequest request, CancellationToken ct = default)
    {
        var board = await _db.Boards.Find(b => b.Id == boardId && b.IsActive).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Board", boardId);

        var post = new BoardPost
        {
            BoardId = boardId,
            Title = request.Title,
            Content = _sanitizer.Sanitize(request.Content), // XSS 방어
            AuthorId = _currentUser.UserId,
            AuthorName = _currentUser.Name,
            AuthorDept = _currentUser.DepartmentName,
            IsNotice = request.IsNotice,
            IsPinned = request.IsPinned,
            Tags = request.Tags,
            CategoryCode = request.CategoryCode,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PublishAt = request.PublishAt,
            CreatedBy = _currentUser.UserId,
        };

        await _db.BoardPosts.InsertOneAsync(post, cancellationToken: ct);
        await _audit.LogAsync("BOARD_POST_CREATE", "BoardPost", post.Id, ct: ct);
        return ToDetailDto(post);
    }

    public async Task<BoardPostDto> UpdatePostAsync(string boardId, string postId, UpdateBoardPostRequest request, CancellationToken ct = default)
    {
        var post = await _db.BoardPosts.Find(p => p.Id == postId && p.BoardId == boardId && !p.IsDeleted)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("BoardPost", postId);

        if (post.AuthorId != _currentUser.UserId && !_currentUser.IsInRole("BoardAdmin"))
            throw new ForbiddenException("게시글 수정 권한이 없습니다.");

        post.Title = request.Title;
        post.Content = _sanitizer.Sanitize(request.Content);
        post.IsNotice = request.IsNotice;
        post.IsPinned = request.IsPinned;
        post.Tags = request.Tags;
        post.CategoryCode = request.CategoryCode;
        post.StartDate = request.StartDate;
        post.EndDate = request.EndDate;
        post.UpdatedAt = DateTime.UtcNow;
        post.UpdatedBy = _currentUser.UserId;

        await _db.BoardPosts.ReplaceOneAsync(p => p.Id == postId, post, cancellationToken: ct);
        await _audit.LogAsync("BOARD_POST_UPDATE", "BoardPost", postId, ct: ct);
        return ToDetailDto(post);
    }

    public async Task DeletePostAsync(string boardId, string postId, CancellationToken ct = default)
    {
        var post = await _db.BoardPosts.Find(p => p.Id == postId && p.BoardId == boardId && !p.IsDeleted)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("BoardPost", postId);

        if (post.AuthorId != _currentUser.UserId && !_currentUser.IsInRole("BoardAdmin"))
            throw new ForbiddenException("게시글 삭제 권한이 없습니다.");

        await _db.BoardPosts.UpdateOneAsync(p => p.Id == postId,
            Builders<BoardPost>.Update
                .Set(p => p.IsDeleted, true)
                .Set(p => p.DeletedAt, DateTime.UtcNow)
                .Set(p => p.DeletedBy, _currentUser.UserId),
            cancellationToken: ct);

        await _audit.LogAsync("BOARD_POST_DELETE", "BoardPost", postId, ct: ct);
    }

    public async Task<List<BoardCommentDto>> GetCommentsAsync(string postId, CancellationToken ct = default)
    {
        var comments = await _db.BoardComments
            .Find(c => c.PostId == postId && !c.IsDeleted && !c.IsHidden)
            .SortBy(c => c.CreatedAt)
            .ToListAsync(ct);

        var roots = comments.Where(c => c.ParentCommentId == null)
            .Select(c => ToCommentDto(c, comments))
            .ToList();

        return roots;
    }

    public async Task<BoardCommentDto> CreateCommentAsync(string postId, CreateCommentRequest request, CancellationToken ct = default)
    {
        var comment = new BoardComment
        {
            PostId = postId,
            ParentCommentId = request.ParentCommentId,
            AuthorId = _currentUser.UserId,
            AuthorName = _currentUser.Name,
            Content = request.Content,
            CreatedBy = _currentUser.UserId,
        };

        await _db.BoardComments.InsertOneAsync(comment, cancellationToken: ct);
        await _db.BoardPosts.UpdateOneAsync(p => p.Id == postId,
            Builders<BoardPost>.Update.Inc(p => p.CommentCount, 1),
            cancellationToken: ct);

        return new BoardCommentDto
        {
            Id = comment.Id,
            ParentCommentId = comment.ParentCommentId,
            AuthorId = comment.AuthorId,
            AuthorName = comment.AuthorName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
        };
    }

    public async Task DeleteCommentAsync(string postId, string commentId, CancellationToken ct = default)
    {
        var comment = await _db.BoardComments
            .Find(c => c.Id == commentId && c.PostId == postId && !c.IsDeleted)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("BoardComment", commentId);

        if (comment.AuthorId != _currentUser.UserId && !_currentUser.IsInRole("BoardAdmin"))
            throw new ForbiddenException("댓글 삭제 권한이 없습니다.");

        await _db.BoardComments.UpdateOneAsync(c => c.Id == commentId,
            Builders<BoardComment>.Update.Set(c => c.IsDeleted, true),
            cancellationToken: ct);
    }

    public async Task<List<BoardDto>> GetAccessibleBoardsAsync(CancellationToken ct = default)
    {
        var boards = await _db.Boards.Find(b => b.IsActive && !b.IsDeleted).ToListAsync(ct);
        return boards.Select(b => new BoardDto
        {
            Id = b.Id,
            Code = b.Code,
            Name = b.Name,
            BoardType = b.BoardType.ToString(),
            IsActive = b.IsActive,
            AllowComment = b.AllowComment,
            AllowAttachment = b.AllowAttachment,
        }).ToList();
    }

    private static BoardPostSummaryDto ToSummaryDto(BoardPost p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        AuthorName = p.AuthorName,
        AuthorDept = p.AuthorDept,
        IsNotice = p.IsNotice,
        IsPinned = p.IsPinned,
        ViewCount = p.ViewCount,
        CommentCount = p.CommentCount,
        CreatedAt = p.CreatedAt,
    };

    private static BoardPostDto ToDetailDto(BoardPost p) => new()
    {
        Id = p.Id,
        BoardId = p.BoardId,
        Title = p.Title,
        Content = p.Content,
        AuthorId = p.AuthorId,
        AuthorName = p.AuthorName,
        AuthorDept = p.AuthorDept,
        IsNotice = p.IsNotice,
        IsPinned = p.IsPinned,
        ViewCount = p.ViewCount,
        LikeCount = p.LikeCount,
        CommentCount = p.CommentCount,
        Tags = p.Tags,
        Attachments = p.Attachments.Select(a => new AttachmentDto
        {
            FileId = a.FileId,
            FileName = a.FileName,
            FileSize = a.FileSize,
            ContentType = a.ContentType,
        }).ToList(),
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
    };

    private static BoardCommentDto ToCommentDto(BoardComment c, List<BoardComment> all) => new()
    {
        Id = c.Id,
        ParentCommentId = c.ParentCommentId,
        AuthorId = c.AuthorId,
        AuthorName = c.AuthorName,
        Content = c.Content,
        CreatedAt = c.CreatedAt,
        Replies = all.Where(r => r.ParentCommentId == c.Id)
            .Select(r => ToCommentDto(r, all)).ToList()
    };
}
