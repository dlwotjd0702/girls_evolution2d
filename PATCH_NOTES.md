# 패치 노트

## [완료] Google Play Games 클라우드 저장 및 업적 시스템 (2024년)

### 추가된 기능
- **클라우드 저장 시스템**
  - Google Play Games SDK 연동
  - 로그인/로그아웃 기능
  - 클라우드 저장/로드 기능
  - 기기 간 동기화
  - 로컬 저장과 클라우드 저장 병행
  - 충돌 해결 로직
  - 새로 시작하기 기능 (로컬 + 클라우드 저장 모두 삭제)

- **업적 시스템**
  - 레벨 25 달성
  - 클릭 1,000회
  - 1시간 플레이
  - 강화 10레벨
  - 1계층 발견
  - 자동 업적 체크 및 달성 처리 (5초마다)
  - 업적 UI 표시 기능

- **수동 저장 버튼 개선**
  - 로그인 체크 및 자동 로그인 시도
  - 로컬 저장 우선, 클라우드 저장 병행
  - 오프라인 환경 안전 처리

- **새로 시작하기 기능**
  - DOTween 애니메이션 정리 (에러 방지)
  - 로컬 및 클라우드 저장 모두 삭제
  - 안전한 씬 리로드

- **튜토리얼 시스템**
  - 단계별 가이드 UI 패널
  - 소환 방법 안내
  - 합성 방법 안내 (드래그 앤 드롭)
  - 상점 사용법 안내
  - 티어 이동 방법 안내
  - 자동소환/자동합성 활성화 방법 안내
  - 튜토리얼 진행 상태 저장
  - 스킵 기능

### 구현된 파일
- `Assets/Scripts/Save_Datas/CloudSaveManager.cs` (신규)
- `Assets/Scripts/AchievementManager.cs` (신규, 업적 시스템)
- `Assets/Scripts/Save_Datas/SaveManager.cs` (클라우드 저장 통합, 로컬 우선 로드, 클라우드 삭제 기능)
- `Assets/Scripts/UI/SettingsPanel.cs` (수동 저장 버튼 개선)
- `Assets/Scripts/UI/NewGameConfirmPanel.cs` (새로 시작하기 기능 개선)
- `Assets/Scripts/UI/TutorialManager.cs` (신규, 튜토리얼 시스템)
- `Assets/Scripts/Save_Datas/SaveData.cs` (tutorialCompleted 필드 추가)

---

## [완료] 플레이 디버그 정보 시스템 (2024년)

### 추가된 기능
- **플레이 통계 수집**
  - 총 플레이타임 추적
  - 클릭/합성/소환 횟수 추적
  - 골드 획득/소비 추적
  - 레벨 25 달성 횟수 및 시간 기록

### 구현된 파일
- `Assets/Scripts/PlayStatsTracker.cs` (신규)
- `Assets/Scripts/Save_Datas/SaveData.cs` (통계 필드 추가)

---

## [완료] 쿠폰 시스템 구현 (2024년)

### 추가된 기능
- **쿠폰 입력 및 사용 시스템**
  - 쿠폰 코드 입력 패널 (`CouponPanelController`)
  - 쿠폰 관리 시스템 (`CouponManager`)
  - 인스펙터에서 쿠폰 코드와 보상 설정 가능
  - 골드 또는 보석 보상 선택 (체크박스)
  - 보상 양 입력 (`rewardAmount`)

### 주요 기능
- 쿠폰 코드 검증 (대소문자 구분 없음)
- 중복 사용 방지 (사용한 쿠폰 목록 저장)
- 오프라인 보상 패널에 보상 표시
- 광고 보고 2배 받기 지원 (골드/보석 모두)
- 성공/실패 메시지 표시 (reason label)

### 구현된 파일
- `Assets/Scripts/UI/CouponManager.cs` (신규)
- `Assets/Scripts/UI/CouponPanelController.cs` (신규)
- `Assets/Scripts/Save_Datas/SaveData.cs` (usedCoupons 필드 추가)
- `Assets/Scripts/UI/OfflineRewardPanel.cs` (ShowCouponReward 메서드 추가)

---

## [완료] 광고 제거 가격 표시 수정 (2024년)

### 수정 내용
- **문제**: 광고 제거 상품의 가격이 price text에 반영되지 않음
- **해결**: 
  - `PremiumCurrencyManager.GetLocalizedPrice()` 개선
  - 가격이 로드되지 않았을 때 "가격 로딩 중..." 메시지 표시
  - `OnProductsFetched`에서 가격 캐싱 개선

### 수정된 파일
- `Assets/Scripts/makesomemoney/PremiumCurrencyManager.cs`
- `Assets/Scripts/makesomemoney/GemStorePanelController.cs`

---

