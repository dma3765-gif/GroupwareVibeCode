using Engine.Application.Board;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Api.Controllers;

[Authorize]
public class BoardsController : BaseController
{
    private readonly IBoardService _service;
    public BoardsController(IBoardService service) => _service = service;

    /// <summary>접근 가능한 게시판 목록</summary>
    [HttpGet]
    public async Task<IActionResult> GetBoards(CancellationToken ct)
        => Ok(await _service.GetAccessibleBoardsAsync(ct));

    /// <summary>게시글 목록</summary>
    [HttpGet("{boardId}/posts")]
    public async Task<IActionResult> GetPosts(string boardId, [FromQuery] BoardPostSearchRequest request, CancellationToken ct)
        => Ok(await _service.GetPostsAsync(boardId, request, ct));

    /// <summary>게시글 상세</summary>
    [HttpGet("{boardId}/posts/{postId}")]
    public async Task<IActionResult> GetPost(string boardId, string postId, CancellationToken ct)
        => Ok(await _service.GetPostByIdAsync(boardId, postId, ct));

    /// <summary>게시글 작성</summary>
    [HttpPost("{boardId}/posts")]
    public async Task<IActionResult> CreatePost(string boardId, [FromBody] CreateBoardPostRequest request, CancellationToken ct)
        => Created(await _service.CreatePostAsync(boardId, request, ct));

    /// <summary>게시글 수정</summary>
    [HttpPut("{boardId}/posts/{postId}")]
    public async Task<IActionResult> UpdatePost(string boardId, string postId, [FromBody] UpdateBoardPostRequest request, CancellationToken ct)
        => Ok(await _service.UpdatePostAsync(boardId, postId, request, ct));

    /// <summary>게시글 삭제</summary>
    [HttpDelete("{boardId}/posts/{postId}")]
    public async Task<IActionResult> DeletePost(string boardId, string postId, CancellationToken ct)
    {
        await _service.DeletePostAsync(boardId, postId, ct);
        return NoContent();
    }

    /// <summary>댓글 목록</summary>
    [HttpGet("{boardId}/posts/{postId}/comments")]
    public async Task<IActionResult> GetComments(string boardId, string postId, CancellationToken ct)
        => Ok(await _service.GetCommentsAsync(postId, ct));

    /// <summary>댓글 작성</summary>
    [HttpPost("{boardId}/posts/{postId}/comments")]
    public async Task<IActionResult> CreateComment(string boardId, string postId, [FromBody] CreateCommentRequest request, CancellationToken ct)
        => Created(await _service.CreateCommentAsync(postId, request, ct));

    /// <summary>댓글 삭제</summary>
    [HttpDelete("{boardId}/posts/{postId}/comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(string boardId, string postId, string commentId, CancellationToken ct)
    {
        await _service.DeleteCommentAsync(postId, commentId, ct);
        return NoContent();
    }
}
