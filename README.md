# 그룹웨어 시스템

한국 기업 환경에 맞는 그룹웨어 시스템입니다. 전자결재, 게시판, 캘린더, 근태관리, 조직관리, 알림 기능을 제공합니다.

## 기술 스택

| 영역 | 기술 |
|------|------|
| Backend | .NET 9, ASP.NET Core, SignalR |
| Database | MongoDB 7+ |
| Cache | Redis |
| Frontend | Vue 3, TypeScript, Vite, Element Plus, Pinia |

## 시작하기

### 사전 요구사항

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [MongoDB 7+](https://www.mongodb.com/try/download/community) (로컬 설치 또는 Docker)
- [Redis](https://redis.io/download/) (로컬 설치 또는 Docker)

### Docker로 빠르게 시작

```bash
# MongoDB + Redis 실행
docker run -d --name mongo -p 27017:27017 mongo:7
docker run -d --name redis -p 6379:6379 redis:7
```

### 1단계: DB 초기화

```bash
mongosh groupware < 99.ETC/scripts/seed-data.js
```

### 2단계: 백엔드 실행

```bash
cd 00.Backend
dotnet run --project Engine.Api
```

→ API: http://localhost:5197  
→ Swagger UI: http://localhost:5197/swagger

### 3단계: 프론트엔드 실행

```bash
cd 01.FrontEnd
npm install   # 최초 1회
npm run dev
```

→ 앱: http://localhost:5173

## 테스트 계정

| 역할 | 이메일 | 비밀번호 |
|------|--------|----------|
| 관리자 | admin@groupware.com | Admin1234! |
| 인사담당자 | hr@groupware.com | User1234! |
| IT담당자 | it@groupware.com | User1234! |
| 일반직원1 | user1@groupware.com | User1234! |
| 일반직원2 | user2@groupware.com | User1234! |

## 프로젝트 구조

```
VibeCode/
├── 00.Backend/                  # .NET 9 솔루션
│   ├── Engine.Domain/           # 도메인 엔티티, 열거형
│   ├── Engine.Application/      # 인터페이스, DTO
│   ├── Engine.Infrastructure/   # 서비스 구현, MongoDB 컨텍스트
│   ├── Engine.Api/              # 컨트롤러, 미들웨어, SignalR 허브
│   └── Engine.Worker/           # 백그라운드 작업자
├── 01.FrontEnd/                 # Vue 3 앱
│   └── src/
│       ├── layouts/             # MainLayout
│       ├── views/               # 페이지 컴포넌트
│       ├── stores/              # Pinia 상태
│       ├── router/              # Vue Router
│       └── shared/api/          # Axios 클라이언트
├── 99.ETC/
│   └── scripts/seed-data.js    # MongoDB 초기 데이터
└── README.md
```

## 주요 기능

- **포털**: 대시보드 (결재현황, 근태, 알림, 빠른실행)
- **전자결재**: 결재 기안/승인/반려, 결재선 지정, 양식 관리
- **게시판**: 공지사항, 자유게시판, 자료실, 댓글
- **캘린더**: 일정 등록/수정/삭제, 월별 보기
- **근태관리**: 출퇴근 체크인/아웃, 연차 조회/신청
- **조직관리**: 조직도 트리, 구성원 조회 (관리자: CRUD)
- **알림**: 실시간 알림 (SignalR), 읽음 처리
