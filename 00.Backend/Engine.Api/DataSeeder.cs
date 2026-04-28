using Engine.Domain.Attendance;
using Engine.Domain.Approval;
using Engine.Domain.Board;
using Engine.Domain.Common.Enums;
using Engine.Domain.Organization;
using Engine.Infrastructure.Persistence.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace Engine.Api;

public static class DataSeeder
{
    public static async Task SeedAsync(GroupwareDbContext db)
    {
        var userCount = await db.Users.CountDocumentsAsync(FilterDefinition<User>.Empty);
        if (userCount > 0)
        {
            Log.Information("시드 데이터가 이미 존재합니다. 건너뜁니다.");
            return;
        }

        // 1. 조직 생성
        var orgCompanyId = ObjectId.GenerateNewId().ToString();
        var orgItId      = ObjectId.GenerateNewId().ToString();
        var orgHrId      = ObjectId.GenerateNewId().ToString();

        var orgs = new List<Organization>
        {
            new() { Id = orgCompanyId, Code = "COMPANY", Name = "(주)그룹웨어", DeptType = "COMPANY", Level = 0, IsActive = true },
            new() { Id = orgItId,      Code = "IT",      Name = "IT개발팀",     DeptType = "TEAM",    Level = 1, ParentId = orgCompanyId, IsActive = true },
            new() { Id = orgHrId,      Code = "HR",      Name = "인사팀",       DeptType = "TEAM",    Level = 1, ParentId = orgCompanyId, IsActive = true },
        };
        await db.Organizations.InsertManyAsync(orgs);

        // 2. 사용자 생성
        var adminId = ObjectId.GenerateNewId().ToString();
        var hrId    = ObjectId.GenerateNewId().ToString();
        var itId    = ObjectId.GenerateNewId().ToString();
        var user1Id = ObjectId.GenerateNewId().ToString();
        var user2Id = ObjectId.GenerateNewId().ToString();

        var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin1234!");
        var userHash  = BCrypt.Net.BCrypt.HashPassword("User1234!");

        var users = new List<User>
        {
            new()
            {
                Id = adminId, EmployeeNo = "E001", Name = "시스템관리자",
                Email = "admin@groupware.com", PasswordHash = adminHash,
                DepartmentId = orgCompanyId, DepartmentName = "(주)그룹웨어",
                PositionCode = "MGR", PositionName = "관리자",
                Roles = new List<string> { nameof(SystemRole.SystemAdmin) },
                HiredAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EmploymentStatus = EmploymentStatus.Active,
            },
            new()
            {
                Id = hrId, EmployeeNo = "E002", Name = "인사팀장",
                Email = "hr@groupware.com", PasswordHash = userHash,
                DepartmentId = orgHrId, DepartmentName = "인사팀",
                PositionCode = "MGR", PositionName = "팀장",
                Roles = new List<string> { nameof(SystemRole.HRAdmin) },
                HiredAt = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                EmploymentStatus = EmploymentStatus.Active,
            },
            new()
            {
                Id = itId, EmployeeNo = "E003", Name = "IT팀장",
                Email = "it@groupware.com", PasswordHash = userHash,
                DepartmentId = orgItId, DepartmentName = "IT개발팀",
                PositionCode = "MGR", PositionName = "팀장",
                Roles = new List<string> { nameof(SystemRole.DepartmentManager) },
                HiredAt = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                EmploymentStatus = EmploymentStatus.Active,
            },
            new()
            {
                Id = user1Id, EmployeeNo = "E004", Name = "홍길동",
                Email = "user1@groupware.com", PasswordHash = userHash,
                DepartmentId = orgItId, DepartmentName = "IT개발팀",
                PositionCode = "EMP", PositionName = "사원",
                Roles = new List<string> { nameof(SystemRole.User) },
                HiredAt = new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                EmploymentStatus = EmploymentStatus.Active,
            },
            new()
            {
                Id = user2Id, EmployeeNo = "E005", Name = "김철수",
                Email = "user2@groupware.com", PasswordHash = userHash,
                DepartmentId = orgHrId, DepartmentName = "인사팀",
                PositionCode = "EMP", PositionName = "사원",
                Roles = new List<string> { nameof(SystemRole.User) },
                HiredAt = new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                EmploymentStatus = EmploymentStatus.Active,
            },
        };
        await db.Users.InsertManyAsync(users);

        // 3. 게시판
        var boardNoticeId = ObjectId.GenerateNewId().ToString();
        var boardFreeId   = ObjectId.GenerateNewId().ToString();
        var boardDataId   = ObjectId.GenerateNewId().ToString();

        var boards = new List<Board>
        {
            new() { Id = boardNoticeId, Code = "NOTICE", Name = "공지사항",   BoardType = BoardType.Notice,  AllowComment = false, AllowAttachment = true,  IsActive = true },
            new() { Id = boardFreeId,   Code = "FREE",   Name = "자유게시판", BoardType = BoardType.Free,    AllowComment = true,  AllowAttachment = true,  IsActive = true },
            new() { Id = boardDataId,   Code = "DATA",   Name = "자료실",     BoardType = BoardType.Library, AllowComment = true,  AllowAttachment = true,  IsActive = true },
        };
        await db.Boards.InsertManyAsync(boards);

        // 4. 공지 게시글
        await db.BoardPosts.InsertOneAsync(new BoardPost
        {
            BoardId = boardNoticeId,
            Title = "그룹웨어 시스템 오픈 안내",
            Content = "<p>안녕하세요. 새로운 그룹웨어 시스템이 오픈되었습니다.</p><p>많은 이용 부탁드립니다.</p>",
            AuthorId = adminId, AuthorName = "시스템관리자", AuthorDept = "(주)그룹웨어",
            IsNotice = true, IsPinned = true,
        });

        // 5. 결재 양식
        var formSchema = "{\"fields\":[{\"key\":\"leaveType\",\"label\":\"휴가종류\",\"type\":\"select\",\"options\":[\"연차\",\"반차(오전)\",\"반차(오후)\",\"병가\"]},{\"key\":\"startDate\",\"label\":\"시작일\",\"type\":\"date\"},{\"key\":\"endDate\",\"label\":\"종료일\",\"type\":\"date\"},{\"key\":\"reason\",\"label\":\"사유\",\"type\":\"textarea\"}]}";
        await db.ApprovalForms.InsertOneAsync(new ApprovalForm
        {
            Code = "LEAVE_REQ", Name = "휴가신청서",
            Description = "연차/반차 등 휴가 신청",
            FormSchema = formSchema,
            IsActive = true,
        });

        // 6. 연차 잔여
        var year = DateTime.UtcNow.Year;
        var leaveBalances = users.Select(u => new LeaveBalance
        {
            UserId = u.Id, Year = year,
            LeaveType = LeaveType.Annual,
            TotalDays = 15, UsedDays = 0,
        }).ToList();
        await db.LeaveBalances.InsertManyAsync(leaveBalances);

        Log.Information("시드 데이터 삽입 완료 - 사용자 {Count}명", users.Count);
    }
}
