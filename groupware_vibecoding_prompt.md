# 대한민국형 그룹웨어 구축을 위한 바이브코딩 프롬프트

> 목적: 이 문서는 AI 코딩 도구에게 **대한민국 기업 환경에 맞는 그룹웨어**를 설계·구현시키기 위한 상위 프롬프트이다.  
> 단순 CRUD 예제가 아니라, 포탈, 전자결재, 게시판, 조직도, 근태, 일정, 알림, 문서관리, 권한, 감사로그까지 포함하는 **실무형 업무 플랫폼**을 목표로 한다.

---

## 0. 역할 지시

너는 다음 역할을 동시에 수행한다.

1. **대한민국 그룹웨어 도메인 전문가**
   - 전자결재, 조직도, 게시판, 문서함, 근태, 일정, 알림, 권한, 감사로그 등 국내 기업형 그룹웨어의 표준 업무 흐름을 이해한다.
   - 한국 기업의 결재 문화, 부서 문서함, 참조/수신/발신 문서함, 전결, 합의, 협의, 대결, 후결, 보존연한, 문서번호 체계를 고려한다.

2. **.NET 10 백엔드 아키텍트**
   - ASP.NET Core 기반 REST API, OAuth2/JWT 인증, 정책 기반 권한, OpenAPI 문서화, 미들웨어, DI, Repository/Service 분리, 감사로그, 예외처리, Background Job 구조를 설계한다.

3. **VueJS 3.x 프론트엔드 아키텍트**
   - Composition API, Pinia, Vue Router, Axios, 공통 API 래퍼, 공통 응답 처리, 권한 기반 라우팅, 컴포넌트 재사용성을 고려한다.

4. **NoSQL 데이터 모델러**
   - 데이터베이스는 NoSQL 중 트렌드와 생태계가 강한 **MongoDB**를 기본으로 한다.
   - 단, 전자결재처럼 트랜잭션 정합성이 중요한 영역은 MongoDB Transaction, Change Stream, Outbox Pattern, 이벤트 로그를 적극 활용한다.

5. **보안·운영 설계자**
   - 모든 요청/응답은 공통 래퍼를 통해 처리한다.
   - 요청 헤더 옵션에 따라 암호화/복호화를 선택적으로 수행한다.
   - 인증, 인가, 개인정보보호, 감사추적, 첨부파일 보안, API Rate Limit, CORS, CSP, 로그 마스킹을 고려한다.

---

## 1. 기술 스택

### 1.1 Backend

- Runtime: **.NET 10**
- Framework: **ASP.NET Core 10 Web API**
- Language: C# 최신 버전
- API Style: REST API 우선
- Auth: OAuth2 + JWT Bearer Token
- Authorization: Role + Permission + Policy 기반 혼합 구조
- API Documentation: OpenAPI 3.1 / Swagger
- Validation: FluentValidation 또는 표준 DataAnnotation + 커스텀 Validator
- Background Job: Hangfire, Quartz.NET 또는 .NET Worker Service 중 적합한 방식 제안
- Logging: Serilog + 구조화 로그
- Cache: Redis 권장
- File Storage: 로컬 개발용 FileSystem, 운영용 Object Storage 호환 구조
- Realtime: SignalR 사용 가능

### 1.2 Frontend

- Framework: **VueJS 3.x**
- Language: TypeScript
- Build Tool: Vite
- State Management: Pinia
- Router: Vue Router
- HTTP Client: Axios
- UI Component: Element Plus, PrimeVue, Vuetify 중 하나를 선택하되, 엔터프라이즈 업무 화면 생산성이 높은 것을 우선한다.
- Form: 동적 양식 렌더링을 고려한 Form Schema 기반 구조
- Rich Text Editor: 게시판/공지/결재 양식용 에디터 필요
- File Upload: 대용량 업로드, 다중 첨부, 확장자 제한, 바이러스 검사 연계 고려

### 1.3 Database

- Primary DB: **MongoDB**
- Cache/Session/Lock: Redis
- Search: MongoDB Atlas Search 또는 OpenSearch/Elasticsearch 연동 가능 구조
- File Metadata: MongoDB
- File Binary: Object Storage 또는 GridFS 중 선택 가능하나, 운영 환경은 Object Storage 우선

### 1.4 공통 통신 규칙

모든 Frontend → Backend 호출은 반드시 단일 래핑 서비스를 통해 수행한다.

- Frontend: `ApiClient`, `SecureApiClient`, `ApprovalApiService`, `BoardApiService` 등은 내부적으로 공통 Axios Wrapper를 사용한다.
- Backend: 모든 API 응답은 `ApiResponse<T>` 형태로 반환한다.
- 예외도 공통 Exception Middleware에서 `ApiResponse<ErrorPayload>` 형태로 변환한다.
- 요청 헤더에 따라 암복호화 옵션을 적용한다.

예시 헤더:

```http
X-Request-Id: {uuid}
X-Client-Timezone: Asia/Seoul
X-Api-Version: v1
X-Crypto-Mode: none | request | response | both
X-Crypto-Key-Id: key-id
Authorization: Bearer {access_token}
```

공통 응답 예시:

```json
{
  "success": true,
  "code": "OK",
  "message": "처리되었습니다.",
  "data": {},
  "traceId": "uuid",
  "timestamp": "2026-04-28T10:00:00+09:00"
}
```

오류 응답 예시:

```json
{
  "success": false,
  "code": "APPROVAL_DOCUMENT_NOT_FOUND",
  "message": "결재 문서를 찾을 수 없습니다.",
  "errors": [
    {
      "field": "documentId",
      "reason": "존재하지 않는 문서 ID입니다."
    }
  ],
  "traceId": "uuid",
  "timestamp": "2026-04-28T10:00:00+09:00"
}
```

---

## 2. 프로젝트 구조

아래 초안을 기준으로 하되, 업무 도메인별 모듈화를 강화한다.

```text
00.Backend
  Engine.Api
    Controllers
      Portal
      Board
      Approval
      Organization
      Attendance
      Calendar
      Notification
      Document
      Admin
    Middlewares
    Filters
    Extensions
    Program.cs

  Engine.Application
    Common
      DTOs
      Responses
      Exceptions
      Interfaces
      Validators
    Portal
    Board
    Approval
    Organization
    Attendance
    Calendar
    Notification
    Document
    Admin

  Engine.Domain
    Common
      Entities
      ValueObjects
      Enums
      Events
    Portal
    Board
    Approval
    Organization
    Attendance
    Calendar
    Notification
    Document
    Security

  Engine.Infrastructure
    Persistence
      Mongo
      Repositories
      UnitOfWork
    Security
      OAuth
      Jwt
      Crypto
    Storage
    Cache
    Search
    Messaging
    Logging

  Engine.Worker
    Jobs
      NotificationJobs
      ApprovalReminderJobs
      DocumentRetentionJobs
      AttendanceJobs

01.FrontEnd
  src
    app
      router
      stores
      plugins
    shared
      api
      components
      composables
      layouts
      utils
      types
      constants
    modules
      portal
      board
      approval
      organization
      attendance
      calendar
      notification
      document
      admin
    assets
    main.ts

99.ETC
  docs
    api
    database
    security
    deployment
    prompts
  scripts
  docker
```

### 2.1 핵심 구조 원칙

- Controller는 HTTP 입출력만 담당한다.
- Service/Application Layer는 비즈니스 유스케이스를 담당한다.
- Repository는 MongoDB 접근만 담당한다.
- Domain Layer는 도메인 규칙, 상태 전이, 이벤트 정의를 포함한다.
- Frontend는 `modules/{domain}` 기준으로 화면, API, 타입, 컴포저블을 분리한다.
- 공통 Axios Wrapper 외부에서 직접 `axios.get`, `axios.post`를 호출하지 않는다.
- 모든 API는 Request DTO, Response DTO를 명시한다.

---

## 3. 핵심 도메인 기능

## 3.1 포탈

### 목표

사용자가 로그인 후 가장 먼저 보는 업무 허브 화면을 제공한다.

### 주요 기능

1. 메인 웹파트 대시보드
   - 공지사항
   - 최근 게시글
   - 결재 대기 문서
   - 결재 진행 문서
   - 오늘 일정
   - 나의 근태 상태
   - 휴가 잔여일
   - 부서 소식
   - 회사 소식
   - 날씨
   - 세계시간
   - 바로가기 링크
   - 생일자/입사기념일
   - 시스템 알림

2. 개인화
   - 웹파트 추가/삭제
   - 웹파트 순서 변경
   - 웹파트 크기 변경
   - 사용자별 포탈 레이아웃 저장
   - 관리자 기본 레이아웃 배포

3. 관리자 기능
   - 회사 공통 웹파트 관리
   - 포탈 배너 관리
   - 긴급 공지 노출
   - 팝업 공지 관리

### 구현 요구사항

- 포탈 웹파트는 동적 컴포넌트 구조로 설계한다.
- 각 웹파트는 독립 API를 가진다.
- 포탈 진입 시 모든 데이터를 한 번에 과도하게 호출하지 말고, 중요 웹파트 우선 로딩 + Lazy Loading을 적용한다.

---

## 3.2 게시판

### 목표

공지, 자유게시판, 자료실, 부서게시판, FAQ 등 다양한 게시판 유형을 지원한다.

### 주요 기능

1. 게시판 유형
   - 공지사항
   - 자유게시판
   - 부서게시판
   - 자료실
   - FAQ
   - 익명게시판 옵션
   - 전사/부서/권한그룹별 게시판

2. 게시글 기능
   - 작성, 수정, 삭제
   - 임시저장
   - 상단 고정
   - 중요 공지
   - 게시 기간 설정
   - 예약 게시
   - 만료 처리
   - 조회수
   - 좋아요/추천
   - 댓글/대댓글
   - 첨부파일
   - 태그
   - 카테고리

3. 검색/정렬/페이징
   - 기간 검색
   - 제목/내용/작성자/첨부파일명 키워드 검색
   - 카테고리 필터
   - 정렬: 최신순, 조회순, 댓글순, 중요도순
   - 페이징 또는 무한스크롤

4. 권한
   - 게시판별 읽기/쓰기/수정/삭제/관리 권한
   - 작성자 본인 수정 가능 옵션
   - 관리자 강제 삭제/숨김
   - 부서 게시판 자동 권한 매핑

5. 감사로그
   - 게시글 생성/수정/삭제/조회 이력
   - 첨부파일 다운로드 이력

### 구현 요구사항

- 게시글 본문은 HTML Sanitizing을 적용한다.
- 첨부파일 확장자, 크기, MIME Type 검증을 수행한다.
- 검색 인덱스 설계를 포함한다.

---

## 3.3 전자결재

### 목표

대한민국 기업 업무 환경에 맞는 전자결재 시스템을 구현한다. 단순 승인 기능이 아니라, 결재선, 합의, 협의, 전결, 참조, 문서함, 양식, 후처리, 알림, 감사추적을 포함한다.

---

### 3.3.1 결재 양식

#### 기본 제공 양식

- 휴가신청서
- 일반품의서
- 지출결의서
- 출장신청서
- 출장보고서
- 구매요청서
- 교육신청서
- 연장근무신청서
- 재택근무신청서
- 업무협조전
- 기안문
- 경조금 신청서

#### 양식 기능

- 관리자 양식 생성/수정/폐기
- 동적 필드 구성
  - 텍스트
  - 숫자
  - 날짜
  - 기간
  - 금액
  - 사용자 선택
  - 부서 선택
  - 체크박스
  - 라디오
  - 셀렉트박스
  - 테이블형 반복 입력
  - 첨부파일
  - 에디터 본문
- 양식별 기본 결재선 지정
- 양식별 문서번호 규칙 지정
- 양식별 후처리 Hook 지정
- 양식별 보존연한 지정
- 양식별 공개/비공개 설정
- 양식 버전 관리

#### 양식 렌더링 방식

- Frontend는 JSON Schema 기반 동적 렌더링을 지원한다.
- Backend는 제출된 양식 데이터에 대해 서버 검증을 수행한다.
- 양식 버전이 변경되어도 기존 문서는 작성 당시의 양식 버전으로 조회 가능해야 한다.

---

### 3.3.2 결재선 유형

결재선은 다음 표현 방식을 모두 지원한다.

1. 테이블형 결재선
   - 단계, 역할, 사용자, 부서, 처리상태, 처리일시, 의견 표시

2. 도장/서명형 결재선
   - 과거 오프라인 결재문서처럼 결재칸에 이름, 직위, 도장 또는 서명 이미지를 표시
   - 출력/PDF 변환 시 레이아웃 유지

3. 혼합형
   - 상단에는 도장형, 하단에는 상세 이력 테이블 표시

---

### 3.3.3 결재 참여자 역할

#### 결재

- 기본 결재권자
- 문서 상태에 영향을 준다.
- 수행 가능 액션:
  - 승인
  - 반려
  - 보류
  - 의견 작성

#### 합의

- 합의 진행 결재권자
- 문서 상태에 영향을 준다.
- 수행 가능 액션:
  - 합의승인
  - 반려
  - 보류
  - 의견 작성

#### 협의

- 협의 진행자
- 문서 상태에는 영향을 주지 않는다.
- 수행 가능 액션:
  - 동의
  - 이의
  - 의견 작성
- 협의 결과는 결재 이력에 남긴다.

#### 전결

- 전결 권한을 가진 결재자
- 문서 상태에 영향을 준다.
- 수행 가능 액션:
  - 전결승인
  - 반려
- 전결승인 시 즉시 문서 완료 상태가 된다.
- 전결 가능 여부는 양식, 직위, 권한, 금액 조건에 따라 제한할 수 있어야 한다.

#### 전참조

- 결재 상신 시 알림을 받는 사용자
- 결재 진행 중 문서를 조회할 수 있다.
- 승인/반려 권한은 없다.

#### 후참조

- 결재 완료 시 알림을 받는 사용자
- 완료 이후 문서를 조회할 수 있다.

#### 수신처

- 결재 완료 문서를 수신하는 부서 또는 사용자
- 부서 수신함에 문서가 들어간다.

#### 공람

- 문서 완료 후 열람 대상으로 지정되는 사용자 또는 부서
- 공람 확인 여부를 기록한다.

#### 대결

- 부재 중인 결재자를 대신해 결재하는 사용자
- 대결자의 처리 이력과 원 결재자를 모두 남긴다.
- 대결 가능 기간, 대상 양식, 대상 사용자 조건을 둔다.

#### 후결

- 긴급 상황에서 선처리 후 사후 결재를 받는 절차
- 후결 문서는 별도 상태와 감사로그를 남긴다.

---

### 3.3.4 결재 프로세스 상태

문서 상태 예시:

```text
Draft           임시저장
Submitted       상신
InApproval      결재진행
InAgreement     합의진행
InConsultation  협의진행
Rejected        반려
Recalled        회수
Canceled        취소
Completed       완료
PostApproved    후결완료
Archived        보관
Deleted         삭제
```

결재자별 처리 상태 예시:

```text
Pending         대기
Approved        승인
Agreed          합의승인
ConsultedAgree  협의동의
ConsultedObject 협의이의
Rejected        반려
Delegated       대결처리
Skipped         전결로 인한 생략
Held            보류
```

### 상태 전이 규칙

- 작성자는 `Draft` 상태 문서를 수정/삭제할 수 있다.
- 작성자는 `Submitted` 이후에도 첫 결재자 처리 전까지 회수할 수 있다.
- 결재자가 반려하면 문서는 `Rejected`가 된다.
- 최종 결재자가 승인하면 `Completed`가 된다.
- 전결자가 전결승인하면 즉시 `Completed`가 된다.
- 협의자는 문서 상태를 변경하지 않는다.
- 합의자가 반려하면 문서는 `Rejected`가 된다.
- 결재 완료 후 후처리 실패 시 문서 상태와 후처리 상태를 분리 관리한다.

---

### 3.3.5 문서함

#### 개인 문서함

- 기안함
- 결재함
- 결재대기함
- 결재진행함
- 반려함
- 완료함
- 임시저장함
- 참조함
- 예고함
- 회수함
- 공람함

#### 부서 문서함

- 부서수신함
- 부서발신함
- 부서참조함
- 부서공람함
- 부서보관함

#### 관리자 문서함

- 전체 문서 조회
- 문서 상태 강제 보정
- 삭제 문서 복원
- 결재선 변경 이력 조회
- 문서 보존/폐기 관리

---

### 3.3.6 조직도 연계

결재선 선택 시 조직도를 구성한다.

필수 기능:

- 회사/부문/본부/팀/파트 계층 구조
- 사용자 검색
- 부서 검색
- 직위/직책/직급 표시
- 겸직 지원
- 부서장 자동 선택
- 상위 부서장 자동 결재선 생성
- 사용자 즐겨찾기
- 최근 선택 결재선
- 개인 결재선 템플릿
- 양식별 기본 결재선
- 부재/대결자 표시

---

### 3.3.7 후처리

결재 완료 시 양식별 후처리를 수행한다.

예시:

- 휴가신청서 완료 → 휴가 사용일 차감, 근태 일정 생성
- 연장근무신청서 완료 → 근무시간 반영
- 지출결의서 완료 → 회계 연계 대기 데이터 생성
- 구매요청서 완료 → 구매 요청 상태 변경
- 출장신청서 완료 → 일정 등록

후처리 요구사항:

- 후처리는 반드시 idempotent 해야 한다.
- 후처리 성공/실패 상태를 별도로 기록한다.
- 실패 시 재처리 기능을 제공한다.
- 후처리 로직은 양식별 Hook 또는 Strategy Pattern으로 분리한다.

---

### 3.3.8 알림

전자결재 알림 이벤트:

- 상신 완료
- 결재 요청
- 합의 요청
- 협의 요청
- 참조 알림
- 반려
- 완료
- 회수
- 대결 지정
- 결재 지연
- 후처리 실패

알림 채널:

- 웹 알림
- 이메일
- 모바일 푸시 확장 가능 구조
- Teams/Webhook 확장 가능 구조

---

## 3.4 조직도 / 인사 기본정보

### 목표

그룹웨어의 모든 권한, 결재, 근태, 일정, 문서 수신의 기준이 되는 조직·사용자 마스터를 제공한다.

### 주요 기능

1. 조직 관리
   - 회사
   - 사업부
   - 본부
   - 부서
   - 팀
   - 파트
   - 조직 개편 이력
   - 사용 여부

2. 사용자 관리
   - 사번
   - 이름
   - 이메일
   - UPN
   - 휴대폰
   - 직위
   - 직책
   - 직급
   - 부서
   - 입사일
   - 퇴사일
   - 재직상태
   - 프로필 이미지

3. 겸직/겸무
   - 주 부서
   - 겸직 부서
   - 겸직 권한
   - 겸직 기간

4. 인사 연동
   - HR DB 연동 가능 구조
   - M365/Azure AD/Entra ID 연동 가능 구조
   - 배치 동기화
   - 변경 이력 관리

5. 조직도 UI
   - 트리 구조
   - 사용자 카드
   - 부서별 사용자 목록
   - 검색
   - 즐겨찾기
   - 결재선 선택 모드

---

## 3.5 근태 / 휴가

### 목표

국내 그룹웨어에서 자주 요구되는 근태, 휴가, 연장근무, 출장, 재택근무 기능을 제공한다.

### 주요 기능

1. 출퇴근
   - 출근 체크
   - 퇴근 체크
   - 외근 체크
   - 재택근무 체크
   - 위치/IP 기반 제한 옵션
   - 근무 상태 표시

2. 휴가
   - 연차 생성
   - 반차
   - 시간차
   - 특별휴가
   - 경조휴가
   - 대체휴가
   - 잔여 휴가 조회
   - 휴가 사용 이력
   - 휴가 촉진 알림

3. 전자결재 연동
   - 휴가신청서 승인 시 휴가 차감
   - 출장신청서 승인 시 출장 일정 등록
   - 연장근무신청서 승인 시 근무시간 반영

4. 관리자 기능
   - 부서원 근태 조회
   - 근태 이상자 조회
   - 월별 근태 마감
   - 휴가 부여/차감 수동 조정
   - 근무제 설정

---

## 3.6 일정 / 자원예약

### 주요 기능

1. 일정
   - 개인 일정
   - 부서 일정
   - 전사 일정
   - 공유 일정
   - 반복 일정
   - 참석자 초대
   - 일정 알림
   - 결재 문서와 일정 연동

2. 자원예약
   - 회의실 예약
   - 차량 예약
   - 장비 예약
   - 예약 승인 옵션
   - 중복 예약 방지
   - 예약 현황 캘린더

3. 캘린더 UI
   - 월/주/일 보기
   - 리스트 보기
   - 드래그 앤 드롭 변경
   - 권한별 공개 범위 제어

---

## 3.7 문서관리 / 자료실

### 주요 기능

- 문서 업로드
- 폴더 구조
- 문서 버전 관리
- 체크인/체크아웃 옵션
- 문서 권한
- 다운로드 이력
- 보존연한
- 폐기 요청
- 태그
- OCR/본문 검색 확장 가능 구조
- 전자결재 완료 문서의 PDF 보관

---

## 3.8 메일 / 쪽지 / 주소록 연계

자체 메일 서버를 구현하지 않더라도 그룹웨어 기능으로 다음 연계를 고려한다.

- 외부 메일 시스템 링크
- 개인 주소록
- 부서 주소록
- 공용 주소록
- 쪽지 또는 내부 메시지
- 결재/게시판/일정 알림 메시지
- Outlook 또는 M365 연동 확장점

---

## 3.9 업무관리 / 협업

### 주요 기능

- 업무 생성
- 담당자 지정
- 시작일/마감일
- 진행상태
- 우선순위
- 댓글
- 첨부파일
- 하위 업무
- 칸반 보드
- 부서 업무함
- 결재 문서와 업무 연결

---

## 3.10 알림 센터

### 주요 기능

- 전체 알림 목록
- 읽음/안읽음
- 도메인별 필터
- 결재 알림
- 게시판 알림
- 일정 알림
- 근태 알림
- 시스템 알림
- 알림 템플릿 관리
- 사용자별 알림 수신 설정

### 실시간 처리

- SignalR 기반 실시간 알림을 고려한다.
- 알림은 MongoDB에 저장하고, Redis Pub/Sub 또는 Message Queue를 통해 확장 가능하게 설계한다.

---

## 3.11 관리자 기능

### 주요 기능

1. 사용자/조직 관리
2. 권한/역할 관리
3. 메뉴 관리
4. 코드 관리
5. 게시판 관리
6. 결재 양식 관리
7. 결재 권한 관리
8. 근태 정책 관리
9. 알림 템플릿 관리
10. 파일 정책 관리
11. 시스템 설정
12. 감사로그 조회
13. 접속 로그 조회
14. 배치 실행 이력 조회
15. API 호출 이력 조회

---

## 4. 권한 설계

### 4.1 권한 모델

다음 3계층 권한 모델을 사용한다.

1. Role
   - SystemAdmin
   - GroupwareAdmin
   - ApprovalAdmin
   - BoardAdmin
   - HRAdmin
   - DepartmentManager
   - User

2. Permission
   - `board.read`
   - `board.write`
   - `approval.draft`
   - `approval.approve`
   - `approval.admin`
   - `organization.manage`
   - `attendance.manage`

3. Resource Policy
   - 특정 게시판
   - 특정 부서
   - 특정 문서함
   - 특정 결재 양식
   - 특정 일정 캘린더

### 4.2 권한 검증 원칙

- Frontend 권한 제어는 UX 목적이다.
- 실제 권한 검증은 Backend에서 반드시 수행한다.
- 모든 민감 API는 사용자 ID, 역할, 권한, 리소스 소유권을 검증한다.
- 감사로그는 권한 실패도 기록할 수 있게 설계한다.

---

## 5. 인증 / 보안

### 5.1 OAuth2/JWT

- Access Token + Refresh Token 구조
- Access Token은 짧은 만료 시간
- Refresh Token은 저장소에 해시로 저장
- Refresh Token Rotation 적용
- 로그아웃 시 Refresh Token 폐기
- 관리자 강제 로그아웃 기능

### 5.2 암복호화

요청 헤더 `X-Crypto-Mode`에 따라 암복호화를 적용한다.

- `none`: 평문 JSON
- `request`: 요청 Body 암호화
- `response`: 응답 Body 암호화
- `both`: 요청/응답 모두 암호화

설계 원칙:

- TLS는 기본 전제이며, Body 암호화는 추가 보호 계층이다.
- 대칭키는 직접 하드코딩하지 않는다.
- `X-Crypto-Key-Id`로 키 버전을 식별한다.
- 서버는 키 관리 서비스를 추상화한다.
- 민감 필드는 DB 저장 시 필드 레벨 암호화를 고려한다.

### 5.3 보안 필수 항목

- CORS 제한
- CSP 적용
- XSS 방어
- CSRF 검토
- 파일 업로드 확장자 제한
- 파일 MIME Type 검증
- 바이러스 검사 연계 확장점
- SQL Injection 대신 NoSQL Injection 방어
- MongoDB Query Sanitizing
- Rate Limiting
- 로그인 실패 제한
- 감사로그
- 개인정보 마스킹
- 로그 민감정보 제거

---

## 6. MongoDB 데이터 모델 초안

### 6.1 공통 필드

모든 주요 컬렉션은 다음 필드를 가진다.

```json
{
  "_id": "ObjectId",
  "tenantId": "string",
  "createdAt": "datetime",
  "createdBy": "userId",
  "updatedAt": "datetime",
  "updatedBy": "userId",
  "isDeleted": false,
  "deletedAt": null,
  "deletedBy": null
}
```

### 6.2 주요 컬렉션

```text
users
organizations
positions
roles
permissions
userRoles
menus
boards
boardPosts
boardComments
files
approvalForms
approvalDocuments
approvalLines
approvalHistories
approvalPostProcesses
approvalDocumentBoxes
attendanceRecords
leaveBalances
leaveRequests
calendars
events
resources
resourceReservations
notifications
auditLogs
systemCodes
systemSettings
```

### 6.3 전자결재 문서 예시

```json
{
  "_id": "ObjectId",
  "tenantId": "companyA",
  "formId": "ObjectId",
  "formVersion": 3,
  "documentNo": "APP-2026-000001",
  "title": "휴가신청서",
  "drafter": {
    "userId": "u001",
    "name": "홍길동",
    "departmentId": "d001",
    "departmentName": "개발팀",
    "positionName": "팀장"
  },
  "status": "InApproval",
  "formData": {
    "leaveType": "Annual",
    "startDate": "2026-05-01",
    "endDate": "2026-05-02",
    "reason": "개인 사유"
  },
  "approvalLine": [
    {
      "seq": 1,
      "type": "Approval",
      "userId": "u010",
      "name": "김부장",
      "status": "Pending",
      "actedAt": null,
      "comment": null
    }
  ],
  "referencesBefore": [],
  "referencesAfter": [],
  "receivers": [],
  "attachments": [],
  "postProcessStatus": "Pending",
  "createdAt": "2026-04-28T10:00:00+09:00"
}
```

---

## 7. API 설계 원칙

### 7.1 URL 규칙

```text
/api/v1/portal
/api/v1/boards
/api/v1/boards/{boardId}/posts
/api/v1/approval/forms
/api/v1/approval/documents
/api/v1/approval/documents/{documentId}/submit
/api/v1/approval/documents/{documentId}/approve
/api/v1/approval/documents/{documentId}/reject
/api/v1/organizations
/api/v1/attendance
/api/v1/calendars
/api/v1/notifications
/api/v1/admin
```

### 7.2 Controller 예시

- `ApprovalDocumentsController`
  - `POST /api/v1/approval/documents/draft`
  - `POST /api/v1/approval/documents/{id}/submit`
  - `POST /api/v1/approval/documents/{id}/approve`
  - `POST /api/v1/approval/documents/{id}/reject`
  - `POST /api/v1/approval/documents/{id}/recall`
  - `GET /api/v1/approval/documents/{id}`
  - `GET /api/v1/approval/boxes/my/drafts`
  - `GET /api/v1/approval/boxes/my/pending`
  - `GET /api/v1/approval/boxes/departments/{departmentId}/received`

### 7.3 공통 처리

- 모든 목록 API는 검색, 정렬, 페이징 DTO를 공통화한다.
- 모든 쓰기 API는 감사로그를 남긴다.
- 모든 상태 변경 API는 동시성 충돌을 고려한다.
- 중복 요청 방지를 위해 Idempotency-Key 헤더를 고려한다.

---

## 8. Frontend 설계

### 8.1 Axios Wrapper

요구사항:

- Access Token 자동 첨부
- Refresh Token 자동 갱신
- 401 처리
- 공통 오류 메시지 처리
- `X-Request-Id` 자동 생성
- `X-Crypto-Mode` 옵션 처리
- 응답 복호화
- 요청 암호화
- 로딩 상태 제어
- 중복 요청 취소 옵션

### 8.2 화면 모듈

```text
modules/approval
  api
  components
  composables
  pages
  routes.ts
  types.ts

modules/board
  api
  components
  composables
  pages
  routes.ts
  types.ts
```

### 8.3 공통 컴포넌트

- AppLayout
- SideMenu
- TopBar
- Breadcrumb
- DataTable
- SearchPanel
- Pagination
- FileUploader
- UserPicker
- DepartmentTree
- ApprovalLineEditor
- DynamicFormRenderer
- ConfirmDialog
- Toast
- LoadingOverlay

---

## 9. 전자결재 구현 상세 지시

AI 코딩 도구는 전자결재를 단순 CRUD로 만들지 말고 다음 순서로 구현한다.

1. 결재 도메인 Enum 정의
2. 결재 문서 Aggregate 설계
3. 결재선 Value Object 설계
4. 상태 전이 메서드 구현
   - Submit
   - Approve
   - Agree
   - Consult
   - Reject
   - Recall
   - DelegateApprove
   - FinalApprove
5. 결재 이력 기록
6. 결재함 조회 쿼리 구현
7. 결재 알림 이벤트 발행
8. 후처리 Strategy 구현
9. 테스트 코드 작성

필수 테스트:

- 기안자가 문서를 상신할 수 있다.
- 첫 결재자가 승인하면 다음 결재자에게 넘어간다.
- 중간 결재자가 반려하면 문서는 반려 상태가 된다.
- 합의자가 반려하면 문서는 반려 상태가 된다.
- 협의자가 이의 처리해도 문서는 계속 진행된다.
- 전결자가 전결승인하면 문서는 즉시 완료된다.
- 첫 결재 전 기안자는 문서를 회수할 수 있다.
- 완료 문서는 일반 사용자가 수정할 수 없다.

---

## 10. 개발 순서

### Phase 1. 기반 구조

- Backend Solution 생성
- Frontend Vite 프로젝트 생성
- MongoDB 연결
- Redis 연결
- 공통 응답 모델
- 공통 예외 처리
- OAuth2/JWT 인증
- Axios Wrapper
- 기본 레이아웃
- 메뉴/권한 구조

### Phase 2. 조직도 / 사용자 / 권한

- 사용자 관리
- 조직 관리
- 역할/권한 관리
- 조직도 UI
- 사용자 선택 팝업
- 부서 선택 팝업

### Phase 3. 게시판

- 게시판 관리
- 게시글 CRUD
- 검색/정렬/페이징
- 댓글
- 첨부파일
- 권한

### Phase 4. 전자결재 MVP

- 양식 관리
- 동적 양식 렌더링
- 결재선 선택
- 문서 임시저장
- 상신
- 승인/반려
- 결재함
- 알림

### Phase 5. 전자결재 고도화

- 합의/협의
- 전결
- 전참조/후참조
- 부서문서함
- 대결/후결
- PDF 출력
- 후처리
- 감사로그

### Phase 6. 포탈 / 일정 / 근태

- 포탈 웹파트
- 일정
- 자원예약
- 근태
- 휴가
- 전자결재 연동

### Phase 7. 운영 기능

- 관리자 대시보드
- 감사로그
- 배치 관리
- 알림 템플릿
- 문서 보존/폐기
- 성능 최적화
- 배포 자동화

---

## 11. 산출물 요구사항

AI 코딩 도구는 기능 구현 시 다음 산출물을 함께 작성한다.

1. Backend
   - Entity/Document Model
   - DTO
   - Controller
   - Service
   - Repository
   - Validator
   - Middleware
   - Unit Test
   - Integration Test

2. Frontend
   - Page Component
   - Domain Component
   - API Service
   - Type Definition
   - Store
   - Composable
   - Router
   - Form Schema

3. 문서
   - API 명세
   - MongoDB 컬렉션 설계
   - 권한 매트릭스
   - 상태 전이표
   - 전자결재 시퀀스 다이어그램
   - 배포 가이드

---

## 12. 코딩 스타일

### Backend

- Nullable Reference Type 활성화
- Async/Await 사용
- CancellationToken 전달
- Result Pattern 또는 예외 기반 중 하나로 일관성 유지
- Service에서 직접 HTTP Context에 의존하지 않기
- Repository에서 비즈니스 판단하지 않기
- MongoDB Index 정의를 코드 또는 Migration Script로 관리
- 도메인 이벤트 활용

### Frontend

- TypeScript strict 모드
- Composition API 사용
- API 응답 타입 명시
- 화면 컴포넌트와 비즈니스 로직 분리
- 권한 체크 Composable 제공
- Form Validation 공통화
- 에러 메시지 공통 처리

---

## 13. 성능 고려사항

- 게시판/결재함 목록은 반드시 인덱스 기반 조회
- 결재함 조회는 사용자별 역할과 상태 조건이 많으므로 별도 조회 모델을 고려
- 포탈은 웹파트별 Lazy Loading
- 첨부파일은 스트리밍 업로드/다운로드
- 대용량 조직도는 검색 우선 + 트리 Lazy Loading
- 알림은 비동기 처리
- 감사로그는 쓰기 부하를 고려한 별도 컬렉션 사용

---

## 14. 감사로그 설계

감사로그 대상:

- 로그인/로그아웃
- 인증 실패
- 권한 실패
- 게시글 생성/수정/삭제
- 첨부파일 다운로드
- 결재 문서 조회
- 결재 승인/반려/회수/전결
- 결재선 변경
- 관리자 설정 변경
- 사용자/조직 변경
- 근태 수정

감사로그 필드:

```json
{
  "tenantId": "companyA",
  "actorUserId": "u001",
  "actorName": "홍길동",
  "action": "APPROVAL_APPROVE",
  "resourceType": "ApprovalDocument",
  "resourceId": "doc001",
  "ipAddress": "127.0.0.1",
  "userAgent": "browser",
  "before": {},
  "after": {},
  "createdAt": "2026-04-28T10:00:00+09:00"
}
```

---

## 15. 화면 목록

### 포탈

- 메인 포탈
- 웹파트 설정
- 공지 팝업

### 게시판

- 게시판 목록
- 게시글 목록
- 게시글 상세
- 게시글 작성/수정
- 게시판 관리자

### 전자결재

- 결재 포탈
- 양식 선택
- 문서 작성
- 결재선 선택
- 문서 상세
- 기안함
- 결재대기함
- 결재진행함
- 반려함
- 완료함
- 참조함
- 예고함
- 부서수신함
- 부서발신함
- 양식 관리
- 결재 관리자

### 조직도

- 조직도 조회
- 사용자 검색
- 조직 관리
- 사용자 관리

### 근태

- 나의 근태
- 휴가 현황
- 휴가 신청
- 부서 근태
- 근태 관리자

### 일정/자원

- 캘린더
- 일정 등록
- 자원예약
- 예약 현황

### 관리자

- 메뉴 관리
- 권한 관리
- 코드 관리
- 시스템 설정
- 감사로그
- 배치 이력

---

## 16. AI에게 주는 최종 구현 지시

다음 원칙을 반드시 따른다.

1. 초기에 전체 시스템을 한 번에 완성하려 하지 말고, 도메인별로 수직 슬라이스 방식으로 구현한다.
2. 모든 기능은 Backend API, Frontend 화면, MongoDB 모델, 권한, 테스트를 함께 고려한다.
3. 전자결재는 이 시스템의 핵심 도메인이므로 가장 정교하게 구현한다.
4. 국내 그룹웨어에서 기대되는 기본 기능인 전자결재, 게시판, 조직도, 근태, 일정, 문서관리, 알림, 관리자 기능을 누락하지 않는다.
5. 모든 요청/응답은 공통 래퍼를 통과해야 한다.
6. Axios 직접 호출을 금지하고, 공통 API Client를 통해서만 호출한다.
7. 암복호화 옵션은 헤더 기반으로 처리하되, TLS를 대체하지 않는다.
8. OAuth2/JWT 기반 인증과 Backend 권한 검증을 반드시 구현한다.
9. MongoDB 스키마는 문서형 장점을 살리되, 결재/근태처럼 정합성이 중요한 도메인은 트랜잭션과 이벤트 로그를 사용한다.
10. 관리자 기능, 감사로그, 권한, 보안은 후순위 부가기능이 아니라 초기 설계에 포함한다.
11. 결과 코드는 실무 프로젝트에 바로 확장 가능한 수준으로 작성한다.
12. 예제 데이터와 Seed Script를 함께 제공한다.
13. README에는 실행 방법, 환경변수, Docker Compose, 테스트 방법을 포함한다.

---

## 17. 첫 번째 작업 요청

위 요구사항을 기준으로 다음 순서대로 작업을 시작하라.

1. 전체 아키텍처 요약 작성
2. Backend Solution 구조 생성
3. Frontend 프로젝트 구조 생성
4. 공통 API 응답 모델 작성
5. 공통 예외 처리 미들웨어 작성
6. OAuth2/JWT 인증 기반 코드 작성
7. MongoDB 연결 설정 작성
8. Axios 공통 래퍼 작성
9. 조직도/사용자/권한의 최소 모델 작성
10. 전자결재 도메인 모델과 상태 전이 설계 작성

각 단계마다 다음을 반드시 포함하라.

- 생성/수정할 파일 경로
- 코드 전문
- 설계 이유
- 다음 단계에서 연결되는 부분
- 테스트 방법

