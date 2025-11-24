# Unity 에디터 설정 체크리스트

이 문서는 프로젝트의 모든 스크립트에서 에디터에서 설정해야 하는 필드들을 정리한 체크리스트입니다.

---

## 📋 1. SummonPanelController

**위치**: 소환 패널 GameObject

### Refs (자동 참조 가능하지만 수동 설정 권장)
- [ ] `dataManager` - GirlDataManager (GameSystem에서 자동 찾음)
- [ ] `fieldManager` - GirlFieldManager (GameSystem에서 자동 찾음)
- [ ] `spriteLoader` - GirlSpriteAddressableLoader (GameSystem에서 자동 찾음)
- [ ] `economy` - EconomyManager (GameSystem에서 자동 찾음)
- [ ] `premiumCurrency` - PremiumCurrencyManager (자동 찾음)

### Gem Summon Settings
- [ ] `gemSummonIncrement` - 보석 소환 비용 증가량 (기본값: 1)

### UI
- [ ] `content` - ScrollView의 Content (RectTransform)
- [ ] `cellPrefab` - SummonCell 프리팹 (GameObject)
- [ ] `reasonLabel` - 오류 메시지 표시용 TextMeshProUGUI

### Refresh
- [ ] `interactableRefreshInterval` - 버튼 갱신 간격 (기본값: 0.25f)

### Reason Popup
- [ ] `reasonShowSeconds` - 오류 메시지 표시 시간 (기본값: 1.15f)

---

## 📋 2. SummonCell 프리팹

**위치**: 프리팹에 SummonCell 컴포넌트 추가

### UI Components
- [ ] `iconImage` - 캐릭터 아이콘 (Image)
- [ ] `nameLabel` - 이름 표시 (TextMeshProUGUI)
- [ ] `costLabel` - 골드 비용 표시 (TextMeshProUGUI)
- [ ] `summonButton` - 골드 소환 버튼 (Button)
- [ ] `gemSummonButton` - 보석 소환 버튼 (Button) ⭐ 새로 추가
- [ ] `gemCostLabel` - 보석 비용 표시 (TextMeshProUGUI, 선택사항) ⭐ 새로 추가

---

## 📋 3. EncyclopediaPanelController

**위치**: 도감 패널 GameObject

### References (자동 참조 가능)
- [ ] `dataManager` - GirlDataManager (GameSystem에서 자동 찾음)
- [ ] `fieldManager` - GirlFieldManager (GameSystem에서 자동 찾음)
- [ ] `spriteLoader` - GirlSpriteAddressableLoader (GameSystem에서 자동 찾음)

### UI
- [ ] `slotContainer` - GridLayoutGroup이 있는 부모 Transform
- [ ] `slotPrefab` - 도감 슬롯 프리팹 (GameObject)
- [ ] `detailPanel` - 상세 팝업 패널의 EncyclopediaDetailPanel 컴포넌트

---

## 📋 4. EncyclopediaSlot 프리팹

**위치**: 프리팹에 EncyclopediaSlot 컴포넌트 추가

### UI Components
- [ ] `iconImage` - 캐릭터 아이콘 (Image)
- [ ] `backgroundImage` - 배경 이미지 (Image, 선택사항)
- [ ] `levelText` - 레벨 표시 (TextMeshProUGUI)
- [ ] `lockedOverlay` - 잠금 오버레이 GameObject (잠금 이미지 포함)
- [ ] `button` - 클릭 버튼 (Button)

---

## 📋 5. EncyclopediaDetailPanel

**위치**: 도감 상세 팝업 패널 GameObject

### UI Components
- [ ] `panelRoot` - 전체 패널 루트 GameObject
- [ ] `ldIllustrationImage` - 상단 큰 LD 일러스트 (Image, Preserve Aspect 체크)
- [ ] `nameText` - 이름 표시 (TextMeshProUGUI)
- [ ] `levelText` - 레벨 표시 (TextMeshProUGUI)
- [ ] `incomeText` - 수익 표시 (TextMeshProUGUI)
- [ ] `closeButton` - 닫기 버튼 (Button)

### Settings
- [ ] `incomeFormat` - 수익 포맷 문자열 (기본값: "수익: {0:N0} G/s")

**추가 설정**:
- [ ] `panelRoot`에 Button 컴포넌트 추가 (배경 클릭 시 닫기용)

---

## 📋 6. GirlFieldManager

**위치**: 필드 관리 GameObject

### Tier
- [ ] `tierManager` - TierManager 참조
- [ ] `activeParent` - 활성 티어 부모 Transform
- [ ] `hiddenParent` - 숨김 티어 부모 Transform

### Visibility
- [ ] `reparentForVisibility` - 가시성 전환 시 부모 변경 여부 (bool)

### Idle Income
- [ ] `idleTickSeconds` - Idle 수익 지급 간격 (기본값: 1.0f)
- [ ] `emaTimeConstant` - 수익 추정치 부드러움 (기본값: 1.5f)

### Discovery FX
- [ ] `ldCenterScale` - LD 중심 스케일 (기본값: 2.0f)
- [ ] `ldCenterScaleFinalMul` - LD 중심 최종 스케일 배수 (기본값: 2.0f)
- [ ] `ldPopOvershoot` - 팝업 오버슈트 (기본값: 0.12f)
- [ ] `ldPopInTime` - 팝업 인 시간 (기본값: 0.12f)
- [ ] `ldHoldTime` - 홀드 시간 (기본값: 0.15f)
- [ ] `moveDuration` - 이동 시간 (기본값: 0.55f)
- [ ] `swapToSDFraction` - SD 스왑 타이밍 (기본값: 0.18f)
- [ ] `discoverySpotlightPanel` - 스포트라이트 패널 GameObject ⭐
- [ ] `discoverySpotlightImage` - 스포트라이트 Image (자동 찾음 또는 수동 설정) ⭐
- [ ] `spotlightFadeIn` - 스포트라이트 페이드 인 시간 (기본값: 0.18f)
- [ ] `spotlightFadeOut` - 스포트라이트 페이드 아웃 시간 (기본값: 0.15f)
- [ ] `spotlightMaxAlpha` - 스포트라이트 최대 알파 (기본값: 0.9f)
- [ ] `discoveryPresentationRoot` - 발견 시 LD 캐릭터 중심 배치용 RectTransform ⭐

### Spawn Button UI
- [ ] `spawnButton` - 소환 버튼 (Button)
- [ ] `chargeFillImage` - 차지 게이지 (Image, Type = Filled)
- [ ] `chargeCountText` - 차지 개수 텍스트 "cur/max" (TextMeshProUGUI)

---

## 📋 7. EconomyManager

**위치**: 경제 관리 GameObject

### Gold UI (TMP)
- [ ] `goldText` - 현재 골드 표시 (TextMeshProUGUI)
- [ ] `goldPerSecText` - 초당 골드 표시 (TextMeshProUGUI)
- [ ] `goldUnitSuffix` - 골드 단위 접미사 (기본값: "G")
- [ ] `showPlusOnPerSec` - 초당 골드에 + 표시 여부 (기본값: true)
- [ ] `hidePerSecWhenZero` - 0일 때 초당 골드 숨김 여부 (기본값: true)

### Spawn/Field Base
- [ ] `baseManualSpawnMax` - 기본 수동 소환 최대 (기본값: 3)
- [ ] `baseManualSpawnInterval` - 기본 수동 소환 간격 (기본값: 10f)
- [ ] `baseFieldCount` - 기본 필드 개수 (기본값: 8)

### Automation (intervals)
- [ ] `autoMergeBaseInterval` - 자동 합성 기본 간격 (기본값: 5f)
- [ ] `autoSpawnBaseInterval` - 자동 소환 기본 간격 (기본값: 6f)
- [ ] `autoPerLevelMul` - 레벨당 배수 (기본값: 0.9f)
- [ ] `autoIntervalFloor` - 최소 간격 (기본값: 0.4f)

### Level Caps
- [ ] `spawnMaxUpgradeCap` - 소환 최대 업그레이드 캡 (기본값: 10)
- [ ] `spawnSpeedUpgradeCap` - 소환 속도 업그레이드 캡 (기본값: 10)
- [ ] `fieldMaxUpgradeCap` - 필드 최대 업그레이드 캡 (기본값: 10)
- [ ] `clickBonusUpgradeCap` - 클릭 보너스 업그레이드 캡 (기본값: 15)
- [ ] `offlineRewardCap` - 오프라인 보상 캡 (기본값: 10)
- [ ] `offlineMaxTimeCap` - 오프라인 최대 시간 캡 (기본값: 10)
- [ ] `autoMergeCap` - 자동 합성 캡 (기본값: 10)
- [ ] `autoSpawnCap` - 자동 소환 캡 (기본값: 10)

---

## 📋 8. GameSystem

**위치**: 메인 시스템 GameObject (DefaultExecutionOrder: -100)

### Data
- [ ] `spriteLoader` - GirlSpriteAddressableLoader
- [ ] `girlDataManager` - GirlDataManager (new로 생성)

### GameLoop
- [ ] `fieldManager` - GirlFieldManager
- [ ] `mergeManager` - GirlMergeManager
- [ ] `prestigeManager` - PrestigeManager

### Economy
- [ ] `economy` - EconomyManager

### UI
- [ ] `summonPanel` - SummonPanelController (선택사항)

---

## 📋 9. 프리팹 제작 체크리스트

### SummonCell 프리팹
- [ ] GameObject 생성
- [ ] SummonCell 컴포넌트 추가
- [ ] IconImage (Image) 추가
- [ ] NameLabel (TextMeshProUGUI) 추가
- [ ] CostLabel (TextMeshProUGUI) 추가
- [ ] SummonButton (Button) 추가
- [ ] GemSummonButton (Button) 추가 ⭐
- [ ] GemCostLabel (TextMeshProUGUI) 추가 (선택사항) ⭐
- [ ] 모든 필드 인스펙터에서 연결

### EncyclopediaSlot 프리팹
- [ ] GameObject 생성
- [ ] EncyclopediaSlot 컴포넌트 추가
- [ ] IconImage (Image) 추가
- [ ] BackgroundImage (Image, 선택사항) 추가
- [ ] LevelText (TextMeshProUGUI) 추가
- [ ] LockedOverlay (GameObject) 추가 (잠금 이미지 포함)
- [ ] Button (Button) 추가
- [ ] 모든 필드 인스펙터에서 연결

### EncyclopediaDetailPanel 프리팹
- [ ] PanelRoot (GameObject) 생성 (전체 화면 덮도록 Anchor: Stretch)
- [ ] EncyclopediaDetailPanel 컴포넌트 추가
- [ ] LDIllustrationImage (Image) 추가 (Preserve Aspect 체크, 상단 중앙 배치)
- [ ] NameText (TextMeshProUGUI) 추가
- [ ] LevelText (TextMeshProUGUI) 추가
- [ ] IncomeText (TextMeshProUGUI) 추가
- [ ] CloseButton (Button) 추가
- [ ] PanelRoot에 Button 컴포넌트 추가 (배경 클릭용)
- [ ] 모든 필드 인스펙터에서 연결

---

## 📋 10. UI 레이아웃 체크리스트

### 소환 패널
- [ ] ScrollView 생성
- [ ] Content (RectTransform) 생성
- [ ] SummonCell 프리팹 준비
- [ ] ReasonLabel (TextMeshProUGUI) 생성 및 배치
- [ ] SummonPanelController 컴포넌트 추가 및 필드 연결

### 도감 패널
- [ ] 도감 패널 GameObject 생성
- [ ] SlotContainer (Transform) 생성 (GridLayoutGroup 추가)
- [ ] EncyclopediaSlot 프리팹 준비
- [ ] EncyclopediaDetailPanel 준비
- [ ] EncyclopediaPanelController 컴포넌트 추가 및 필드 연결

---

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

