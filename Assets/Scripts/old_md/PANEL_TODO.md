# 패널 시스템 TODO 리스트

## ✅ 완료된 작업

### 1. 골드 부족 패널 연동
- [x] `SummonPanelController` - 골드 소환 실패 시 `InsufficientFundsPanel.ShowForGoldShortage()` 호출 연결
- [x] `ShopPanelController` - 골드 업그레이드 실패 시 `InsufficientFundsPanel.ShowForGoldShortage()` 호출 연결

### 2. 환생 확인 패널
- [x] `PrestigeConfirmPanel` 스크립트 제작
- [x] 환생 포인트 가중 요소 표시 기능 구현
- [x] `PrestigeManager` - `OnClickPrestigeButton` 수정하여 확인 패널 표시 후 `DoPrestige` 호출
- [x] 환생 포인트 계산 로직 연동 (`PreviewPrestigeGain()` 사용)

---

## 📋 남은 작업

### 1. 종료 확인 패널 (ExitPanelToggler)
- [ ] 종료 확인 패널 UI 제작
  - 패널 루트 GameObject
  - 제목 텍스트 ("게임을 종료하시겠습니까?")
  - 메시지 텍스트 (선택사항)
  - "예" 버튼 (ExitPanelToggler.ConfirmExit 연결)
  - "아니오" 버튼 (ExitPanelToggler.Cancel 연결)
  - 배경 클릭으로 닫기 (Button 컴포넌트 추가)
- [ ] `ExitPanelToggler` 인스펙터에서 `panelRoot` 연결 확인
- [ ] 뒤로가기 키 입력 감지 테스트 (모바일 환경)

**기존 구조**: `ExitPanelToggler.cs` 이미 존재
- `Show()`, `Hide()`, `ConfirmExit()`, `Cancel()` 메서드 제공
- 뒤로가기 키(ESC) 자동 감지

---

### 2. 골드 부족 패널 UI
- [ ] `InsufficientFundsPanel` UI 제작
  - 패널 루트 GameObject
  - 메시지 텍스트 (`messageText`) - "골드가 부족합니다" 등 간단 안내
  - 광고 보상 텍스트 (`rewardText`) - 예상 지급량
  - 광고 시청 버튼 (`watchAdButton`)
  - 광고 아이콘 (`watchAdIcon`)
- [ ] 광고 서비스 연결 (`adServiceBehaviour` 필드에 `IAdOfferService` 구현체 할당)
- [ ] 광고 준비 상태 스프라이트 설정 (`adReadySprite`, `adNotReadySprite`)
- [ ] "아니오" 버튼은 패널에서 직접 `SetActive(false)` 호출로 처리

**기존 구조**: `InsufficientFundsPanel.cs` 이미 존재
- `ShowForGoldShortage(need, have)` - 골드 부족 시 호출
- `ShowGeneric(title, msg)` - 범용 메시지
- 광고 시청 시 5분치 골드 지급

---

### 3. 환생 확인 패널 UI
- [ ] `PrestigeConfirmPanel` UI 제작
  - 패널 루트 GameObject
  - 제목 텍스트 ("환생하시겠습니까?")
  - 메시지 텍스트 (선택사항)
  - 환생 포인트 텍스트 (`prestigePointText`) - "획득 예상: X 환생석"
  - 가중 요소 목록 컨테이너 (`breakdownContainer`) - VerticalLayoutGroup 권장
  - 가중 요소 아이템 프리팹 (`breakdownItemPrefab`) - TextMeshProUGUI 포함
  - "예" 버튼 (`confirmButton`)
  - "아니오" 버튼 (`cancelButton`)
  - 배경 클릭으로 닫기 (Button 컴포넌트 추가)
- [ ] `PrestigeManager` 인스펙터에서 `confirmPanel` 필드 연결
- [ ] `PrestigeConfirmPanel` 인스펙터에서 `prestigeManager`, `fieldManager` 연결 (자동 찾기 가능)

**기존 구조**: `PrestigeConfirmPanel.cs` 새로 제작
- `Show(onConfirmCallback)` - 확인 패널 표시
- `Hide()` - 패널 숨기기
- 환생 포인트 계산 및 가중 요소 표시 자동 처리

---

## 🔗 연결된 기존 구조

### ExitPanelToggler
- 위치: `Assets/Scripts/ExitPanelToggler.cs`
- 기능: 뒤로가기 키 감지, 패널 표시/숨김, 종료 확인
- 연결 필요: UI 제작 후 `panelRoot` 필드 연결

### InsufficientFundsPanel
- 위치: `Assets/Scripts/makesomemoney/InsufficientFundsPanel.cs`
- 기능: 골드/보석 부족 패널, 광고 시청 기능
- 연결 완료:
  - `SummonPanelController` - 골드 소환 실패 시 호출
  - `ShopPanelController` - 골드 업그레이드 실패 시 호출
- 연결 필요: UI 제작 및 광고 서비스 연결

### PrestigeManager
- 위치: `Assets/Scripts/Mainsystem/PrestigeManager.cs`
- 기능: 환생 시스템 관리
- 연결 완료:
  - `PrestigeConfirmPanel` - 확인 패널 표시 후 환생 실행
- 연결 필요: `confirmPanel` 필드에 `PrestigeConfirmPanel` 할당

---

## 📝 에디터 설정 체크리스트

### ExitPanelToggler
- [ ] `panelRoot` - 종료 확인 패널 GameObject 연결
- [ ] `toggleWithBackKey` - 뒤로가기 키 활성화 (기본값: true)
- [ ] `closeIfOpenOnBack` - 열려있을 때 뒤로가기로 닫기 (기본값: true)

### InsufficientFundsPanel
- [ ] `panelRoot` - 패널 루트 GameObject
- [ ] `economy` - EconomyManager (자동 찾기 가능)
- [ ] `adServiceBehaviour` - IAdOfferService 구현체 (광고 서비스)
- [ ] `messageText` - 메시지 텍스트
- [ ] `rewardText` - 광고 보상 텍스트
- [ ] `watchAdButton` - 광고 시청 버튼
- [ ] `watchAdIcon` - 광고 아이콘 Image
- [ ] `adReadySprite` - 광고 준비 스프라이트
- [ ] `adNotReadySprite` - 광고 미준비 스프라이트

### SummonPanelController
- [ ] `insufficientFundsPanel` - InsufficientFundsPanel 컴포넌트 연결

### ShopPanelController
- [ ] `insufficientFundsPanel` - InsufficientFundsPanel 컴포넌트 연결

### PrestigeManager
- [ ] `confirmPanel` - PrestigeConfirmPanel 컴포넌트 연결

### PrestigeConfirmPanel
- [ ] `panelRoot` - 패널 루트 GameObject
- [ ] `titleText` - 제목 텍스트
- [ ] `messageText` - 메시지 텍스트 (선택사항)
- [ ] `confirmButton` - 확인 버튼
- [ ] `cancelButton` - 취소 버튼
- [ ] `prestigePointText` - 환생 포인트 텍스트
- [ ] `breakdownContainer` - 가중 요소 목록 컨테이너 (VerticalLayoutGroup 권장)
- [ ] `breakdownItemPrefab` - 가중 요소 아이템 프리팹
- [ ] `prestigeManager` - PrestigeManager (자동 찾기 가능)
- [ ] `fieldManager` - GirlFieldManager (자동 찾기 가능)

---

## 🎯 구현 우선순위

1. **ExitPanelToggler UI 제작** (가장 간단)
2. **InsufficientFundsPanel UI 제작** (골드 부족 시 즉시 필요)
3. **PrestigeConfirmPanel UI 제작** (환생 기능 완성)


