# Google Play Games SDK 설정 가이드

이 문서는 Google Play Games SDK를 사용한 클라우드 저장 및 리더보드 연동 설정 방법을 안내합니다.

## 1. Google Play Games SDK 설치

### Unity Package Manager를 통한 설치
1. Unity 에디터에서 `Window` > `Package Manager` 열기
2. 왼쪽 상단의 `+` 버튼 클릭 > `Add package from git URL...`
3. 다음 URL 입력:
   ```
   https://github.com/playgameservices/play-games-plugin-for-unity.git
   ```
4. 설치 완료 대기

### 또는 수동 설치
- [Google Play Games Plugin for Unity](https://github.com/playgameservices/play-games-plugin-for-unity/releases)에서 최신 버전 다운로드
- Unity 프로젝트에 임포트

## 2. Google Play Console 설정

### 2.1 게임 등록
1. [Google Play Console](https://play.google.com/console) 접속
2. 게임 선택 또는 새 게임 생성
3. `게임 서비스` > `설정 및 관리` > `설정` 이동

### 2.2 OAuth 2.0 클라이언트 ID 설정
1. `연결된 앱` 섹션에서 Android 앱 연결
2. SHA-1 인증서 지문 등록 (keystore 파일 기준)
   - 키스토어 파일 위치: `Assets/keystore.keystore`
   - 키스토어 비밀번호는 프로젝트 설정에서 확인

### 2.3 리더보드 생성
1. `게임 서비스` > `리더보드` 이동
2. 새 리더보드 생성 (각각 생성):
   - **최고 달성 레벨** (Leaderboard ID 예: `CgkI-XXXXX`)
   - **총 획득 골드** (Leaderboard ID 예: `CgkI-YYYYY`)
   - **환생 횟수** (Leaderboard ID 예: `CgkI-ZZZZZ`)
   - **총 플레이타임** (Leaderboard ID 예: `CgkI-WWWWW`)
3. 각 리더보드의 ID를 복사 (예: `CgkI-XXXXXXXXXXXXXX`)

## 3. Unity 프로젝트 설정

### 3.1 게임 ID 설정
1. Unity 에디터에서 `Window` > `Google Play Games` > `Setup` 열기
2. Google Play Console에서 복사한 `게임 ID` 입력
3. Android 패키지 이름 확인 (`Project Settings` > `Player` > `Android`)

### 3.2 리더보드 ID 설정
1. `Assets/Scripts/LeaderboardManager.cs` 파일 열기
2. Inspector에서 다음 필드에 리더보드 ID 입력:
   - `Leaderboard Max Level`: 최고 달성 레벨 리더보드 ID
   - `Leaderboard Total Gold`: 총 획득 골드 리더보드 ID
   - `Leaderboard Prestige Count`: 환생 횟수 리더보드 ID
   - `Leaderboard Play Time`: 총 플레이타임 리더보드 ID

또는 코드에서 직접 수정:
```csharp
[SerializeField] private string leaderboardMaxLevel = "CgkI-XXXXXXXXXXXXXX";
[SerializeField] private string leaderboardTotalGold = "CgkI-YYYYYYYYYYYYYY";
[SerializeField] private string leaderboardPrestigeCount = "CgkI-ZZZZZZZZZZZZZ";
[SerializeField] private string leaderboardPlayTime = "CgkI-WWWWWWWWWWWWW";
```

## 4. 씬 설정

### 4.1 매니저 오브젝트 생성
1. 게임 씬에서 빈 GameObject 생성 (`CloudSaveManager`, `LeaderboardManager`)
2. 각각에 해당 스크립트 컴포넌트 추가

또는 기존 매니저 오브젝트에 추가:
- `SaveManager`와 같은 부모 오브젝트에 추가
- `DontDestroyOnLoad` 자동 적용됨

### 4.2 SaveManager 설정
1. `SaveManager` 오브젝트 선택
2. Inspector에서 다음 옵션 설정:
   - `Enable Cloud Save`: 클라우드 저장 활성화
   - `Auto Cloud Save`: 로컬 저장 후 자동 클라우드 저장
   - `Sync On Start`: 게임 시작 시 클라우드와 동기화

## 5. 테스트

### 5.1 에디터에서 테스트
- 에디터에서는 실제 클라우드 저장/리더보드 기능이 동작하지 않습니다
- 로그 메시지를 통해 호출 여부 확인 가능

### 5.2 실제 기기에서 테스트
1. 개발 빌드 생성
2. Google Play Console에서 테스트 계정 추가
3. 테스트 계정으로 로그인하여 기능 확인

## 6. 사용 방법

### 6.1 클라우드 저장
```csharp
// 자동: SaveManager가 자동으로 클라우드에 저장
// 수동 저장
SaveManager.Instance.SaveToCloud();

// 클라우드에서 로드
SaveManager.Instance.LoadFromCloud();
```

### 6.2 리더보드
```csharp
// 점수 제출
LeaderboardManager.Instance.SubmitMaxLevel(25);
LeaderboardManager.Instance.SubmitTotalGold(1000000);
LeaderboardManager.Instance.SubmitPrestigeCount(5);

// 리더보드 UI 표시
LeaderboardManager.Instance.ShowMaxLevelLeaderboard();
LeaderboardManager.Instance.ShowLeaderboard(); // 전체 리더보드
```

### 6.3 로그인 관리
```csharp
// 수동 로그인
CloudSaveManager.Instance.SignIn((success) => {
    if (success) {
        Debug.Log("로그인 성공!");
    }
});

// 로그아웃
CloudSaveManager.Instance.SignOut();
```

## 7. 주의사항

- Google Play Games SDK는 **Android 빌드**에서만 동작합니다 (에디터 제외)
- 리더보드 ID는 Google Play Console에서 생성 후 설정해야 합니다
- 클라우드 저장은 로그인 상태에서만 동작합니다
- 로컬 저장과 클라우드 저장이 병행되므로, 클라우드 저장 실패 시에도 로컬 데이터는 안전합니다

## 8. 문제 해결

### 로그인이 안 되는 경우
- Google Play Console에서 OAuth 클라이언트 ID 설정 확인
- SHA-1 인증서 지문이 올바르게 등록되었는지 확인
- 기기가 Google Play Games 앱에 로그인되어 있는지 확인

### 클라우드 저장이 안 되는 경우
- 로그인 상태 확인 (`CloudSaveManager.Instance.IsAuthenticated`)
- 인터넷 연결 확인
- Google Play Console에서 Saved Games API 활성화 확인

### 리더보드가 표시되지 않는 경우
- 리더보드 ID가 올바른지 확인
- Google Play Console에서 리더보드가 게시되었는지 확인
- 테스트 계정이 올바르게 설정되었는지 확인
