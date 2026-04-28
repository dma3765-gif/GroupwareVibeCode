// MongoDB 초기화 스크립트 - 테스트용 기본 데이터 생성
db = db.getSiblingDB('groupware');

// 시스템 코드 초기화
db.systemCodes.insertMany([
  { code: 'DEPT_TYPE', value: 'COMPANY', label: '회사', sortOrder: 1, isActive: true },
  { code: 'DEPT_TYPE', value: 'DIVISION', label: '사업부', sortOrder: 2, isActive: true },
  { code: 'DEPT_TYPE', value: 'TEAM', label: '팀', sortOrder: 3, isActive: true },
  { code: 'POSITION', value: 'CEO', label: '대표이사', sortOrder: 1, isActive: true },
  { code: 'POSITION', value: 'DIRECTOR', label: '이사', sortOrder: 2, isActive: true },
  { code: 'POSITION', value: 'GENERAL_MANAGER', label: '부장', sortOrder: 3, isActive: true },
  { code: 'POSITION', value: 'MANAGER', label: '차장', sortOrder: 4, isActive: true },
  { code: 'POSITION', value: 'DEPUTY_MANAGER', label: '과장', sortOrder: 5, isActive: true },
  { code: 'POSITION', value: 'SENIOR_STAFF', label: '대리', sortOrder: 6, isActive: true },
  { code: 'POSITION', value: 'STAFF', label: '사원', sortOrder: 7, isActive: true },
]);

print('MongoDB 초기화 완료');
