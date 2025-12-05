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
   - 키 별칭: `evolution`
   - 키스토어 비밀번호는 프로젝트 설정에서 확인
3. **참고**: OAuth 클라이언트 ID는 Google이 자동으로 생성합니다
   - Unity에서는 이 클라이언트 ID를 직접 입력할 필요가 없습니다
   - SHA-1만 등록하면 자동으로 연결됩니다

#### SHA-1 인증서 지문 확인 방법

**방법 1: keytool 명령어 사용 (권장)**

1. 명령 프롬프트(CMD) 또는 PowerShell 열기
2. 프로젝트 루트 디렉토리로 이동
3. 다음 명령어 실행:
   ```bash
   keytool -list -v -keystore Assets/keystore.keystore -alias evolution
   ```
4. 비밀번호 입력 (프로젝트 설정에서 확인)
5. 출력 결과에서 `SHA1:` 또는 `SHA-1:` 뒤의 값을 복사
   - 예: `AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD`

**방법 2: Unity 에디터에서 확인**

1. Unity 에디터에서 `Edit` > `Project Settings` > `Player` > `Android` > `Publishing Settings` 이동
2. `Keystore Manager` 클릭
3. 키스토어 정보 확인 (SHA-1 표시 여부는 Unity 버전에 따라 다름)

**방법 3: Google Play Console에서 확인 (이미 업로드된 앱인 경우)**

1. Google Play Console > `릴리스` > `프로덕션` 이동
2. 최신 앱 번들/APK의 `앱 서명` 섹션에서 SHA-1 확인

**참고:**
- 개발용 디버그 키스토어의 SHA-1도 필요할 수 있습니다
- 디버그 키스토어 SHA-1 확인:
  ```bash
  keytool -list -v -keystore "%USERPROFILE%\.android\debug.keystore" -alias androiddebugkey -storepass android -keypass android
  ```

### 2.3 리더보드 생성
1. `게임 서비스` > `리더보드` 이동
2. 새 리더보드 생성 (각각 생성):
   - **최고 달성 레벨** (Leaderboard ID 예: `CgkI-XXXXX`)
   - **총 획득 골드** (Leaderboard ID 예: `CgkI-YYYYY`)
   - **환생 횟수** (Leaderboard ID 예: `CgkI-ZZZZZ`)
   - **총 플레이타임** (Leaderboard ID 예: `CgkI-WWWWW`)
3. 각 리더보드의 ID를 복사 (예: `CgkI-XXXXXXXXXXXXXX`)

## 3. Unity 프로젝트 설정

### 3.1 게임 ID 설정 (중요!)
1. Unity 에디터에서 `Window` > `Google Play Games` > `Setup` 열기
2. **"Resources Definition" 필드에 Android Resources XML 붙여넣기**
   - Google Play Console에서 Android Resources 다운로드:
     - Google Play Console > `게임 서비스` > `설정 및 관리` > `설정`
     - `연결된 앱` 섹션에서 "Android Resources 다운로드" 클릭
     - 다운로드된 XML 파일 내용을 복사
   - Unity의 "Resources Definition" 큰 텍스트 영역에 붙여넣기
   - ⚠️ **이 XML 안에 게임 ID가 포함되어 있습니다!**

3. **"Web App Client ID (Optional)" 필드는 비워두세요**
   - 설명에 "It is not required for Game Services"라고 명시되어 있습니다
   - 클라우드 저장/리더보드 기능에는 필요 없습니다
   - 고급 기능(사용자 ID 토큰 접근 등)을 사용할 때만 필요합니다

4. Android 패키지 이름 확인 (`Project Settings` > `Player` > `Android`)
   - 패키지 이름이 Google Play Console에 등록된 것과 일치해야 합니다

5. "Setup" 버튼 클릭
   - 자동으로 `GameInfo.cs` 파일이 생성되고 게임 ID가 설정됩니다

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
