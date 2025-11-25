# Girls Evolution 2D – Technical Highlights

게임 업계 포트폴리오 제출용 기술 문서 요약입니다. 프로젝트의 핵심 기능과 적용한 주요 기법을 모듈 단위로 정리했습니다.

---

## 1. Core Game Loop & Initialization

### 게임 초기화 플로우
- **GameSystem** (`DefaultExecutionOrder(-100)`): 싱글톤 패턴으로 모든 매니저 간 의존성 주입
  - `Awake()`: 필드 매니저, 머지 매니저, 프레스티지 매니저 간 참조 자동 연결
  - `Start()`: 비동기 데이터 로딩 (CSV → 스프라이트) → `AssetsReady` 이벤트 브로드캐스트
  - 로딩 패널 관리: 세이브 파일 없으면 스프라이트 로딩 완료 시 숨김, 있으면 데이터 적용 완료 후 숨김

### 핵심 게임 루프
- **GirlFieldManager**: 캐릭터 스폰, 합성, 자동 수익 계산 등을 관리하는 핵심 시스템
  - Idle 골드 계산 시 계승/환생 배수를 동적으로 적용 (리플렉션 기반 안전 호출)
  - `JumpBounceLoop`로 자동 점프·바운스를 수행하며, 클릭/합성 시마다 즉시 수익 반영
  - 인구수 UI 동적 갱신 (`UpdatePopulationUI()`)
  - 오프라인 보상 패널 자동 표시 (`TryShowOfflineReward()`)
- **GirlMergeManager**: 드래그/자동 합성 로직과 +2단 도약 확률 적용
  - 클릭 수익 시 골드 팝업 표시 (`ShowGoldPopup()`)
- **PrestigeManager**: 환생/환생 포인트 및 환생 상점(12종 영구 업그레이드) 관리
  - 환생 확인 패널 연동 (`PrestigeConfirmPanel`)

---

## 2. Data & Asset Pipeline

### CSV/TSV 자동 로딩
- **GirlDataManager.LoadAsync()**: Addressables TextAsset을 비동기로 읽고, 컬럼명 자동 매칭으로 TSV/CSV를 모두 지원
  - `initialDirection` 필드 지원 (1: 왼쪽, -1: 오른쪽)
- **Addressables 기반 스프라이트 로딩**: `GirlSpriteAddressableLoader`가 SD/LD 스프라이트 라벨을 비동기로 프리로드하여 메모리 효율 확보
- **StreamingAssets/girl.csv**: 25레벨 캐릭터 메타 데이터를 외부 파일로 분리해 손쉽게 밸런스 조정 가능

---

## 3. Save System & Persistence

### SaveManager 아키텍처
- **로컬 파일 저장**: `Application.persistentDataPath`에 JSON 기반 로컬 파일 저장
  - 백업 파일 자동 생성 (`save_backup.json`)
  - PlayerPrefs 병행 저장 (호환성 유지)
- **자동 저장**: 1분 간격 자동 저장, `OnApplicationPause/Quit` 시 즉시 저장
- **ISaveable 인터페이스**: `ApplyLoadedData/CollectSaveData`로 모듈별 상태를 독립적으로 직렬화
  - 구현체: `EconomyManager`, `GirlFieldManager`, `PrestigeManager`, `PremiumCurrencyManager`, `TierManager`, `LegacyRankManager`
- **오프라인 시간 추적**: `CaptureOfflineDuration()`으로 마지막 저장 시간과 현재 시간 차이 계산
  - `TryConsumeOfflineSeconds()`: 오프라인 시간을 한 번만 소비하도록 보장

---

## 4. Economy & Currency Systems

### EconomyManager
- **골드 관리**: `double` 기반 고정밀도 계산, 이벤트 기반 UI 갱신 (`OnGoldChanged`, `OnUpgradeChanged`)
- **업그레이드 시스템**: 수동 소환 최대/쿨타임, 필드 최대, 클릭 보너스, 오프라인 보상 등
- **자동화 시스템**: 자동 합성/소환 업그레이드 (레벨당 간격 감소)
- **소환 비용 가중**: 레벨별 소환 횟수 추적으로 지수적 비용 증가

### PremiumCurrencyManager (IAP v5)
- **Unity IAP Services 통합**: `StoreController` 기반 상품 구매, 복원, 가격 캐싱
- **보석 관리**: `long` 타입으로 보석 잔액 관리, `TrySpendGems()` 실패 시 이벤트 발생
- **광고 제거 상품**: Non-consumable 상품으로 광고 영구 제거 (`AdsRemoved` 플래그)
- **구매 실패 이벤트**: `OnPurchaseFailed` 이벤트로 UI에 실패 메시지 전달

---

## 5. UI & UX Systems

### 도감(Encyclopedia) 시스템
- **EncyclopediaPanelController**: 발견 여부(`discoveredLevels`) 기반 UI 락 처리
- **EncyclopediaSlot**: 슬롯별 잠금 오버레이 (`lockedOverlay.SetActive()`)
- **EncyclopediaDetailPanel**: LD 일러스트 팝업, 이름/레벨/수익 표시
- **클로저 캡처 문제 해결**: `for` 루프 변수를 로컬 변수로 복사하여 람다 안전성 확보

### LD Discovery 연출
- **PlayDiscoveryOnce**: LD 일러스트를 중앙 프리젠테이션 Root로 이동
- **Spotlight 시스템**: `discoverySpotlightImage`의 알파를 DOTween으로 제어하여 페이드 인/아웃
- **SD 교체 시점**: SD로 스왑될 때 빠른 페이드아웃(`FadeOutSpotlight()`)으로 연출 마무리

### 골드 획득 팝업 (Object Pooling)
- **GoldGainPopupPool**: `SimpleUIPool` 기반 효율적인 UI 객체 재사용
- **GoldGainPopup**: DOTween 기반 플로팅 애니메이션 (위로 이동 + 페이드 아웃)
- **자동/클릭 구분**: 색상으로 구분 (자동: 흰색, 클릭: 노란색)

### 부족 자금 패널 (InsufficientFundsPanel)
- **통합 패널**: 골드 부족, 보석 부족, IAP 구매 실패 등 모든 부족 상황 처리
- **광고 보상 연동**: `IAdOfferService` 인터페이스로 광고 시청 후 골드 보상
- **사용처**: `SummonPanelController`, `ShopPanelController`, `AutoAutomationController`, `GemStorePanelController`

### 확인 패널 시스템
- **ExitConfirmPanel**: 모바일 백버튼(`KeyCode.Escape`) 입력 시 종료 확인
- **PrestigeConfirmPanel**: 환생 포인트 계산 요소 표시 (기본 획득량, 배수, 총 획득량)

### 오프라인 보상 패널
- **OfflineRewardPanel**: 재접속 시 오프라인 수익 계산 및 표시
  - 최대 시간 제한 적용 (`GetOfflineMaxSeconds()`)
  - 보상 배수 적용 (`GetOfflineRewardMultiplier()`)
  - 광고 시청 시 2배 보상 옵션

---

## 6. Animation & Feel

### GirlCharacter 개선
- **방향 전환**: `initialDirection` 필드 기반 초기 방향 설정, 이동 시 스프라이트 Flip
- **Idle Groove**: 미세 스케일 변화로 생동감 강화
- **점프/바운스**: 자연스러운 이징 적용 (`Ease.OutQuad`, `Ease.InOutSine`)
- **Pulse/Bounce**: 드리프트 방지, Final 모드 시 화면 중앙 고정 연출

### DOTween 전면 활용
- **Jump, Bounce, Spotlight 페이드, 발견 팝업** 등 모든 애니메이션을 DOTween으로 구성
- **Sequence 기반**: OutBack, OutCubic, InOutSine 등 다양한 Ease 조합으로 생동감 강화
- **메모리 효율**: `OnDisable`에서 트윈 자동 정리

---

## 7. Modular Architecture & Design Patterns

### 싱글톤 패턴
- `GameSystem`, `SaveManager`, `PremiumCurrencyManager`, `SimpleUIPool`, `GoldGainPopupPool`

### 인터페이스 기반 확장성
- **ISaveable**: 저장/로드 시스템에 신규 모듈 쉽게 편입
- **IAdOfferService**: 광고 서비스 추상화 (AdMob 등 다양한 구현체 지원)

### 리플렉션 기반 안전 호출
- 외부 매니저(`LegacyRankManager`, `PrestigeShopManager`) 호출 시 컴파일 타임 의존성 최소화
- `FindTypeByName()`, `GetMethod()`, `Invoke()` 패턴으로 선택적 기능 활성화

### 이벤트 기반 통신
- `OnGoldChanged`, `OnUpgradeChanged`, `OnGemsChanged`, `OnPurchaseFailed` 등
- UI와 게임 로직 간 느슨한 결합 유지

### Object Pooling
- **SimpleUIPool**: 범용 UI 객체 풀링
- **GoldGainPopupPool**: 골드 팝업 전용 풀링

---

## 8. Tier & Progression System

### TierManager
- **4층 기반 배경 전환**: 층별 Unlock 규칙, Ascend UI 제어
- **가시성 관리**: `activeParent`/`hiddenParent`로 현재 층 외 캐릭터 숨김
- **리플렉션 기반**: 외부 시스템과의 의존성 최소화

---

## 9. Tools & Workflow Notes

### Addressables
- SD/LD 라벨 구성 및 Key 규칙(숫자 기반) 문서화
- 비동기 로딩으로 프레임 드롭 최소화

### DOTween
- 프로젝트 시작 시 `DOTween Utility Panel`에서 Setup 필수
- 모든 애니메이션을 DOTween으로 통일하여 일관된 느낌 제공

### 에디터 체크리스트
- `EDITOR_SETUP_CHECKLIST.md`: 반드시 연결해야 할 필드 목록 정리
  - Spotlight 패널, 도감 프리팹, 세이브 매니저, 각종 UI 패널 등

---

## 10. Code Flow Summary

### 게임 시작 플로우
1. `GameSystem.Awake()`: 매니저 간 의존성 주입
2. `SaveManager.Start()`: 세이브 파일 로드 시도
3. `GameSystem.Start()`: CSV 로드 → 스프라이트 로드 → `AssetsReady` 이벤트
4. 세이브 파일 있으면: `GirlFieldManager.ApplyLoadedData()` → 오프라인 보상 패널 표시
5. 세이브 파일 없으면: 로딩 패널 즉시 숨김

### 저장 플로우
1. `SaveManager.SaveGame()`: 모든 `ISaveable` 구현체에서 데이터 수집
2. JSON 직렬화 → 로컬 파일 저장 + PlayerPrefs 백업
3. 자동 저장: 1분 간격 또는 `OnApplicationPause/Quit`

### 수익 생성 플로우
1. `GirlFieldManager.Update()`: 1초 간격으로 `ComputeIdleGoldPerSec()` 계산
2. `EconomyManager.AddGold()`: 골드 추가 → `OnGoldChanged` 이벤트 → UI 갱신
3. 클릭 수익: `GirlCharacter.OnPointerClick()` → `GirlMergeManager.AddIncomeGold(isClick: true)` → 골드 팝업 표시

### 합성 플로우
1. 드래그 앤 드롭 또는 자동 합성 트리거
2. `GirlMergeManager.TryMerge()`: +2단 도약 확률 계산 (계승 등급 + 환생 상점)
3. 합성 애니메이션 → 새 캐릭터 스폰 → 합성 보너스 골드 지급

### 환생 플로우
1. `PrestigeManager.OnClickPrestigeButton()`: 확인 패널 표시
2. 사용자 확인 → `DoPrestige()`: 환생 포인트 계산 → 필드 비우기 → 경제 리셋 → 시작 자금 지급

---

이 문서(`readmeanlist.md`)는 포트폴리오 제출 시 프로젝트의 기술적 강점을 빠르게 파악할 수 있도록 만든 요약본입니다. 보다 상세한 사용법 및 실행 방법은 `README.md`의 본문을 참고하세요.
