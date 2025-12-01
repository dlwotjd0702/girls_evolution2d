# Girls Evolution 2D

<div align="center">

![Unity Version](https://img.shields.io/badge/Unity-2022.3.62f3-blue.svg)
![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS-lightgrey.svg)
![License](https://img.shields.io/badge/License-Proprietary-red.svg)

**Unity 기반 2D 방치형/진화형 합성 게임**

[게임 개요](#-게임-개요) • [시스템 아키텍처](#-시스템-아키텍처) • [기술 스택](#-기술-스택) • [프로젝트 구조](#-프로젝트-구조)

</div>

---

## 📋 목차

- [게임 개요](#-게임-개요)
- [시스템 아키텍처](#-시스템-아키텍처)
- [기술 스택](#-기술-스택)
- [프로젝트 구조](#-프로젝트-구조)
- [핵심 시스템 상세](#-핵심-시스템-상세)
- [데이터 파이프라인](#-데이터-파이프라인)
- [성능 최적화](#-성능-최적화)
- [설계 패턴](#-설계-패턴)
- [빌드 및 배포](#-빌드-및-배포)

---

## 🎮 게임 개요

### 핵심 게임플레이

- **합성 시스템**: 같은 레벨의 캐릭터 2개를 드래그하여 합성하면 다음 레벨의 캐릭터가 생성됩니다
- **25단계 진화**: 레벨 1부터 25까지 총 25단계의 캐릭터 진화 시스템
- **4층 구조**: 0층(1~8레벨), 1층(9~16레벨), 2층(17~24레벨), 3층(25레벨)로 구성된 층 시스템
- **방치형 수익**: 캐릭터가 자동으로 골드를 생성하며, 오프라인 보상 시스템 지원
- **환생 시스템**: 레벨 25 달성 후 환생하여 영구 보너스를 획득할 수 있습니다

### 주요 시스템

1. **골드 시스템**: `2^(레벨-1)` 기반의 지수적 수익 성장
2. **자동화**: 자동 소환(기본 60초), 자동 합성(기본 50초) 기능 지원
3. **업그레이드**: 수동 소환 최대치, 쿨타임, 필드 최대 칸수, 클릭 보너스(10% + 레벨×1%) 등
4. **프레스티지 상점**: 환생 포인트로 수익 배수, +2단 확률, 시작 자금 등 구매 가능
5. **프리미엄 통화**: 보석(Gem) 시스템으로 독립적인 업그레이드 경로 제공

---

## 🏗️ 시스템 아키텍처

### 초기화 플로우

```
SaveManager (ExecutionOrder: -200)
  └─> GameSystem (ExecutionOrder: -100)
      ├─> GirlDataManager.LoadAsync()        # CSV/TSV 데이터 로드
      ├─> GirlSpriteAddressableLoader        # SD/LD 스프라이트 비동기 로드
      └─> AssetsReadyEvent 브로드캐스트
```

### 게임 루프 구조

```
Update Loop:
├─> GirlFieldManager.Update()
│   ├─> ComputeIdleGoldPerSec()              # 1초마다 골드 지급
│   ├─> TryAutoSpawn()                       # 자동 소환 타이머
│   ├─> TryAutoMerge()                       # 자동 합성 타이머
│   └─> UpdateSpawnCharge()                  # 수동 소환 차지
│
├─> EconomyManager
│   └─> OnGoldChanged / OnUpgradeChanged 이벤트
│
└─> SaveManager
    └─> 60초마다 자동 저장
```

### 골드 수익 계산 흐름

```
GirlFieldManager.ComputeIdleGoldPerSec()
  └─> 각 GirlCharacter.GetIncome()
      └─> EconomyManager.GetLevelIncomePerSec(level)
          └─> baseIncome * Math.Pow(2, level - 1)
      └─> 계승 등급 배수 × 환생 상점 배수 적용
  └─> 1초마다 EconomyManager.AddGold() 호출
```

### 합성 시스템 흐름

```
사용자 드래그 또는 자동 합성
  └─> GirlMergeManager.TryMerge()
      ├─> 레벨 검증
      ├─> +2단 도약 확률 계산 (LegacyRankManager/PrestigeShopManager)
      ├─> 합성 애니메이션 (DOTween Sequence)
      ├─> 새 캐릭터 생성
      └─> 발견 이펙트 (LD → SD 전환)
```

---

## 🛠️ 기술 스택

### Unity & 패키지

- **Unity Version**: 2022.3.62f3
- **Addressables**: 1.22.3 - 에셋 비동기 로딩
- **TextMeshPro**: 3.0.7 - 고품질 텍스트 렌더링
- **DOTween**: 애니메이션 및 트윈 시스템
- **Unity Purchasing**: 5.0.2 - 인앱 구매
- **Google Mobile Ads**: 광고 통합

### 주요 라이브러리

- **CsvHelper**: CSV/TSV 파싱
- **System.Reflection**: 동적 메서드 호출 (외부 매니저 연동)

### 플랫폼

- **Android**: Google Play Games 연동
- **iOS**: (준비 중)

---

## 📁 프로젝트 구조

```
Assets/Scripts/
├── Mainsystem/                    # 핵심 게임 시스템
│   ├── GameSystem.cs              # 전체 시스템 초기화 및 관리 (싱글톤)
│   ├── GirlMergeManager.cs        # 캐릭터 합성 로직 (+2단 도약 확률 포함)
│   ├── PrestigeManager.cs         # 환생 시스템 및 환생 상점 관리
│   └── LegacyRankManager.cs       # 계승 등급 시스템
│
├── Girl/                          # 캐릭터 관련
│   ├── GirlCharacter.cs           # 캐릭터 개체 (움직임, 클릭, 드래그, 애니메이션)
│   ├── GirlData.cs                # 캐릭터 데이터 구조
│   ├── GirlDataManager.cs         # CSV/TSV 데이터 로더
│   ├── GirlFieldManager.cs        # 필드 관리 (소환, 수익 계산, 발견 이펙트)
│   ├── GirlSpriteAddressableLoader.cs  # Addressables 기반 스프라이트 로더
│   └── SimpleUIPool.cs            # UI 오브젝트 풀링
│
├── Shop/                          # 상점 및 경제
│   ├── EconomyManager.cs          # 골드 관리, 업그레이드, 수익 계산
│   ├── ShopPanelController.cs     # 골드/보석 탭 상점 UI
│   ├── AutoAutomationController.cs # 자동 소환/합성 UI 컨트롤러
│   └── PrestigeShopPanelController.cs  # 환생 상점 UI
│
├── Save_Datas/                    # 저장 시스템
│   ├── SaveData.cs                # 저장 데이터 구조
│   └── SaveManager.cs             # 저장/로드 관리 (JSON 기반, 백업 지원)
│
├── Tiers/                         # 층 시스템
│   └── TierManager.cs             # 층 전환, 언락, 배경 전환 애니메이션
│
├── UI/                            # UI 관리
│   ├── GameUIManager.cs           # 게임 UI 통합 관리
│   ├── EncyclopediaPanelController.cs  # 도감 시스템
│   ├── OfflineRewardPanel.cs     # 오프라인 보상 패널
│   ├── GoldGainPopup.cs           # 골드 획득 팝업
│   └── GoldGainPopupPool.cs      # 골드 팝업 풀링
│
├── makesomemoney/                 # 수익화 시스템
│   ├── PremiumCurrencyManager.cs  # 보석(Gem) 관리
│   ├── AdMobOfferService.cs      # AdMob 광고 서비스
│   ├── RewardedAdsManager_AdMob.cs  # 리워드 광고 관리
│   └── InsufficientFundsPanel.cs # 자금 부족 패널
│
├── Audio/                         # 오디오 시스템
│   └── AudioManager.cs            # BGM/SFX 통합 관리
│
├── Interface/                     # 인터페이스
│   └── ISaveable.cs               # 저장 가능한 객체 인터페이스
│
└── LocalizationManager.cs         # 다국어 지원 (한국어/영어)
```

---

## 🔧 핵심 시스템 상세

### 1. 캐릭터 시스템 (`GirlCharacter`)

#### 주요 기능

- **입력 처리**: `IPointerDownHandler`, `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, `IPointerClickHandler` 구현
- **애니메이션**: DOTween 기반 점프, 바운스, 펄스, Idle Groove
- **방향 전환**: `localScale.x` 플립 방식 (UI 레이캐스트 호환)
- **입력 잠금**: LD(발견) 연출 중 클릭/드래그 비활성화

#### 성능 최적화

```csharp
// WaitForSeconds 재사용으로 GC 할당 최소화
WaitForSeconds cachedDelay = null;
cachedDelay ??= new WaitForSeconds(jumpDelay);
```

### 2. 합성 시스템 (`GirlMergeManager`)

#### 핵심 로직

- **드래그 앤 드롭**: 두 캐릭터를 드래그하여 합성
- **자동 합성**: 최대 레벨 미만의 가장 가까운 쌍을 자동으로 합성
- **+2단 도약**: 확률 기반으로 2단계 건너뛰기 (리플렉션 기반 외부 매니저 연동)
- **최대 레벨 보호**: 현재 발견 최대 레벨은 자동 합성에서 제외

#### 성능 최적화

```csharp
// Vector2.Distance 대신 sqrMagnitude 사용
float distSqr = delta.sqrMagnitude;

// N > 80일 때 조기 종료로 O(N²) 스파이크 방지
if (n > HARD_PAIR_SCAN_LIMIT) {
    StartCoroutine(MergeRoutine(first, second));
    return;
}
```

### 3. 경제 시스템 (`EconomyManager`)

#### 골드 계산

- **기본 수익**: `2^(레벨-1)` 골드/초
- **클릭 보너스**: 초당 수익의 `10% + 강화 레벨 × 1%`
- **소수점 처리**: 모든 골드 추가 시 `Math.Ceiling()` 적용
- **가격 반올림**: 모든 골드 비용은 100원 단위로 반올림

#### 업그레이드 시스템

- **수동 소환**: 최대치, 쿨타임 업그레이드
- **필드 확장**: 최대 칸수 업그레이드
- **클릭 보너스**: 퍼센트 증가 방식 (10% → 11% → 12%...)
- **오프라인 보상**: 배수 및 최대 시간 업그레이드
- **자동화**: 자동 소환/합성 간격 단축

### 4. 저장 시스템 (`SaveManager`)

#### 특징

- **ISaveable 인터페이스**: 확장 가능한 저장 시스템
- **자동 저장**: 60초마다 자동 저장
- **백업 시스템**: `save_backup.json` 자동 생성
- **오프라인 시간 계산**: 저장 시간 기반 오프라인 보상 계산
- **PlayerPrefs 호환**: 기존 세이브 파일 호환성 유지

#### 저장 데이터 구조

```csharp
public class SaveData {
    public string savedAt;                    // 저장 시간
    public double gold;                       // 골드
    public int[] discoveredLevels;            // 발견한 레벨 목록
    public int[] upgradeLevels;               // 업그레이드 레벨
    // ... 기타 게임 상태
}
```

### 5. 프레스티지 시스템 (`PrestigeManager`)

#### 환생 조건

- 레벨 25 달성 시 환생 가능
- 환생 포인트 획득: `레벨 25 달성 횟수 × 배수`

#### 환생 상점 (12종 영구 업그레이드)

- 수익 배수
- +2단 도약 확률
- 시작 자금 배수
- 환생 포인트 획득량 증가
- 수동 소환 최대치/쿨타임 보너스
- 자동 소환/합성 간격 단축
- 필드 최대 칸수 보너스
- 클릭 보너스 배수
- 오프라인 보상 배수/시간 보너스

---

## 📊 데이터 파이프라인

### CSV/TSV 로딩

```csharp
// GirlDataManager.LoadAsync()
// - Addressables TextAsset 비동기 로드
// - CSV/TSV 자동 판별 (탭/쉼표 구분)
// - 컬럼명 자동 매칭
// - initialDirection 필드 지원 (1: 왼쪽, -1: 오른쪽)
```

### 스프라이트 로딩

```csharp
// GirlSpriteAddressableLoader.LoadAllGirlSpritesAsync()
// - SD 스프라이트 먼저 로드 (빠른 표시)
// - LD 스프라이트 순차 로드 (발견 연출용)
// - Addressables 라벨 기반 그룹 로딩
```

### 데이터 구조

```csharp
public class GirlData {
    public int id;
    public string name;
    public int level;
    public double incomePerSec;
    public int mergeCount;
    public string spriteName;
    public string unlockDesc;
    public int initialDirection;  // 1=왼쪽, -1=오른쪽
}
```

---

## ⚡ 성능 최적화

### 1. 객체 풀링

- **SimpleUIPool**: 범용 UI 객체 풀링
- **GoldGainPopupPool**: 골드 팝업 전용 풀링
- **EncyclopediaPanelController**: 슬롯 재사용으로 인스턴스화 최소화

### 2. 거리 계산 최적화

```csharp
// Vector2.Distance 대신 sqrMagnitude 사용
float distSqr = delta.sqrMagnitude;
if (distSqr < bestDistSqr) { ... }
```

### 3. 코루틴 GC 최소화

```csharp
// WaitForSeconds 재사용
WaitForSeconds cachedDelay = null;
cachedDelay ??= new WaitForSeconds(jumpDelay);
```

### 4. 리플렉션 최적화

```csharp
// MethodInfo.Invoke 대신 Func<float> 델리게이트 캐싱
private Func<float> _getLegacyTwoStepChance;
_getLegacyTwoStepChance = (Func<float>)Delegate.CreateDelegate(
    typeof(Func<float>), _legacyRankManager, _miGetTwoStepChance);
```

### 5. 이벤트 기반 UI 갱신

```csharp
// 값이 변경될 때만 UI 갱신
if (Math.Abs(gold - amount) < 1e-9) return;
gold = amount;
OnGoldChanged?.Invoke(gold);
RefreshGoldHUD();
```

### 6. 자동 합성 최적화

- N > 80일 때 조기 종료로 O(N²) 스파이크 방지
- `_mergingSet`으로 중복 합성 방지
- `IsFinal` 체크로 최종 레벨 제외

---

## 🎨 설계 패턴

### 1. 싱글톤 패턴

```csharp
public class GameSystem : MonoBehaviour {
    public static GameSystem Instance { get; private set; }
    // ...
}
```

**사용처**: `GameSystem`, `SaveManager`, `PremiumCurrencyManager`, `SimpleUIPool`, `GoldGainPopupPool`

### 2. 인터페이스 기반 확장성

```csharp
public interface ISaveable {
    void ApplyLoadedData(SaveData data);
    void CollectSaveData(SaveData data);
}
```

**장점**: 새로운 저장 가능한 객체를 쉽게 추가 가능

### 3. 이벤트 기반 통신

```csharp
public event Action<double> OnGoldChanged;
public event Action OnUpgradeChanged;
```

**장점**: UI와 게임 로직 간 느슨한 결합 유지

### 4. 리플렉션 기반 안전 호출

```csharp
// 외부 매니저가 없어도 안전하게 동작
var t = FindTypeByName("PrestigeShopManager");
if (t != null) {
    var method = t.GetMethod("GetClickBonusMul");
    // ...
}
```

**장점**: 컴파일 타임 의존성 없이 선택적 기능 활성화

### 5. 의존성 주입

```csharp
// GameSystem.Awake()에서 자동 연결
if (fieldManager.dataManager == null) 
    fieldManager.dataManager = girlDataManager;
```

**장점**: 매니저 간 의존성을 명확하게 관리

---

## 🚀 빌드 및 배포

### 빌드 설정

1. **Unity Version**: 2022.3.62f3
2. **Target Platform**: Android (API Level 21+)
3. **Scripting Backend**: IL2CPP (권장)
4. **Minify**: ProGuard (Android)

### Addressables 빌드

```bash
# Addressables 그룹 빌드 필요
# - Data 그룹: CSV/TSV 파일
# - SD/LD 스프라이트 그룹
```

### 키스토어 설정

- `Assets/keystore.keystore` 파일 존재
- 빌드 시 자동 적용

### 저장 경로

- **에디터**: `EditorSaves/save.json`
- **빌드**: `Application.persistentDataPath/save.json`

---

## 📝 주요 기능 요약

### ✅ 구현 완료

- [x] 25단계 캐릭터 진화 시스템
- [x] 드래그 앤 드롭 합성
- [x] 자동 소환/합성 시스템
- [x] 골드/보석 이중 통화 시스템
- [x] 환생 및 프레스티지 상점
- [x] 오프라인 보상 시스템
- [x] 다국어 지원 (한국어/영어)
- [x] 광고 통합 (AdMob)
- [x] 인앱 구매 시스템
- [x] 도감 시스템
- [x] 4층 구조 및 배경 전환
- [x] 발견 연출 (LD → SD 전환)
- [x] 자동 저장 시스템

### 🔄 최적화 완료

- [x] 객체 풀링 적용
- [x] 거리 계산 최적화 (sqrMagnitude)
- [x] 코루틴 GC 최소화
- [x] 리플렉션 델리게이트 캐싱
- [x] 이벤트 기반 UI 갱신
- [x] 자동 합성 알고리즘 최적화

---

## 📚 추가 문서

- [오디오 매니저 설정 가이드](Assets/Scripts/Audio/AUDIO_MANAGER_SETUP_GUIDE.md)
- [골드 팝업 설정 가이드](Assets/Scripts/UI/GOLD_POPUP_SETUP_GUIDE.md)
- [기존 README](Assets/Scripts/README.md)
- [기술 하이라이트](Assets/Scripts/old_md/readmeanalist.md)

---

## 👥 기여

이 프로젝트는 개인/팀 프로젝트입니다. 문의사항이 있으시면 이슈를 등록해주세요.

---

## 📄 라이선스

이 프로젝트는 사유 소프트웨어입니다. 무단 사용 및 배포를 금지합니다.

---

<div align="center">

**Made with ❤️ using Unity**

</div>

