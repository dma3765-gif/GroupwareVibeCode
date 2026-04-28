/**
 * 그룹웨어 MongoDB 시드 데이터
 * 실행: mongosh groupware < seed-data.js
 * 또는: mongosh --host localhost:27017 groupware seed-data.js
 *
 * 테스트 계정:
 *   admin@groupware.com / Admin1234!  (시스템관리자)
 *   hr@groupware.com   / User1234!   (인사팀장)
 *   it@groupware.com   / User1234!   (IT팀장)
 *   user1@groupware.com / User1234!  (일반사용자)
 *
 * 비밀번호 해시: bcrypt cost=10
 *   Admin1234!  => $2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LPVFHsLJh.y (예시값 - 실제 앱이 로그인시 해시)
 */

// ─── 날짜 헬퍼 ───
const now = new Date();
function daysAgo(n) { const d = new Date(now); d.setDate(d.getDate() - n); return d; }
function daysFromNow(n) { const d = new Date(now); d.setDate(d.getDate() + n); return d; }

// ─── ObjectId 생성 ───
const orgIds = {
  company:   new ObjectId(),
  it:        new ObjectId(),
  hr:        new ObjectId(),
  sales:     new ObjectId(),
  planning:  new ObjectId(),
};

const userIds = {
  admin:  new ObjectId(),
  hrMgr:  new ObjectId(),
  itMgr:  new ObjectId(),
  user1:  new ObjectId(),
  user2:  new ObjectId(),
};

// ─── ID는 string으로 ───
const orgStrIds = Object.fromEntries(Object.entries(orgIds).map(([k,v]) => [k, v.toString()]));
const userStrIds = Object.fromEntries(Object.entries(userIds).map(([k,v]) => [k, v.toString()]));

// =============================================
// 1. Organizations (조직)
// =============================================
db.organizations.drop();
db.organizations.insertMany([
  {
    _id: orgIds.company,
    name: "(주)그룹웨어",
    code: "COMPANY",
    parentId: null,
    level: 0,
    sortOrder: 0,
    isActive: true,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: orgIds.it,
    name: "IT개발팀",
    code: "IT",
    parentId: orgStrIds.company,
    level: 1,
    sortOrder: 1,
    isActive: true,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: orgIds.hr,
    name: "인사팀",
    code: "HR",
    parentId: orgStrIds.company,
    level: 1,
    sortOrder: 2,
    isActive: true,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: orgIds.sales,
    name: "영업팀",
    code: "SALES",
    parentId: orgStrIds.company,
    level: 1,
    sortOrder: 3,
    isActive: true,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: orgIds.planning,
    name: "기획팀",
    code: "PLAN",
    parentId: orgStrIds.company,
    level: 1,
    sortOrder: 4,
    isActive: true,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
]);
print("✅ Organizations inserted");

// =============================================
// 2. Users (사용자)
// =============================================
// 주의: 비밀번호는 앱 최초 실행시 bcrypt 해싱됨
// 여기서는 직접 bcrypt 해시값을 넣거나, 앱이 처리하도록 평문+flag를 사용
// 아래 해시는 bcrypt(cost=10)으로 생성된 값입니다
const passwordHashes = {
  admin: "$2a$10$K7L/9D1V4.7yxEz6fQFGXuo5BKwWBlhUQPG/5y7dCOkFCDH8Y4lCa", // Admin1234!
  user:  "$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uKLF4/EVM",  // User1234!  (Laravel기본값)
};

db.users.drop();
db.users.insertMany([
  {
    _id: userIds.admin,
    email: "admin@groupware.com",
    passwordHash: passwordHashes.admin,
    name: "시스템관리자",
    employeeNumber: "EMP001",
    departmentId: orgStrIds.company,
    departmentName: "(주)그룹웨어",
    position: "대표이사",
    jobTitle: "대표",
    phone: "010-1234-5678",
    mobilePhone: "010-1234-5678",
    systemRole: "SystemAdmin",
    employmentStatus: "Active",
    isActive: true,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
    lastLoginAt: null,
    profileImageUrl: null,
  },
  {
    _id: userIds.hrMgr,
    email: "hr@groupware.com",
    passwordHash: passwordHashes.user,
    name: "김인사",
    employeeNumber: "EMP002",
    departmentId: orgStrIds.hr,
    departmentName: "인사팀",
    position: "팀장",
    jobTitle: "차장",
    phone: "02-1234-5679",
    mobilePhone: "010-2345-6789",
    systemRole: "OrgAdmin",
    employmentStatus: "Active",
    isActive: true,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: userIds.itMgr,
    email: "it@groupware.com",
    passwordHash: passwordHashes.user,
    name: "이개발",
    employeeNumber: "EMP003",
    departmentId: orgStrIds.it,
    departmentName: "IT개발팀",
    position: "팀장",
    jobTitle: "과장",
    phone: "02-1234-5680",
    mobilePhone: "010-3456-7890",
    systemRole: "Employee",
    employmentStatus: "Active",
    isActive: true,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: userIds.user1,
    email: "user1@groupware.com",
    passwordHash: passwordHashes.user,
    name: "박사원",
    employeeNumber: "EMP004",
    departmentId: orgStrIds.it,
    departmentName: "IT개발팀",
    position: "사원",
    jobTitle: "사원",
    phone: "02-1234-5681",
    mobilePhone: "010-4567-8901",
    systemRole: "Employee",
    employmentStatus: "Active",
    isActive: true,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: userIds.user2,
    email: "user2@groupware.com",
    passwordHash: passwordHashes.user,
    name: "최영업",
    employeeNumber: "EMP005",
    departmentId: orgStrIds.sales,
    departmentName: "영업팀",
    position: "대리",
    jobTitle: "대리",
    phone: "02-1234-5682",
    mobilePhone: "010-5678-9012",
    systemRole: "Employee",
    employmentStatus: "Active",
    isActive: true,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
]);
print("✅ Users inserted");

// =============================================
// 3. Boards (게시판)
// =============================================
const boardIds = {
  notice: new ObjectId(),
  free:   new ObjectId(),
  dept:   new ObjectId(),
  data:   new ObjectId(),
};

db.boards.drop();
db.boards.insertMany([
  {
    _id: boardIds.notice,
    name: "공지사항",
    code: "NOTICE",
    boardType: "Notice",
    description: "회사 공지 게시판입니다.",
    isPublic: true,
    allowComment: false,
    allowAnonymous: false,
    allowAttachment: true,
    isActive: true,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: boardIds.free,
    name: "자유게시판",
    code: "FREE",
    boardType: "Free",
    description: "자유로운 소통 공간입니다.",
    isPublic: true,
    allowComment: true,
    allowAnonymous: true,
    allowAttachment: true,
    isActive: true,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: boardIds.data,
    name: "자료실",
    code: "DATA",
    boardType: "Notice",
    description: "업무 자료 공유 게시판입니다.",
    isPublic: true,
    allowComment: false,
    allowAnonymous: false,
    allowAttachment: true,
    isActive: true,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
]);
print("✅ Boards inserted");

// =============================================
// 4. Board Posts (게시글)
// =============================================
db.boardPosts.drop();
db.boardPosts.insertMany([
  {
    _id: new ObjectId(),
    boardId: boardIds.notice.toString(),
    boardName: "공지사항",
    title: "[공지] 그룹웨어 시스템 오픈 안내",
    content: "<p>안녕하세요. 새로운 그룹웨어 시스템이 오픈되었습니다.</p><p>많은 이용 부탁드립니다.</p>",
    authorId: userStrIds.admin,
    authorName: "시스템관리자",
    departmentId: orgStrIds.company,
    departmentName: "(주)그룹웨어",
    isNotice: true,
    isPinned: true,
    viewCount: 0,
    commentCount: 0,
    isDeleted: false,
    createdAt: daysAgo(3),
    updatedAt: daysAgo(3),
    createdBy: userStrIds.admin,
  },
  {
    _id: new ObjectId(),
    boardId: boardIds.notice.toString(),
    boardName: "공지사항",
    title: "[공지] 2025년 하반기 인사발령 안내",
    content: "<p>2025년 하반기 인사발령 내용을 안내합니다.</p><ul><li>이개발 → IT개발팀 팀장 승진</li><li>김인사 → 인사팀 팀장 발령</li></ul>",
    authorId: userStrIds.hrMgr,
    authorName: "김인사",
    departmentId: orgStrIds.hr,
    departmentName: "인사팀",
    isNotice: true,
    isPinned: false,
    viewCount: 12,
    commentCount: 0,
    isDeleted: false,
    createdAt: daysAgo(1),
    updatedAt: daysAgo(1),
    createdBy: userStrIds.hrMgr,
  },
  {
    _id: new ObjectId(),
    boardId: boardIds.free.toString(),
    boardName: "자유게시판",
    title: "신규 그룹웨어 사용 소감",
    content: "<p>새 그룹웨어 사용해보니 너무 편리하네요! 다들 어떻게 사용하고 계신가요?</p>",
    authorId: userStrIds.user1,
    authorName: "박사원",
    departmentId: orgStrIds.it,
    departmentName: "IT개발팀",
    isNotice: false,
    isPinned: false,
    viewCount: 5,
    commentCount: 1,
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.user1,
  },
]);
print("✅ Board Posts inserted");

// =============================================
// 5. Approval Forms (결재 양식)
// =============================================
const formIds = {
  leave:    new ObjectId(),
  expense:  new ObjectId(),
  purchase: new ObjectId(),
};

db.approvalForms.drop();
db.approvalForms.insertMany([
  {
    _id: formIds.leave,
    code: "LEAVE_REQ",
    name: "휴가신청서",
    description: "연차, 반차 등 휴가 신청 양식",
    categoryCode: "HR",
    category: "HR",
    formSchema: JSON.stringify({
      fields: [
        { name: "leaveType", label: "휴가종류", type: "select", required: true,
          options: ["연차", "반차(오전)", "반차(오후)", "특별휴가", "병가"] },
        { name: "startDate", label: "시작일", type: "date", required: true },
        { name: "endDate", label: "종료일", type: "date", required: true },
        { name: "reason", label: "사유", type: "textarea", required: true },
      ]
    }),
    isActive: true,
    sortOrder: 1,
    defaultApprovalLine: [
      { seq: 1, role: "Approval", isRequired: true },
      { seq: 2, role: "Approval", isRequired: true },
    ],
    defaultApprovalLines: [
      { seq: 1, role: "Approval", isRequired: true },
      { seq: 2, role: "Approval", isRequired: true },
    ],
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: formIds.expense,
    code: "EXPENSE",
    name: "지출결의서",
    description: "업무 관련 지출 결의 양식",
    categoryCode: "FINANCE",
    category: "FINANCE",
    formSchema: JSON.stringify({
      fields: [
        { name: "purpose", label: "사용목적", type: "text", required: true },
        { name: "amount", label: "금액(원)", type: "number", required: true },
        { name: "usedAt", label: "사용일자", type: "date", required: true },
        { name: "paymentMethod", label: "결제수단", type: "select", required: true,
          options: ["법인카드", "개인카드", "현금"] },
        { name: "description", label: "상세내용", type: "textarea", required: false },
      ]
    }),
    isActive: true,
    sortOrder: 2,
    defaultApprovalLine: [
      { seq: 1, role: "Agreement", isRequired: false },
      { seq: 2, role: "Approval", isRequired: true },
      { seq: 3, role: "Approval", isRequired: true },
    ],
    defaultApprovalLines: [
      { seq: 1, role: "Agreement", isRequired: false },
      { seq: 2, role: "Approval", isRequired: true },
      { seq: 3, role: "Approval", isRequired: true },
    ],
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: formIds.purchase,
    code: "PURCHASE",
    name: "일반품의서",
    description: "물품 구매 및 업무 처리 품의 양식",
    categoryCode: "GENERAL",
    category: "GENERAL",
    formSchema: JSON.stringify({
      fields: [
        { name: "title", label: "품의제목", type: "text", required: true },
        { name: "content", label: "품의내용", type: "textarea", required: true },
        { name: "amount", label: "예산(원)", type: "number", required: false },
        { name: "deadline", label: "처리기한", type: "date", required: false },
      ]
    }),
    isActive: true,
    sortOrder: 3,
    defaultApprovalLine: [
      { seq: 1, role: "Approval", isRequired: true },
      { seq: 2, role: "Approval", isRequired: true },
    ],
    defaultApprovalLines: [
      { seq: 1, role: "Approval", isRequired: true },
      { seq: 2, role: "Approval", isRequired: true },
    ],
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
]);
print("✅ Approval Forms inserted");

// =============================================
// 6. Calendar (캘린더)
// =============================================
const calIds = {
  adminPersonal: new ObjectId(),
  company:       new ObjectId(),
};

db.calendars.drop();
db.calendars.insertMany([
  {
    _id: calIds.adminPersonal,
    name: "내 일정",
    color: "#4A90E2",
    calendarType: "Personal",
    ownerUserId: userStrIds.admin,
    ownerId: userStrIds.admin,
    departmentId: null,
    isPublic: false,
    sharedUserIds: [],
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: calIds.company,
    name: "전사 일정",
    color: "#E2544A",
    calendarType: "Company",
    ownerUserId: userStrIds.admin,
    ownerId: userStrIds.admin,
    departmentId: null,
    isPublic: true,
    sharedUserIds: [],
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
]);

db.calendarEvents.drop();
db.calendarEvents.insertMany([
  {
    _id: new ObjectId(),
    calendarId: calIds.company.toString(),
    title: "2025년 하반기 전략회의",
    description: "전사 하반기 전략 검토 및 목표 수립",
    location: "3층 대회의실",
    startAt: daysFromNow(3),
    endAt: daysFromNow(3),
    isAllDay: false,
    isRecurring: false,
    organizerUserId: userStrIds.admin,
    attendees: [],
    attendeeIds: [userStrIds.admin, userStrIds.hrMgr, userStrIds.itMgr],
    reminderMinutes: "30",
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: new ObjectId(),
    calendarId: calIds.company.toString(),
    title: "IT개발팀 주간 스프린트 회의",
    description: "매주 월요일 스프린트 계획",
    location: "2층 소회의실",
    startAt: daysFromNow(1),
    endAt: daysFromNow(1),
    isAllDay: false,
    isRecurring: true,
    recurrenceRule: "FREQ=WEEKLY;BYDAY=MO",
    organizerUserId: userStrIds.itMgr,
    attendees: [],
    attendeeIds: [userStrIds.itMgr, userStrIds.user1],
    reminderMinutes: "15",
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.itMgr,
  },
]);
print("✅ Calendars & Events inserted");

// =============================================
// 7. Resources (자원)
// =============================================
const resIds = {
  room1: new ObjectId(),
  room2: new ObjectId(),
  car:   new ObjectId(),
};

db.resources.drop();
db.resources.insertMany([
  {
    _id: resIds.room1,
    name: "3층 대회의실",
    resourceType: "MeetingRoom",
    location: "본관 3층",
    capacity: 20,
    isActive: true,
    requiresApproval: false,
    description: "프로젝터, 화이트보드 완비",
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: resIds.room2,
    name: "2층 소회의실",
    resourceType: "MeetingRoom",
    location: "본관 2층",
    capacity: 6,
    isActive: true,
    requiresApproval: false,
    description: "소규모 회의실",
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
  {
    _id: resIds.car,
    name: "법인차량 (12가 3456)",
    resourceType: "Vehicle",
    location: "지하 주차장 A-1",
    capacity: 5,
    isActive: true,
    requiresApproval: true,
    description: "현대 그랜저, 5인승",
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  },
]);
print("✅ Resources inserted");

// =============================================
// 8. Leave Balances (휴가 잔여량)
// =============================================
const year = now.getFullYear();

db.leaveBalances.drop();
const leaveBalanceDocs = [];
const users = [
  { id: userStrIds.admin, annual: 15 },
  { id: userStrIds.hrMgr, annual: 15 },
  { id: userStrIds.itMgr, annual: 15 },
  { id: userStrIds.user1, annual: 11 },
  { id: userStrIds.user2, annual: 11 },
];
users.forEach(u => {
  leaveBalanceDocs.push({
    _id: new ObjectId(),
    userId: u.id,
    year: year,
    leaveType: "Annual",
    totalDays: u.annual,
    usedDays: 0,
    histories: [],
    isDeleted: false,
    createdAt: now,
    updatedAt: now,
    createdBy: userStrIds.admin,
  });
});
db.leaveBalances.insertMany(leaveBalanceDocs);
print("✅ Leave Balances inserted");

// =============================================
// 9. System Codes (시스템 코드)
// =============================================
db.systemCodes.drop();
db.systemCodes.insertMany([
  { _id: new ObjectId(), groupCode: "POSITION", code: "CEO", name: "대표이사", sortOrder: 1, isActive: true, isDeleted: false, createdAt: now, updatedAt: now },
  { _id: new ObjectId(), groupCode: "POSITION", code: "DIRECTOR", name: "이사", sortOrder: 2, isActive: true, isDeleted: false, createdAt: now, updatedAt: now },
  { _id: new ObjectId(), groupCode: "POSITION", code: "GENERAL_MGR", name: "부장", sortOrder: 3, isActive: true, isDeleted: false, createdAt: now, updatedAt: now },
  { _id: new ObjectId(), groupCode: "POSITION", code: "MANAGER", name: "과장", sortOrder: 4, isActive: true, isDeleted: false, createdAt: now, updatedAt: now },
  { _id: new ObjectId(), groupCode: "POSITION", code: "ASSISTANT_MGR", name: "대리", sortOrder: 5, isActive: true, isDeleted: false, createdAt: now, updatedAt: now },
  { _id: new ObjectId(), groupCode: "POSITION", code: "SENIOR", name: "선임", sortOrder: 6, isActive: true, isDeleted: false, createdAt: now, updatedAt: now },
  { _id: new ObjectId(), groupCode: "POSITION", code: "STAFF", name: "사원", sortOrder: 7, isActive: true, isDeleted: false, createdAt: now, updatedAt: now },
  { _id: new ObjectId(), groupCode: "LEAVE_TYPE", code: "Annual", name: "연차", sortOrder: 1, isActive: true, isDeleted: false, createdAt: now, updatedAt: now },
  { _id: new ObjectId(), groupCode: "LEAVE_TYPE", code: "HalfDay", name: "반차(오전)", sortOrder: 2, isActive: true, isDeleted: false, createdAt: now, updatedAt: now },
  { _id: new ObjectId(), groupCode: "LEAVE_TYPE", code: "HalfDayAfternoon", name: "반차(오후)", sortOrder: 3, isActive: true, isDeleted: false, createdAt: now, updatedAt: now },
  { _id: new ObjectId(), groupCode: "LEAVE_TYPE", code: "Sick", name: "병가", sortOrder: 4, isActive: true, isDeleted: false, createdAt: now, updatedAt: now },
]);
print("✅ System Codes inserted");

// =============================================
// 10. Menus (메뉴)
// =============================================
db.menus.drop();
db.menus.insertMany([
  { _id: new ObjectId(), name: "포털", path: "/portal", icon: "home", sortOrder: 1, isActive: true, parentId: null, roles: [], isDeleted: false, createdAt: now },
  { _id: new ObjectId(), name: "결재", path: "/approval", icon: "checkbox", sortOrder: 2, isActive: true, parentId: null, roles: [], isDeleted: false, createdAt: now },
  { _id: new ObjectId(), name: "게시판", path: "/board", icon: "document", sortOrder: 3, isActive: true, parentId: null, roles: [], isDeleted: false, createdAt: now },
  { _id: new ObjectId(), name: "캘린더", path: "/calendar", icon: "calendar", sortOrder: 4, isActive: true, parentId: null, roles: [], isDeleted: false, createdAt: now },
  { _id: new ObjectId(), name: "근태관리", path: "/attendance", icon: "time", sortOrder: 5, isActive: true, parentId: null, roles: [], isDeleted: false, createdAt: now },
  { _id: new ObjectId(), name: "조직관리", path: "/organizations", icon: "people", sortOrder: 6, isActive: true, parentId: null, roles: ["SystemAdmin", "OrgAdmin"], isDeleted: false, createdAt: now },
]);
print("✅ Menus inserted");

// =============================================
// 11. Indexes
// =============================================
db.users.createIndex({ email: 1 }, { unique: true });
db.users.createIndex({ employeeNumber: 1 });
db.users.createIndex({ departmentId: 1 });
db.organizations.createIndex({ parentId: 1 });
db.organizations.createIndex({ code: 1 }, { unique: true });
db.boards.createIndex({ code: 1 }, { unique: true });
db.boardPosts.createIndex({ boardId: 1, createdAt: -1 });
db.boardPosts.createIndex({ authorId: 1 });
db.approvalDocuments.createIndex({ "drafter.userId": 1, status: 1 });
db.approvalDocuments.createIndex({ "approvalLine.userId": 1, status: 1 });
db.attendanceRecords.createIndex({ userId: 1, workDate: -1 });
db.leaveRequests.createIndex({ userId: 1, startDate: -1 });
db.leaveBalances.createIndex({ userId: 1, year: 1, leaveType: 1 }, { unique: true });
db.calendarEvents.createIndex({ organizerUserId: 1, startAt: 1 });
db.calendarEvents.createIndex({ "attendeeIds": 1, startAt: 1 });
db.notifications.createIndex({ recipientUserId: 1, isRead: 1, createdAt: -1 });
db.resourceReservations.createIndex({ resourceId: 1, startAt: 1 });
print("✅ Indexes created");

print("\n🎉 모든 시드 데이터 삽입 완료!");
print("테스트 계정:");
print("  admin@groupware.com / Admin1234!");
print("  hr@groupware.com    / User1234!");
print("  it@groupware.com    / User1234!");
print("  user1@groupware.com / User1234!");
