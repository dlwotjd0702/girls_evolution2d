
## ⚠️ 중요 참고사항

1. **자동 참조**: `GameSystem`에서 자동으로 찾는 필드들은 비워두어도 되지만, 명시적으로 설정하는 것을 권장합니다.

2. **프리팹 필드**: 프리팹의 컴포넌트 필드들은 프리팹 자체에서 설정해야 합니다.

3. **⭐ 표시**: 새로 추가되거나 중요한 필드입니다.

4. **기본값**: 기본값이 있는 필드는 설정하지 않아도 되지만, 튜닝이 필요하면 변경하세요.

---

## 🔍 빠른 확인 방법

1. 각 스크립트의 인스펙터에서 빨간색으로 표시된 필드 확인
2. 콘솔에서 NullReferenceException 발생 시 해당 필드 확인
3. 기능이 동작하지 않을 때 관련 스크립트의 필드 확인

---

## ✅ GirlFieldManager 필드 정리

### Population / Gold Popup
- [ ] `populationText` (TMP) – `현재 인구수/최대 인구수` 표기
- [ ] `populationFormat` (string) – 기본 `{0}/{1}` 포맷, 필요 시 커스텀
- [ ] `goldPopupPool` (GoldGainPopupPool) – 골드 획득 팝업 풀
- [ ] `goldPopupOffset` (Vector2) – 팝업 위치 오프셋

### Discovery / Tier
- [ ] `tierManager`, `activeParent`, `hiddenParent`
- [ ] `discoverySpotlightPanel`, `discoverySpotlightImage`, `discoveryPresentationRoot`

---

## ✅ GoldGainPopupPool
- [ ] `popupPrefab` – TextMeshProUGUI + CanvasGroup 포함된 팝업 프리팹
- [ ] `preloadCount` – 초기 풀 사이즈 (기본 12)
- [ ] 풀 오브젝트는 Canvas 상단 계층에 배치

---


## ✅ InsufficientFundsPanel
- [ ] `panelRoot` – 부족 알림 패널
- [ ] `messageText` – "골드가 부족합니다" 또는 "보석이 부족합니다" 안내 문구
- [ ] `rewardText` – 광고 시 획득 골드 표시 (골드 부족 시에만 사용)
- [ ] `watchAdButton`, `watchAdIcon`, `adReadySprite`, `adNotReadySprite`
- [ ] `adServiceBehaviour` – `IAdOfferService` 구현체(예: `RewardedAdsManager_AdMob`). 미지정 시 광고 버튼이 숨겨짐/비활성화됨.
- [ ] "아니오" 버튼은 패널에서 직접 `SetActive(false)`를 호출하도록 UX 설계 (별도 필드 없음)
- [ ] **사용처**: `SummonPanelController`, `ShopPanelController` (보석 부족), `GemStorePanelController` (IAP 구매 실패), `AutoAutomationController` (골드 부족)

---

## ✅ SummonPanelController (보석 부족 처리)
- [ ] `insufficientFundsPanel` (InsufficientFundsPanel) – 보석 부족 시 안내 패널
- [ ] 기타 필드는 기존과 동일

---

## ✅ ShopPanelController (보석 부족 처리)
- [ ] `insufficientFundsPanel` (InsufficientFundsPanel) – 보석 부족 시 안내 패널
- [ ] `premiumCurrency` (PremiumCurrencyManager, 자동 찾기 가능) – 보석 보유량 확인용
- [ ] 기타 필드는 기존과 동일

---

## ✅ AutoAutomationController (보석 구매 지원)
- [ ] `economy` (EconomyManager)
- [ ] `premiumCurrency` (PremiumCurrencyManager, 자동 찾기 가능) – 보석 구매용
- [ ] `insufficientPanel` (InsufficientFundsPanel) – 골드/보석 부족 시 안내 패널
- [ ] 골드 단위: "G" 자동 표시
- [ ] 보석 단위: "gems" 자동 표시
- [ ] 보석 구매 시 PremiumCurrencyManager 직접 연결하여 처리

---

## ✅ PrestigeConfirmPanel
- [ ] `panelRoot`
- [ ] `titleText`, `messageText`
- [ ] `baseGainText`, `multiplierText`, `totalGainText`
- [ ] `confirmButton`, `cancelButton`
- [ ] `PrestigeManager` 인스펙터에서 `prestigeConfirmPanel` 필드 연결


---

## ✅ OfflineRewardPanel
- [ ] `panelRoot`
- [ ] `titleText`, `durationText`, `capNoteText`
- [ ] `rewardText`, `multiplierText`, `perSecText`
- [ ] `claimButton`, `claimAdButton`, `adButtonLabel`
- [ ] `economy` (EconomyManager)
- [ ] `adServiceBehaviour` (IAdOfferService 구현체, 선택)
- [ ] `adRewardMultiplier` (기본 2배)
- [ ] `GirlFieldManager.offlineRewardPanel` 필드에 이 컴포넌트 연결

---

## ✅ GemStorePanelController
- [ ] `premium` (PremiumCurrencyManager, 자동 찾기 가능)
- [ ] `entries` (List<Entry>) – IAP 상품 목록 + 광고 Entry (isAdEntry=true)
- [ ] `insufficientPanel` (InsufficientFundsPanel) – 구매 실패 시 안내 패널
- [ ] `adServiceBehaviour` (IAdOfferService 구현체) – 광고 서비스
- [ ] `adReadySprite` – 광고 준비됨 스프라이트
- [ ] `adNotReadySprite` – 광고 준비 중 스프라이트
- [ ] `adGemReward` (기본 5) – 광고 시청 시 지급할 젬 수
- [ ] 광고 Entry 설정:
  - `isAdEntry = true`
  - `buttonIcon` (Image) – 광고 버튼 아이콘
  - `productId`는 사용하지 않음 (광고 Entry는 IAP가 아님)

---

## 📝 추가 TODO (에디터 작업)
- [ ] **OfflineRewardPanel UI 구축** – UI 배치, TMP 연결, `GirlFieldManager.offlineRewardPanel` 필드 지정
- [ ] **InsufficientFundsPanel 연결 검증** – 광고 버튼/메시지 텍스트 연결 및 Shop·Summon 호출 테스트
- [ ] **PrestigeConfirmPanel 배치** – 패널 UI 배치 및 `PrestigeManager.prestigeConfirmPanel` 필드 연결
- [ ] **Exit/백버튼 UX 점검** – `ExitPanelToggler` UI 구성 후 저장·종료 플로우 확인


