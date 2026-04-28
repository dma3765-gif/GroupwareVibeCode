namespace Engine.Domain.Common.Enums;

/// <summary>재직 상태</summary>
public enum EmploymentStatus
{
    Active,    // 재직
    OnLeave,   // 휴직
    Resigned,  // 퇴직
    Retired    // 명예퇴직
}

/// <summary>결재 문서 상태</summary>
public enum ApprovalDocumentStatus
{
    Draft,           // 임시저장
    Submitted,       // 상신
    InApproval,      // 결재진행
    InAgreement,     // 합의진행
    InConsultation,  // 협의진행
    Rejected,        // 반려
    Recalled,        // 회수
    Canceled,        // 취소
    Completed,       // 완료
    PostApproved,    // 후결완료
    Archived,        // 보관
    Deleted          // 삭제
}

/// <summary>결재 참여자 역할</summary>
public enum ApprovalLineRole
{
    Approval,      // 결재
    Agreement,     // 합의
    Consultation,  // 협의
    ReferenceBefore, // 전참조
    ReferenceAfter,  // 후참조
    Receiver,      // 수신처
    Review         // 공람
}

/// <summary>결재 참여자 처리 상태</summary>
public enum ApproverStatus
{
    Pending,           // 대기
    Approved,          // 승인
    Agreed,            // 합의승인
    ConsultedAgree,    // 협의동의
    ConsultedObject,   // 협의이의
    Rejected,          // 반려
    Delegated,         // 대결처리
    Skipped,           // 전결로 인한 생략
    Held               // 보류
}

/// <summary>게시판 유형</summary>
public enum BoardType
{
    Notice,      // 공지사항
    Free,        // 자유게시판
    Department,  // 부서게시판
    Library,     // 자료실
    FAQ,         // FAQ
    Anonymous    // 익명게시판
}

/// <summary>알림 채널</summary>
public enum NotificationChannel
{
    Web,
    Email,
    Push,
    Webhook
}

/// <summary>알림 유형</summary>
public enum NotificationType
{
    ApprovalRequested,     // 결재 요청
    ApprovalApproved,      // 승인
    ApprovalRejected,      // 반려
    ApprovalCompleted,     // 완료
    ApprovalRecalled,      // 회수
    ApprovalReferenced,    // 참조
    BoardNewPost,          // 새 게시글
    BoardComment,          // 댓글
    CalendarReminder,      // 일정 알림
    AttendanceWarning,     // 근태 이상
    SystemNotice,          // 시스템 공지
    LeaveGranted           // 휴가 부여
}

/// <summary>근무 상태</summary>
public enum AttendanceStatus
{
    Normal,         // 정상
    Late,           // 지각
    EarlyLeave,     // 조퇴
    Absent,         // 결근
    BusinessTrip,   // 출장
    Remote,         // 재택
    DayOff,         // 휴가
    Holiday         // 공휴일
}

/// <summary>휴가 유형</summary>
public enum LeaveType
{
    Annual,          // 연차
    HalfDay,         // 반차 (오전)
    HalfDayAfternoon, // 반차 (오후)
    Hourly,          // 시간차
    Special,         // 특별휴가
    Condolence,      // 경조휴가
    Substitute,      // 대체휴가
    Sick             // 병가
}

/// <summary>역할 유형</summary>
public enum SystemRole
{
    SystemAdmin,
    GroupwareAdmin,
    ApprovalAdmin,
    BoardAdmin,
    HRAdmin,
    DepartmentManager,
    User
}

/// <summary>자원 유형</summary>
public enum ResourceType
{
    MeetingRoom, // 회의실
    Vehicle,     // 차량
    Equipment    // 장비
}

/// <summary>근무 형태</summary>
public enum WorkType
{
    Office,       // 사무실 근무
    Remote,       // 재택근무
    BusinessTrip, // 출장
    OvertimeWork, // 연장근무
}

/// <summary>휴가 승인 상태</summary>
public enum LeaveApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Canceled,
}

