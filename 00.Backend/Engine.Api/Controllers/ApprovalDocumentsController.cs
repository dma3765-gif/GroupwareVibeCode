using Engine.Application.Approval;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Api.Controllers;

[Authorize]
public class ApprovalDocumentsController : BaseController
{
    private readonly IApprovalDocumentService _service;
    public ApprovalDocumentsController(IApprovalDocumentService service) => _service = service;

    /// <summary>기안함</summary>
    [HttpGet("box/drafts")]
    public async Task<IActionResult> GetDrafts([FromQuery] ApprovalBoxQuery query, CancellationToken ct)
        => Ok(await _service.GetMyDraftsAsync(query, ct));

    /// <summary>결재 대기함</summary>
    [HttpGet("box/pending")]
    public async Task<IActionResult> GetPending([FromQuery] ApprovalBoxQuery query, CancellationToken ct)
        => Ok(await _service.GetMyPendingAsync(query, ct));

    /// <summary>진행 중인 결재</summary>
    [HttpGet("box/in-progress")]
    public async Task<IActionResult> GetInProgress([FromQuery] ApprovalBoxQuery query, CancellationToken ct)
        => Ok(await _service.GetMyInProgressAsync(query, ct));

    /// <summary>완료 결재함</summary>
    [HttpGet("box/completed")]
    public async Task<IActionResult> GetCompleted([FromQuery] ApprovalBoxQuery query, CancellationToken ct)
        => Ok(await _service.GetMyCompletedAsync(query, ct));

    /// <summary>반려함</summary>
    [HttpGet("box/rejected")]
    public async Task<IActionResult> GetRejected([FromQuery] ApprovalBoxQuery query, CancellationToken ct)
        => Ok(await _service.GetMyRejectedAsync(query, ct));

    /// <summary>부서 수신함</summary>
    [HttpGet("box/department")]
    public async Task<IActionResult> GetDepartmentReceived([FromQuery] ApprovalBoxQuery query, CancellationToken ct)
        => Ok(await _service.GetDepartmentReceivedAsync(query, ct));

    /// <summary>문서 상세 조회</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    /// <summary>문서 기안</summary>
    [HttpPost]
    public async Task<IActionResult> Draft([FromBody] DraftDocumentRequest request, CancellationToken ct)
        => Created(await _service.DraftAsync(request, ct));

    /// <summary>문서 상신</summary>
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(string id, CancellationToken ct)
        => Ok(await _service.SubmitAsync(id, ct));

    /// <summary>결재</summary>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(string id, [FromBody] ApproveRequest request, CancellationToken ct)
        => Ok(await _service.ApproveAsync(id, request, ct));

    /// <summary>합의</summary>
    [HttpPost("{id}/agree")]
    public async Task<IActionResult> Agree(string id, [FromBody] ApproveRequest request, CancellationToken ct)
        => Ok(await _service.AgreeAsync(id, request, ct));

    /// <summary>협의</summary>
    [HttpPost("{id}/consult")]
    public async Task<IActionResult> Consult(string id, [FromBody] ConsultRequest request, CancellationToken ct)
        => Ok(await _service.ConsultAsync(id, request, ct));

    /// <summary>반려</summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(string id, [FromBody] RejectRequest request, CancellationToken ct)
        => Ok(await _service.RejectAsync(id, request, ct));

    /// <summary>최종 전결</summary>
    [HttpPost("{id}/final-approve")]
    public async Task<IActionResult> FinalApprove(string id, [FromBody] ApproveRequest request, CancellationToken ct)
        => Ok(await _service.FinalApproveAsync(id, request, ct));

    /// <summary>대결 처리</summary>
    [HttpPost("{id}/delegate-approve")]
    public async Task<IActionResult> DelegateApprove(string id, [FromBody] DelegateApproveRequest request, CancellationToken ct)
        => Ok(await _service.DelegateApproveAsync(id, request, ct));

    /// <summary>회수</summary>
    [HttpPost("{id}/recall")]
    public async Task<IActionResult> Recall(string id, CancellationToken ct)
        => Ok(await _service.RecallAsync(id, new Engine.Application.Approval.RecallRequest(), ct));
}
