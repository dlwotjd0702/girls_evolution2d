# 패치 노트

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

