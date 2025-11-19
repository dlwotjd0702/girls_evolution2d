# Girls Evolution 2D

Unity 기반의 2D 방치형/진화형 게임 프로젝트입니다. 소녀 캐릭터를 합성하여 레벨을 올리고, 골드를 수집하며, 업그레이드와 프레스티지 시스템을 통해 성장하는 게임입니다.

## 🎮 게임 개요

### 핵심 게임플레이
- **합성 시스템**: 같은 레벨의 캐릭터 2개를 드래그하여 합성하면 다음 레벨의 캐릭터가 생성됩니다
- **25단계 진화**: 레벨 1부터 25까지 총 25단계의 캐릭터 진화 시스템
- **4층 구조**: 0층(1~8레벨), 1층(9~16레벨), 2층(17~24레벨), 3층(25레벨)로 구성된 층 시스템
- **방치형 수익**: 캐릭터가 자동으로 골드를 생성하며, 오프라인 보상 시스템 지원
- **환생 시스템**: 레벨 25 달성 후 환생하여 영구 보너스를 획득할 수 있습니다

### 주요 시스템
1. **골드 시스템**: 2^(레벨-1) 기반의 지수적 수익 성장
2. **자동화**: 자동 소환, 자동 합성 기능 지원
3. **업그레이드**: 수동 소환 최대치, 쿨타임, 필드 최대 칸수, 클릭 보너스 등
4. **프레스티지 상점**: 환생 포인트로 수익 배수, +2단 확률, 시작 자금 등 구매 가능

## 📁 프로젝트 구조

### 주요 폴더 구조
```
Assets/Scripts/
├── Mainsystem/          # 핵심 게임 시스템
│   ├── GameSystem.cs           # 전체 시스템 초기화 및 관리 (싱글톤)
│   ├── GirlMergeManager.cs     # 캐릭터 합성 로직 (+2단 도약 확률 포함)
│   └── PrestigeManager.cs      # 환생 시스템 및 환생 상점 관리
│
├── Girl/                # 캐릭터 관련
│   ├── GirlCharacter.cs        # 캐릭터 개체 (움직임, 클릭, 드래그)
│   ├── GirlData.cs             # 캐릭터 데이터 구조
│   ├── GirlDataManager.cs      # CSV/TSV 데이터 로더
│   ├── GirlFieldManager.cs     # 필드 관리 (소환, 수익 계산, 발견 이펙트)
│   └── SimpleUIPool.cs         # UI 오브젝트 풀링
│
├── Shop/                # 상점 및 경제
│   ├── EconomyManager.cs       # 골드 관리, 업그레이드, 수익 계산
│   ├── ShopPanelController.cs  # 골드/보석 탭 상점 UI
│   └── PrestigeShopPanelController.cs  # 환생 상점 UI
│
├── Save_Datas/          # 저장 시스템
│   ├── SaveData.cs             # 저장 데이터 구조
│   └── SaveManager.cs          # 저장/로드 관리 (현재 PlayerPrefs 사용)
│
├── Tiers/               # 층 시스템
│   └── TierManager.cs          # 층 전환, 언락, 배경 전환 애니메이션
│
├── UI/                  # UI 관리
│   └── GameUIManager.cs        # 게임 UI 통합 관리
│
└── Interface/           # 인터페이스
    └── ISaveable.cs            # 저장 가능한 객체 인터페이스
```

## 🔄 코드 플로우

### 게임 시작 플로우
1. **GameSystem.Awake()**: 싱글톤 초기화, 매니저 간 의존성 주입
2. **GameSystem.Start()**: 
   - `GirlDataManager.LoadAsync()`: CSV/TSV에서 캐릭터 데이터 로드
   - `GirlSpriteAddressableLoader.LoadAllGirlSpritesAsync()`: SD/LD 스프라이트 로드
3. **SaveManager.Start()**: `LoadGame()` 호출하여 저장 데이터 복원
4. **각 매니저 초기화**: EconomyManager, GirlFieldManager, PrestigeManager 등

### 게임 루프 플로우

#### 골드 수익 흐름
```
GirlFieldManager.Update()
  └─> ComputeIdleGoldPerSec()
      └─> 각 캐릭터의 GetIncome() 합산
          └─> EconomyManager.GetLevelIncomePerSec(level)
              └─> 2^(level-1) 기반 계산
      └─> 계승 등급 배수 × 환생 상점 배수 적용
  └─> 1초마다 EconomyManager.AddGold() 호출
```

#### 합성 플로우
```
사용자 드래그 또는 자동 합성
  └─> GirlMergeManager.TryMergeByDrag() / TryAutoMerge()
      └─> MergeRoutine()
          └─> +2단 도약 확률 계산 (계승 등급 + 환생 상점)
          └─> MergeAnimation() 실행
          └─> 기존 캐릭터 제거, 새 캐릭터 소환
          └─> 합성 보너스 골드 지급
```

#### 소환 플로우
```
수동 소환 버튼 클릭 또는 자동 소환
  └─> GirlFieldManager.OnClickSpawnButton() / TryAutoSpawn()
      └─> SpawnGirl(level, position)
          └─> GirlDataManager.GetDataByLevel()로 데이터 조회
          └─> GirlSpriteAddressableLoader.GetSpriteForData()로 스프라이트 로드
          └─> SimpleUIPool에서 캐릭터 오브젝트 가져오기
          └─> GirlCharacter.Init() 및 OnGetFromPool()
          └─> 첫 발견 시 PlayDiscoveryOnce() (LD → SD 전환 애니메이션)
```

#### 환생 플로우
```
PrestigeManager.DoPrestige()
  └─> 환생 포인트 계산 (레벨 25 이상 캐릭터 기준)
  └─> 필드 비우기
  └─> EconomyManager.ResetGoldUpgradesForPrestige()
  └─> TierManager.SwitchTo(0)
  └─> 시작 자금 지급 (계급 배수 × 환생 상점 배수 적용)
```

### 저장/로드 플로우
```
SaveManager.SaveGame()
  └─> FindObjectsOfType<ISaveable>()로 모든 저장 가능 객체 수집
  └─> 각 객체의 CollectSaveData() 호출
  └─> JsonUtility.ToJson()로 직렬화
  └─> PlayerPrefs.SetString("SaveData", json) 저장

SaveManager.LoadGame()
  └─> PlayerPrefs.GetString("SaveData") 로드
  └─> JsonUtility.FromJson<SaveData>()로 역직렬화
  └─> 각 객체의 ApplyLoadedData() 호출
```

## 🎯 주요 기능 상세

### 캐릭터 시스템
- **25단계 진화**: 각 레벨마다 고유한 이름과 수익량을 가진 캐릭터
- **SD/LD 스프라이트**: 필드에서는 SD, 첫 발견 시 LD 일러스트 표시
- **자동 움직임**: 랜덤 점프 및 바운스 애니메이션
- **드래그 앤 드롭**: 캐릭터를 드래그하여 합성 가능한 대상에 가까이 가면 하이라이트

### 경제 시스템
- **지수적 성장**: 레벨당 수익 = 2^(레벨-1) 골드/초
- **업그레이드**: 수동 소환 최대치, 쿨타임, 필드 최대 칸수, 클릭 보너스 등
- **자동화**: 자동 소환/합성 간격은 업그레이드 레벨에 따라 단축
- **소환 시스템**: 레벨별 소환 비용 = 60초 수익 × 구매 횟수별 배수

### 환생 시스템
- **환생 포인트**: 레벨 25 이상 캐릭터 기준으로 획득
- **환생 상점**: 12종의 영구 업그레이드 구매 가능
  - 핵심 4종: 수익 배수, +2단 확률, 시작 자금, 환생 포인트 획득량
  - Plus 8종: 각종 업그레이드의 영구 보정치

### 층 시스템
- **4층 구조**: 레벨에 따라 자동으로 층 분류
- **층 전환**: Ascend 버튼으로 상층 이동, 배경 전환 애니메이션
- **자동 언락**: 레벨 9, 17, 25 달성 시 각 층 자동 언락

## 🛠 기술 스택

- **Unity**: 2D 프로젝트
- **C#**: 모든 스크립트
- **DOTween**: 애니메이션 라이브러리
- **Addressables**: 스프라이트 비동기 로딩
- **TextMesh Pro**: UI 텍스트 렌더링
- **PlayerPrefs**: 현재 저장 시스템 (로컬 파일 저장 예정)

## 📊 데이터 구조

### 캐릭터 데이터 (girls.tsv)
- `id`: 고유 ID
- `name`: 캐릭터 이름
- `level`: 레벨 (1~25)
- `incomePerSec`: 초당 수익 (기본값, 실제는 2^(level-1) 적용)
- `mergeCount`: 합성 필요 개수 (현재 2)
- `spriteName`: 스프라이트 키
- `unlockDesc`: 언락 설명

### 저장 데이터 (SaveData)
- 골드, 업그레이드 레벨, 자동화 설정
- 발견한 레벨 마스크, 필드의 캐릭터 목록
- 환생 포인트, 환생 상점 레벨
- 현재 층, 언락된 층 마스크

## 🚀 실행 방법

1. Unity Hub에서 프로젝트 폴더 열기
2. `SampleScene.unity` 실행
3. 게임 시작 시 자동으로 저장 데이터 로드 (있는 경우)

## 📝 개발 노트

- 현재 저장 시스템은 PlayerPrefs를 사용하며, 향후 로컬 파일 저장으로 전환 예정
- Addressables를 사용하여 SD/LD 스프라이트를 비동기 로딩
- 리플렉션을 활용하여 외부 매니저(계승 등급, 환생 상점)와의 의존성 최소화
- UI 오브젝트 풀링을 통해 성능 최적화

---

## 📋 TODO 리스트

### 1. 로컬 저장 활성화
- [ ] 로컬 파일 시스템을 사용한 세이브 파일 생성
  - [ ] `Application.persistentDataPath`에 JSON 파일로 저장
  - [ ] 게임 시작 시 자동으로 세이브 파일 불러오기
  - [ ] 세이브 파일 리셋/삭제 기능 구현 (테스트용)
  - [ ] 세이브 파일 백업/복원 기능 (선택사항)

### 2. 도감 시스템 제작
- [ ] LD 도감 패널 UI 제작
  - [ ] 도감 슬롯 그리드 레이아웃 (25개 슬롯)
  - [ ] 언락된 슬롯만 클릭 가능하도록 설정
  - [ ] 미해방 슬롯은 비활성화 표시
- [ ] 도감 팝업 패널
  - [ ] 슬롯 클릭 시 팝업 패널 표시
  - [ ] 상단: LD 일러스트 크게 표시
  - [ ] 하단: 이름, 레벨, income 정보 표시
- [ ] 도감 데이터 관리
  - [ ] 발견한 레벨 추적 (기존 `discoveredLevels` 활용)
  - [ ] 도감 언락 상태 저장/로드

### 3. 캐릭터 움직임 루프 개선
- [ ] 이동 방향 전환 확인 및 수정
  - [ ] `GirlCharacter.StartJump()`에서 방향 전환 로직 검토
  - [ ] 이동 방향에 따라 스프라이트 Flip 적용
- [ ] Idle 상태 애니메이션 개선
  - [ ] 뛰거나 또잉거리지 않을 때 미세한 그루브 애니메이션 추가
  - [ ] 호흡 효과 또는 미세한 스케일 변화
- [ ] 점프 애니메이션 자연스럽게
  - [ ] 점프 궤적 및 타이밍 조정
  - [ ] 착지 시 바운스 효과 개선
- [ ] 또잉거리는 애니메이션 자연스럽게
  - [ ] `BounceAnim()` 타이밍 및 스케일 변화 조정
