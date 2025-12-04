# Girls Evolution 2D - 종합 TODO 리스트

> 프로젝트 전체 분석 기반 종합 TODO (2024년 기준)
> **마지막 업데이트**: 우선순위 재조정 완료

---

## 📊 프로젝트 개요

**Unity 2022.3.62f3** 기반 2D 방치형/진화형 합성 게임
- **핵심 시스템**: 25단계 캐릭터 진화, 합성 시스템, 방치형 수익, 환생 시스템
- **기술 스택**: Addressables, DOTween, TextMeshPro, Google Mobile Ads, Unity Purchasing
- **플랫폼**: Android (iOS 준비 중)

---

## ✅ 완료된 작업 요약

### 시스템 구현
- ✅ 로컬 저장 시스템 (JSON 기반, 백업 지원)
- ✅ 도감 시스템 (LD 도감 패널, 슬롯, 상세 팝업)
- ✅ 캐릭터 움직임 개선 (방향 전환, Idle 그루브, 점프/바운스)
- ✅ 자동소환/합성 시각 연출 (타이머 fill 이미지)
- ✅ 환생 이후 발견 연출시간 단축
- ✅ 소환비용 증가율 조정 (2배 → 2.2배)
- ✅ 강화골드 리밸런싱 (GROW 2.00 → 2.20)

### 성능 최적화 (완료)
- ✅ 드래그/합성 성능 개선 (같은 레벨 필터링, sqrMagnitude, early-out)
- ✅ 자동합성 알고리즘 최적화 (N>80 조기 종료)
- ✅ 도감 슬롯 풀링 (재사용 로직)
- ✅ 리플렉션 캐싱 (static 변수, 델리게이트 캐싱)

---

## 🔥 우선순위 작업 (즉시 진행)

### 1. 튜토리얼 시스템
- **설명**: 첫 실행 시 게임 조작법 안내
- **기능 요구사항**:
  - [ ] `TutorialManager.cs` 클래스 생성
  - [ ] 단계별 가이드 UI 패널
  - [ ] 소환 방법 안내
  - [ ] 합성 방법 안내 (드래그 앤 드롭)
  - [ ] 상점 사용법 안내
  - [ ] 티어 이동 방법 안내
  - [ ] 자동소환/자동합성 활성화 방법 안내
  - [ ] 튜토리얼 진행 상태 저장 (`SaveData`에 `tutorialCompleted` 필드 추가)
  - [ ] 스킵 기능
- **관련 파일**: 
  - `Assets/Scripts/UI/TutorialManager.cs` (신규 생성)
  - `Assets/Scripts/Save_Datas/SaveData.cs` (필드 추가)

### 2. 쿠폰 시스템 (서버 없이 내부 검증)
- **설명**: 쿠폰 코드 입력 및 보상 지급 (로컬 검증)
- **기능 요구사항**:
  - [ ] `CouponManager.cs` 클래스 생성
  - [ ] 쿠폰 코드 입력 UI
  - [ ] 내부 쿠폰 코드 딕셔너리 (코드 → 보상 매핑)
  - [ ] 쿠폰 검증 로직 (해시 기반 또는 간단한 암호화)
  - [ ] 보상 지급 시스템 (골드, 보석 등)
  - [ ] 사용한 쿠폰 저장 (`SaveData`에 `usedCoupons` 리스트 추가)
  - [ ] 중복 사용 방지
- **관련 파일**: 
  - `Assets/Scripts/UI/CouponManager.cs` (신규 생성)
  - `Assets/Scripts/Save_Datas/SaveData.cs` (필드 추가)

### 3. 온라인 저장 (Google Play Games)
- **설명**: Google Play Games 로그인 및 클라우드 저장
- **기능 요구사항**:
  - [ ] Google Play Games SDK 연동
  - [ ] 로그인/로그아웃 기능
  - [ ] 클라우드 저장/로드 기능
  - [ ] 기기 간 동기화
  - [ ] 로컬 저장과 클라우드 저장 병행
  - [ ] 충돌 해결 로직 (최신 세이브 우선 또는 사용자 선택)
- **관련 파일**: 
  - `Assets/Scripts/Save_Datas/CloudSaveManager.cs` (신규 생성)
  - `Assets/Scripts/Save_Datas/SaveManager.cs` (클라우드 저장 연동)

### 4. 플레이 디버그 정보 (SaveData 추가)
- **설명**: 밸런스 패치용 플레이 통계 수집
- **추가할 필드** (`SaveData.cs`):
  - [ ] `totalPlayTimeSeconds` (double) - 총 플레이타임 (초)
  - [ ] `firstPlayTime` (string) - 최초 플레이 시간
  - [ ] `lastPlayTime` (string) - 마지막 플레이 시간
  - [ ] `level25ReachedCount` (int) - 레벨 25 달성 횟수
  - [ ] `level25ReachedTimes` (List<string>) - 레벨 25 달성 시간 목록
  - [ ] `totalClicks` (long) - 총 클릭 횟수
  - [ ] `totalMerges` (long) - 총 합성 횟수
  - [ ] `totalSpawns` (long) - 총 소환 횟수
  - [ ] `totalGoldEarned` (double) - 총 획득 골드
  - [ ] `totalGoldSpent` (double) - 총 소비 골드
  - [ ] `prestigeCount` (int) - 환생 횟수 (이미 `totalPrestigeCount` 있음, 확인 필요)
- **구현 작업**:
  - [ ] `PlayStatsTracker.cs` 클래스 생성 (통계 수집)
  - [ ] 각 매니저에서 통계 업데이트 호출
  - [ ] 세이브/로드 시 통계 저장/복원
- **관련 파일**: 
  - `Assets/Scripts/Save_Datas/SaveData.cs` (필드 추가)
  - `Assets/Scripts/PlayStatsTracker.cs` (신규 생성)
  - `Assets/Scripts/Girl/GirlFieldManager.cs` (통계 업데이트)
  - `Assets/Scripts/Mainsystem/GirlMergeManager.cs` (통계 업데이트)
  - `Assets/Scripts/Shop/EconomyManager.cs` (통계 업데이트)

---

## ⚖️ 중간 우선순위 (간단한 최적화)

### 5. 간단한 최적화 작업

#### 5-1. 디버그 로그 레벨링 (간단)
- **작업 내용**:
  - [ ] `Log.cs` 래퍼 클래스 생성
  - [ ] `#if UNITY_EDITOR` 또는 빌드 플래그로 디버그 로그 제어
  - [ ] 주요 `Debug.Log` 호출을 `Log.Info`로 변경
- **예상 작업량**: 낮음
- **관련 파일**: 전체 프로젝트

#### 5-2. 자동합성 티어 필터링 (간단)
- **작업 내용**:
  - [ ] `TryAutoMerge()`에서 현재 티어의 캐릭터만 필터링
  - [ ] `TierManager.CurrentTierIndex`와 `TierRules.TierIndexFromLevel()` 사용
- **예상 작업량**: 낮음
- **관련 파일**: 
  - `Assets/Scripts/Mainsystem/GirlMergeManager.cs` (line 207-271)

#### 5-3. 합성 후 발견 연출 개선 (간단)
- **작업 내용**:
  - [ ] `GirlCharacter.cs`에 `PlayMergeBounce()` 메서드 추가
  - [ ] `MergeRoutine()`에서 발견 여부 확인 후 바운스 연출
- **예상 작업량**: 낮음
- **관련 파일**: 
  - `Assets/Scripts/Girl/GirlCharacter.cs`
  - `Assets/Scripts/Mainsystem/GirlMergeManager.cs`

---

## 📦 후순위 작업 (복잡한 최적화)

### 6. 복잡한 최적화 (성능 지장 없으면 나중에)

#### 6-1. SaveManager 최적화 (복잡)
- **설명**: `FindObjectsOfType` 제거, 등록 시스템으로 변경
- **상태**: 현재 성능 지장 없음 → 후순위
- **관련 파일**: `Assets/Scripts/Save_Datas/SaveManager.cs`

#### 6-2. 코루틴 최적화 (복잡)
- **설명**: 글로벌 타이머 방식으로 전환
- **상태**: 현재 성능 지장 없음 → 후순위
- **관련 파일**: `Assets/Scripts/Girl/GirlCharacter.cs`

---

## 📋 UI 작업 (Unity 에디터)

### 7. UI 프리팹 제작
- **설명**: 코드는 완료, Unity 에디터에서 UI만 제작 필요

#### 7-1. 도감 패널 UI
- [ ] 슬롯 프리팹 제작
- [ ] 그리드 레이아웃 구성
- [ ] 상세 팝업 패널 구성
- **관련 파일**: `Assets/Scripts/UI/EncyclopediaPanelController.cs`

#### 7-2. 세이브 파일 관리 UI
- [ ] 리셋/삭제 버튼 UI
- [ ] 세이브 파일 경로 표시
- **관련 파일**: `Assets/Scripts/Save_Datas/SaveManager.cs`

#### 7-3. 종료 확인 패널 UI
- [ ] 종료 확인 패널 UI 제작
- **관련 파일**: `Assets/Scripts/ExitPanelToggler.cs`

#### 7-4. 골드 부족 패널 UI
- [ ] 패널 루트 GameObject
- [ ] 메시지 텍스트, 광고 보상 텍스트
- [ ] 광고 시청 버튼, 아이콘
- **관련 파일**: `Assets/Scripts/makesomemoney/InsufficientFundsPanel.cs`

#### 7-5. 튜토리얼 UI
- [ ] 튜토리얼 가이드 패널
- [ ] 단계별 안내 UI
- [ ] 스킵 버튼

#### 7-6. 쿠폰 입력 UI
- [ ] 쿠폰 코드 입력 필드
- [ ] 입력 버튼
- [ ] 결과 메시지 표시

---

## 🎯 작업 우선순위 순서

### 1단계: 핵심 기능 구현 (즉시)
1. **플레이 디버그 정보 추가** - SaveData에 필드 추가 및 통계 수집 시스템
2. **튜토리얼 시스템** - 첫 실행 가이드
3. **쿠폰 시스템** - 내부 검증 방식
4. **온라인 저장** - Google Play Games 연동

### 2단계: 간단한 최적화 (1-2주)
5. 디버그 로그 레벨링
6. 자동합성 티어 필터링
7. 합성 후 발견 연출 개선

### 3단계: UI 작업 (Unity 에디터)
8. 튜토리얼 UI
9. 쿠폰 입력 UI
10. 도감 패널 UI
11. 기타 UI 프리팹

### 4단계: 복잡한 최적화 (나중에)
12. SaveManager 최적화
13. 코루틴 최적화

---

## 📝 구현 상세 사항

### 플레이 디버그 정보 필드 상세

```csharp
// SaveData.cs에 추가할 필드
[Serializable]
public class SaveData
{
    // ... 기존 필드 ...
    
    // 플레이 통계 (밸런스 패치용)
    public double totalPlayTimeSeconds = 0.0;      // 총 플레이타임 (초)
    public string firstPlayTime = "";              // 최초 플레이 시간
    public string lastPlayTime = "";               // 마지막 플레이 시간
    public int level25ReachedCount = 0;            // 레벨 25 달성 횟수
    public List<string> level25ReachedTimes = new List<string>(); // 달성 시간 목록
    public long totalClicks = 0;                   // 총 클릭 횟수
    public long totalMerges = 0;                   // 총 합성 횟수
    public long totalSpawns = 0;                   // 총 소환 횟수
    public double totalGoldEarned = 0.0;           // 총 획득 골드
    public double totalGoldSpent = 0.0;            // 총 소비 골드
}
```

### 쿠폰 시스템 구조

```csharp
// CouponManager.cs 기본 구조
public class CouponManager : MonoBehaviour
{
    // 내부 쿠폰 코드 딕셔너리 (코드 → 보상)
    private Dictionary<string, CouponReward> couponDatabase;
    
    // 사용한 쿠폰 목록 (SaveData에서 로드)
    private HashSet<string> usedCoupons;
    
    // 쿠폰 검증 및 지급
    public bool RedeemCoupon(string code);
}
```

### 튜토리얼 시스템 구조

```csharp
// TutorialManager.cs 기본 구조
public class TutorialManager : MonoBehaviour
{
    // 튜토리얼 단계
    private int currentStep = 0;
    private bool isCompleted = false;
    
    // 단계별 가이드 실행
    public void StartTutorial();
    public void NextStep();
    public void CompleteTutorial();
}
```

---

## 📚 참고 문서

- [프로젝트 README](README.md) - 전체 개요 및 아키텍처
- [기술 분석 보고서](Assets/Scripts/read/analysis_report.md) - 시스템 분석
- [에디터 설정 체크리스트](Assets/Scripts/old_md/EDITOR_SETUP_CHECKLIST.md) - Unity 에디터 설정

---

## 📌 참고사항

- **우선순위**: 튜토리얼 → 쿠폰 → 온라인 저장 → 플레이 디버그 정보
- **최적화**: 간단한 것만 우선, 복잡한 건 후순위
- **UI 작업**: 코드 완료 후 Unity 에디터에서 작업
- **플레이 디버그**: 밸런스 패치에 필수이므로 우선 구현 권장

---

**마지막 업데이트**: 우선순위 재조정 완료 (2024년)
