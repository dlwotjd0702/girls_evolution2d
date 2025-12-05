# 클라우드 저장 및 리더보드 시스템 구현 완료

## 구현 완료 항목

### 1. CloudSaveManager.cs
- Google Play Games SDK 기반 클라우드 저장 시스템
- 로그인/로그아웃 기능
- 클라우드 저장/로드 기능
- 충돌 해결 로직 (최신 세이브 우선)
- 로컬 저장과 클라우드 저장 병행

**주요 기능:**
- `SignIn()` / `SignOut()`: 수동 로그인/로그아웃
- `SaveToCloud()`: 클라우드에 저장
- `LoadFromCloud()`: 클라우드에서 로드
- `SyncWithCloud()`: 로컬과 클라우드 자동 동기화

### 2. LeaderboardManager.cs
- Google Play Games SDK 기반 리더보드 시스템
- 여러 리더보드 카테고리 지원
- 점수 제출 및 리더보드 UI 표시

**리더보드 카테고리:**
- 최고 달성 레벨 (`leaderboardMaxLevel`)
- 총 획득 골드 (`leaderboardTotalGold`)
- 환생 횟수 (`leaderboardPrestigeCount`)
- 총 플레이타임 (`leaderboardPlayTime`)

**주요 기능:**
- `SubmitMaxLevel()`: 최고 레벨 점수 제출
- `SubmitTotalGold()`: 총 골드 점수 제출
- `SubmitPrestigeCount()`: 환생 횟수 점수 제출
- `SubmitPlayTime()`: 플레이타임 점수 제출
- `ShowLeaderboard()`: 리더보드 UI 표시
- `SubmitAllScores()`: 모든 점수 일괄 제출

### 3. SaveManager 통합
- 클라우드 저장 옵션 추가
- 자동 클라우드 저장 기능
- 게임 시작 시 클라우드 동기화
- 주기적 리더보드 점수 제출

**새로운 옵션:**
- `enableCloudSave`: 클라우드 저장 활성화
- `autoCloudSave`: 로컬 저장 후 자동 클라우드 저장
- `syncOnStart`: 게임 시작 시 클라우드와 동기화

### 4. 자동 리더보드 업데이트 통합
- **환생 시**: 환생 횟수 자동 제출 (`PrestigeManager.DoPrestige()`)
- **최고 레벨 달성 시**: 최고 레벨 자동 제출 (`GirlFieldManager.UpdateMaxLevel()`)
- **주기적**: 5분마다 모든 점수 일괄 제출 (`SaveManager.SaveGame()`)

## 설정 가이드

자세한 설정 방법은 `GOOGLE_PLAY_GAMES_SETUP.md` 파일을 참고하세요.

### 빠른 시작

1. **Google Play Games SDK 설치**
   - Unity Package Manager에서 설치
   - 또는 GitHub에서 최신 버전 다운로드

2. **Google Play Console 설정**
   - 게임 등록 및 OAuth 클라이언트 ID 설정
   - 리더보드 4개 생성 (최고 레벨, 총 골드, 환생 횟수, 플레이타임)

3. **Unity 프로젝트 설정**
   - `Window` > `Google Play Games` > `Setup`에서 게임 ID 입력
   - `LeaderboardManager.cs`의 Inspector에서 리더보드 ID 입력

4. **씬 설정**
   - `CloudSaveManager` 및 `LeaderboardManager` GameObject 생성
   - 각각에 해당 스크립트 컴포넌트 추가
   - `SaveManager`의 클라우드 저장 옵션 활성화

## 사용 방법

### 클라우드 저장
```csharp
// 수동 저장
SaveManager.Instance.SaveToCloud();

// 클라우드에서 로드
SaveManager.Instance.LoadFromCloud();

// 동기화
CloudSaveManager.Instance.SyncWithCloud();
```

### 리더보드
```csharp
// 점수 제출
LeaderboardManager.Instance.SubmitMaxLevel(25);
LeaderboardManager.Instance.SubmitTotalGold(1000000);
LeaderboardManager.Instance.SubmitPrestigeCount(5);

// 리더보드 UI 표시
LeaderboardManager.Instance.ShowMaxLevelLeaderboard();
LeaderboardManager.Instance.ShowLeaderboard(); // 전체 리더보드
```

### 로그인 관리
```csharp
// 로그인
CloudSaveManager.Instance.SignIn((success) => {
    Debug.Log(success ? "로그인 성공" : "로그인 실패");
});

// 로그아웃
CloudSaveManager.Instance.SignOut();
```

## 자동 동작

다음 상황에서 자동으로 클라우드 저장 및 리더보드 점수 제출이 이루어집니다:

1. **자동 저장 시** (1분마다)
   - 로컬 저장 후 클라우드에도 자동 저장 (옵션)

2. **환생 완료 시**
   - 환생 횟수 리더보드에 자동 제출

3. **최고 레벨 달성 시**
   - 최고 레벨 리더보드에 자동 제출

4. **5분마다**
   - 모든 리더보드 점수 일괄 제출 (최신 데이터 동기화)

## 주의사항

- Google Play Games SDK는 **Android 빌드**에서만 동작합니다 (에디터 제외)
- 리더보드 ID는 Google Play Console에서 생성 후 설정해야 합니다
- 클라우드 저장은 로그인 상태에서만 동작합니다
- 로컬 저장과 클라우드 저장이 병행되므로, 클라우드 저장 실패 시에도 로컬 데이터는 안전합니다

## 파일 구조

```
Assets/Scripts/
├── Save_Datas/
│   ├── CloudSaveManager.cs              # 클라우드 저장 관리
│   ├── SaveManager.cs                    # 저장 관리 (클라우드 통합)
│   ├── SaveData.cs                       # 저장 데이터 구조
│   ├── GOOGLE_PLAY_GAMES_SETUP.md       # 설정 가이드
│   └── CLOUD_SAVE_AND_LEADERBOARD_SUMMARY.md  # 이 문서
├── LeaderboardManager.cs                # 리더보드 관리
├── PlayStatsTracker.cs                  # 플레이 통계 (리더보드 데이터 소스)
├── Mainsystem/
│   └── PrestigeManager.cs               # 환생 관리 (리더보드 통합)
└── Girl/
    └── GirlFieldManager.cs              # 필드 관리 (리더보드 통합)
```

## 다음 단계

1. Google Play Console에서 리더보드 생성 및 ID 확인
2. Unity 에디터에서 리더보드 ID 설정
3. 실제 기기에서 테스트
4. UI 버튼 연결 (선택사항):
   - 로그인/로그아웃 버튼
   - 클라우드 저장/로드 버튼
   - 리더보드 표시 버튼
