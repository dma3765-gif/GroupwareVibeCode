using Engine.Application.Attendance;
using Engine.Application.Common.DTOs;
using Engine.Application.Common.Exceptions;
using Engine.Application.Common.Interfaces;
using Engine.Application.Common.Responses;
using Engine.Domain.Attendance;
using LeaveRequestDto = Engine.Application.Attendance.LeaveRequestDto;
using LeaveRequestDomain = Engine.Domain.Attendance.LeaveRequest;
using LeaveRequestApp = Engine.Application.Attendance.LeaveRequest;
using Engine.Domain.Common.Enums;
using Engine.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace Engine.Infrastructure.Services;

public class AttendanceServiceImpl : IAttendanceService
{
    private readonly GroupwareDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogService _audit;
    private readonly INotificationPublisher _notification;

    public AttendanceServiceImpl(GroupwareDbContext db, ICurrentUserContext currentUser,
        IAuditLogService audit, INotificationPublisher notification)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
        _notification = notification;
    }

    public async Task<AttendanceRecordDto> CheckInAsync(CheckInRequest request, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var existing = await _db.AttendanceRecords
            .Find(r => r.UserId == _currentUser.UserId && r.WorkDate == today && !r.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (existing != null && existing.CheckInTime.HasValue)
            throw new DomainException("이미 출근 체크인이 완료되었습니다.");

        AttendanceRecord record;
        if (existing != null)
        {
            existing.CheckInTime = DateTime.UtcNow;
            existing.WorkType = request.WorkType;
            existing.Location = request.Location;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = _currentUser.UserId;
            await _db.AttendanceRecords.ReplaceOneAsync(r => r.Id == existing.Id, existing, cancellationToken: ct);
            record = existing;
        }
        else
        {
            record = new AttendanceRecord
            {
                UserId = _currentUser.UserId,
                UserName = _currentUser.Name,
                DepartmentId = _currentUser.DepartmentId,
                WorkDate = today,
                CheckInTime = DateTime.UtcNow,
                WorkType = request.WorkType,
                Location = request.Location,
                Status = AttendanceStatus.Normal,
                CreatedBy = _currentUser.UserId,
            };
            await _db.AttendanceRecords.InsertOneAsync(record, cancellationToken: ct);
        }

        await _audit.LogAsync("CHECKIN", "AttendanceRecord", record.Id, ct: ct);
        return ToDto(record);
    }

    public async Task<AttendanceRecordDto> CheckOutAsync(CheckOutRequest request, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var record = await _db.AttendanceRecords
            .Find(r => r.UserId == _currentUser.UserId && r.WorkDate == today && !r.IsDeleted)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("AttendanceRecord", "today");

        if (!record.CheckInTime.HasValue)
            throw new DomainException("출근 기록이 없습니다. 먼저 체크인 해주세요.");

        if (record.CheckOutTime.HasValue)
            throw new DomainException("이미 퇴근 처리가 완료되었습니다.");

        record.CheckOutTime = DateTime.UtcNow;
        record.Memo = request.Memo;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = _currentUser.UserId;

        // 근무시간 계산
        record.WorkMinutes = (int)(record.CheckOutTime.Value - record.CheckInTime.Value).TotalMinutes;
        record.OvertimeMinutes = Math.Max(0, record.WorkMinutes - 480); // 8시간 초과분

        await _db.AttendanceRecords.ReplaceOneAsync(r => r.Id == record.Id, record, cancellationToken: ct);
        await _audit.LogAsync("CHECKOUT", "AttendanceRecord", record.Id, ct: ct);
        return ToDto(record);
    }

    public async Task<AttendanceRecordDto?> GetTodayRecordAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var record = await _db.AttendanceRecords
            .Find(r => r.UserId == _currentUser.UserId && r.WorkDate == today && !r.IsDeleted)
            .FirstOrDefaultAsync(ct);
        return record == null ? null : ToDto(record);
    }

    public async Task<PagedResult<AttendanceRecordDto>> GetMyRecordsAsync(AttendanceQuery query, CancellationToken ct = default)
    {
        var filter = Builders<AttendanceRecord>.Filter.And(
            Builders<AttendanceRecord>.Filter.Eq(r => r.UserId, _currentUser.UserId),
            Builders<AttendanceRecord>.Filter.Eq(r => r.IsDeleted, false)
        );

        if (query.From.HasValue)
            filter &= Builders<AttendanceRecord>.Filter.Gte(r => r.WorkDate, query.From.Value);
        if (query.To.HasValue)
            filter &= Builders<AttendanceRecord>.Filter.Lte(r => r.WorkDate, query.To.Value);
        if (query.Status.HasValue)
            filter &= Builders<AttendanceRecord>.Filter.Eq(r => r.Status, query.Status.Value);

        var total = await _db.AttendanceRecords.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _db.AttendanceRecords.Find(filter)
            .SortByDescending(r => r.WorkDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Limit(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<AttendanceRecordDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = (int)total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<PagedResult<AttendanceRecordDto>> GetTeamRecordsAsync(string departmentId, AttendanceQuery query, CancellationToken ct = default)
    {
        var filter = Builders<AttendanceRecord>.Filter.And(
            Builders<AttendanceRecord>.Filter.Eq(r => r.DepartmentId, departmentId),
            Builders<AttendanceRecord>.Filter.Eq(r => r.IsDeleted, false)
        );

        if (query.From.HasValue) filter &= Builders<AttendanceRecord>.Filter.Gte(r => r.WorkDate, query.From.Value);
        if (query.To.HasValue) filter &= Builders<AttendanceRecord>.Filter.Lte(r => r.WorkDate, query.To.Value);

        var total = await _db.AttendanceRecords.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _db.AttendanceRecords.Find(filter)
            .SortByDescending(r => r.WorkDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Limit(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<AttendanceRecordDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = (int)total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<LeaveBalanceDto> GetMyLeaveBalanceAsync(int? year = null, CancellationToken ct = default)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var balance = await _db.LeaveBalances
            .Find(b => b.UserId == _currentUser.UserId && b.Year == targetYear && !b.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (balance == null)
            return new LeaveBalanceDto { UserId = _currentUser.UserId, Year = targetYear };

        return new LeaveBalanceDto
        {
            Id = balance.Id,
            UserId = balance.UserId,
            Year = balance.Year,
            TotalDays = balance.TotalDays,
            UsedDays = balance.UsedDays,
            RemainingDays = balance.RemainingDays,
        };
    }

    public async Task<LeaveRequestDto> ApplyLeaveAsync(LeaveRequestApp request, CancellationToken ct = default)
    {
        var year = request.StartDate.Year;
        var balance = await _db.LeaveBalances
            .Find(b => b.UserId == _currentUser.UserId && b.Year == year && !b.IsDeleted)
            .FirstOrDefaultAsync(ct);

        decimal requestedDays = (request.EndDate - request.StartDate).Days + 1;
        if (request.IsHalfDay) requestedDays = 0.5m;

        if (balance == null || balance.RemainingDays < requestedDays)
            throw new DomainException($"잔여 연차가 부족합니다. 신청: {requestedDays}일, 잔여: {balance?.RemainingDays ?? 0}일");

        var leaveReq = new LeaveRequestDomain
        {
            UserId = _currentUser.UserId,
            UserName = _currentUser.Name,
            DepartmentId = _currentUser.DepartmentId,
            LeaveType = request.LeaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsHalfDay = request.IsHalfDay,
            HalfDayPeriod = request.HalfDayPeriod,
            Reason = request.Reason,
            DaysCount = requestedDays,            ApprovalStatus = LeaveApprovalStatus.Pending,
            CreatedBy = _currentUser.UserId,
        };

        await _db.LeaveRequests.InsertOneAsync(leaveReq, cancellationToken: ct);
        await _audit.LogAsync("LEAVE_REQUEST", "LeaveRequest", leaveReq.Id, ct: ct);

        return new LeaveRequestDto
        {
            Id = leaveReq.Id,
            LeaveType = leaveReq.LeaveType.ToString(),
            StartDate = leaveReq.StartDate,
            EndDate = leaveReq.EndDate,
            DaysCount = leaveReq.DaysCount,
            Reason = leaveReq.Reason,
            ApprovalStatus = leaveReq.ApprovalStatus.ToString(),
            CreatedAt = leaveReq.CreatedAt,
        };
    }

    public async Task<LeaveRequestDto> ApproveLeaveAsync(string requestId, LeaveApproveRequest request, CancellationToken ct = default)
    {
        var leaveReq = await _db.LeaveRequests.Find(r => r.Id == requestId && !r.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("LeaveRequest", requestId);

        if (leaveReq.ApprovalStatus != LeaveApprovalStatus.Pending)
            throw new DomainException("처리 가능한 상태가 아닙니다.");

        leaveReq.ApprovalStatus = request.IsApproved ? LeaveApprovalStatus.Approved : LeaveApprovalStatus.Rejected;
        leaveReq.ApprovedBy = _currentUser.UserId;
        leaveReq.ApprovedAt = DateTime.UtcNow;
        leaveReq.ApprovalComment = request.Comment;
        leaveReq.UpdatedAt = DateTime.UtcNow;
        leaveReq.UpdatedBy = _currentUser.UserId;

        await _db.LeaveRequests.ReplaceOneAsync(r => r.Id == requestId, leaveReq, cancellationToken: ct);

        if (request.IsApproved)
        {
            // 연차 차감
            await _db.LeaveBalances.UpdateOneAsync(
                b => b.UserId == leaveReq.UserId && b.Year == leaveReq.StartDate.Year,
                Builders<LeaveBalance>.Update.Inc(b => b.UsedDays, leaveReq.DaysCount),
                cancellationToken: ct);
        }

        await _notification.PublishAsync(leaveReq.UserId, request.IsApproved ? NotificationType.ApprovalApproved : NotificationType.ApprovalRejected,
            request.IsApproved ? "휴가 승인" : "휴가 반려",
            request.IsApproved ? "휴가 신청이 승인되었습니다." : $"휴가 신청이 반려되었습니다. 사유: {request.Comment}",
            "LeaveRequest", leaveReq.Id, ct);

        return new LeaveRequestDto
        {
            Id = leaveReq.Id,
            LeaveType = leaveReq.LeaveType.ToString(),
            StartDate = leaveReq.StartDate,
            EndDate = leaveReq.EndDate,
            DaysCount = leaveReq.DaysCount,
            Reason = leaveReq.Reason,
            ApprovalStatus = leaveReq.ApprovalStatus.ToString(),
        };
    }

    private static AttendanceRecordDto ToDto(AttendanceRecord r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        UserName = r.UserName,
        WorkDate = r.WorkDate,
        CheckInTime = r.CheckInTime,
        CheckOutTime = r.CheckOutTime,
        WorkMinutes = r.WorkMinutes,
        OvertimeMinutes = r.OvertimeMinutes,
        WorkType = r.WorkType.ToString(),
        Status = r.Status.ToString(),
        Location = r.Location,
        Memo = r.Memo,
    };
}
