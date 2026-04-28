using Engine.Domain.Approval;
using Engine.Domain.Attendance;
using Engine.Domain.Board;
using Engine.Domain.Calendar;
using CalendarEntity = Engine.Domain.Calendar.Calendar;
using Engine.Domain.Notification;
using Engine.Domain.Organization;
using Engine.Domain.Security;
using MongoDB.Driver;

namespace Engine.Infrastructure.Persistence.Mongo;

/// <summary>
/// MongoDB 컨텍스트 - 모든 컬렉션에 대한 접근 포인트
/// </summary>
public class GroupwareDbContext
{
    private readonly IMongoDatabase _db;

    public GroupwareDbContext(IMongoDatabase db)
    {
        _db = db;
        try { EnsureIndexes(); } catch { /* MongoDB 연결 없이 시작 허용 */ }
    }

    // ─── 컬렉션 ───
    public IMongoCollection<User> Users => _db.GetCollection<User>("users");
    public IMongoCollection<Organization> Organizations => _db.GetCollection<Organization>("organizations");
    public IMongoCollection<UserRole> UserRoles => _db.GetCollection<UserRole>("userRoles");
    public IMongoCollection<RolePermission> RolePermissions => _db.GetCollection<RolePermission>("rolePermissions");

    public IMongoCollection<ApprovalForm> ApprovalForms => _db.GetCollection<ApprovalForm>("approvalForms");
    public IMongoCollection<ApprovalDocument> ApprovalDocuments => _db.GetCollection<ApprovalDocument>("approvalDocuments");

    public IMongoCollection<Board> Boards => _db.GetCollection<Board>("boards");
    public IMongoCollection<BoardPost> BoardPosts => _db.GetCollection<BoardPost>("boardPosts");
    public IMongoCollection<BoardComment> BoardComments => _db.GetCollection<BoardComment>("boardComments");

    public IMongoCollection<AttendanceRecord> AttendanceRecords => _db.GetCollection<AttendanceRecord>("attendanceRecords");
    public IMongoCollection<LeaveBalance> LeaveBalances => _db.GetCollection<LeaveBalance>("leaveBalances");
    public IMongoCollection<LeaveRequest> LeaveRequests => _db.GetCollection<LeaveRequest>("leaveRequests");

    public IMongoCollection<CalendarEntity> Calendars => _db.GetCollection<CalendarEntity>("calendars");
    public IMongoCollection<CalendarEvent> CalendarEvents => _db.GetCollection<CalendarEvent>("calendarEvents");
    public IMongoCollection<Resource> Resources => _db.GetCollection<Resource>("resources");
    public IMongoCollection<ResourceReservation> ResourceReservations => _db.GetCollection<ResourceReservation>("resourceReservations");

    public IMongoCollection<Notification> Notifications => _db.GetCollection<Notification>("notifications");
    public IMongoCollection<NotificationTemplate> NotificationTemplates => _db.GetCollection<NotificationTemplate>("notificationTemplates");

    public IMongoCollection<AuditLog> AuditLogs => _db.GetCollection<AuditLog>("auditLogs");
    public IMongoCollection<FileMetadata> Files => _db.GetCollection<FileMetadata>("files");
    public IMongoCollection<SystemCode> SystemCodes => _db.GetCollection<SystemCode>("systemCodes");
    public IMongoCollection<Menu> Menus => _db.GetCollection<Menu>("menus");
    public IMongoCollection<SystemSetting> SystemSettings => _db.GetCollection<SystemSetting>("systemSettings");

    // ─── 인덱스 생성 ───
    private void EnsureIndexes()
    {
        // Users
        Users.Indexes.CreateMany([
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Email), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.EmployeeNo), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.DepartmentId)),
        ]);

        // ApprovalDocuments
        ApprovalDocuments.Indexes.CreateMany([
            new CreateIndexModel<ApprovalDocument>(Builders<ApprovalDocument>.IndexKeys
                .Ascending(d => d.Status)
                .Ascending("ApprovalLine.UserId")),
            new CreateIndexModel<ApprovalDocument>(Builders<ApprovalDocument>.IndexKeys
                .Ascending(d => d.TenantId)
                .Ascending("Drafter.UserId")
                .Ascending(d => d.Status)),
            new CreateIndexModel<ApprovalDocument>(Builders<ApprovalDocument>.IndexKeys
                .Ascending(d => d.DocumentNo), new CreateIndexOptions { Unique = true, Sparse = true }),
        ]);

        // BoardPosts
        BoardPosts.Indexes.CreateMany([
            new CreateIndexModel<BoardPost>(Builders<BoardPost>.IndexKeys
                .Ascending(p => p.BoardId)
                .Descending(p => p.CreatedAt)),
            new CreateIndexModel<BoardPost>(Builders<BoardPost>.IndexKeys
                .Text(p => p.Title).Text(p => p.Content)),
        ]);

        // AttendanceRecords
        AttendanceRecords.Indexes.CreateMany([
            new CreateIndexModel<AttendanceRecord>(Builders<AttendanceRecord>.IndexKeys
                .Ascending(a => a.UserId)
                .Ascending(a => a.WorkDate)),
        ]);

        // Notifications
        Notifications.Indexes.CreateMany([
            new CreateIndexModel<Notification>(Builders<Notification>.IndexKeys
                .Ascending(n => n.RecipientUserId)
                .Ascending(n => n.IsRead)
                .Descending(n => n.CreatedAt)),
        ]);
    }
}
