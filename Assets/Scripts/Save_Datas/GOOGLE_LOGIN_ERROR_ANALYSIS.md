# Google Play Games 로그인 오류 분석 보고서

## 📋 오류 로그 분석

### 발생한 오류 로그 (원본)
```
2025-12-07 21:39:22.744 10617 10692 Warn Unity *** [Play Games Plugin 2.1.0] 12/07/25 21:39:22 +00:00 ERROR: Returning an error code.
2025-12-07 21:39:22.744 10617 10692 Warn Unity GooglePlayGames.OurUtils.PlayGamesHelperObject:Update()
2025-12-07 21:39:22.744 10617 10692 Warn Unity 
2025-12-07 21:39:22.778 10617 10692 Info Unity [CloudSaveManager] 수동 로그인 결과: False, 상태: Canceled
2025-12-07 21:39:22.778 10617 10692 Info Unity <>c__DisplayClass34_0:<SignIn>b__0(SignInStatus)
2025-12-07 21:39:22.778 10617 10692 Info Unity GooglePlayGames.OurUtils.PlayGamesHelperObject:Update()
2025-12-07 21:39:22.778 10617 10692 Info Unity 
```

### 오류 로그 상세 분석

#### 1. 첫 번째 오류
```
[Play Games Plugin 2.1.0] ERROR: Returning an error code.
```
- **발생 위치**: `AndroidClient.cs:178` - `SignInOnResult()` 메서드
- **로그 레벨**: `Warn Unity`
- **호출 스택**: `GooglePlayGames.OurUtils.PlayGamesHelperObject:Update()`
- **의미**: 
  - Google Play Games SDK가 로그인 시도 중 오류 코드를 반환함
  - `isAuthenticated`가 `false`로 확인되어 `SignInStatus.Canceled` 상태로 처리됨
  - 이는 사용자 인증이 실패했거나 취소되었음을 의미

#### 2. 두 번째 오류
```
[CloudSaveManager] 수동 로그인 결과: False, 상태: Canceled
```
- **발생 위치**: `CloudSaveManager.cs:154` - `SignIn()` 메서드의 콜백
- **로그 레벨**: `Info Unity`
- **호출 스택**: `<>c__DisplayClass34_0:<SignIn>b__0(SignInStatus)`
- **의미**:
  - 사용자가 설정 패널에서 수동으로 로그인을 시도했으나 실패
  - `SignInStatus.Canceled` 상태로 반환됨
  - 로그인 프로세스가 완전히 취소되었거나 인증에 실패함

### 오류 발생 위치
1. **AndroidClient.cs:178** - `SignInOnResult()` 메서드
   - `isAuthenticated`가 `false`일 때 `SignInStatus.Canceled` 반환
   - 로그: `"Returning an error code."` 출력

2. **CloudSaveManager.cs:154** - `SignIn()` 메서드의 콜백
   - `PlayGamesPlatform.Instance.Authenticate()`의 결과를 받아 처리
   - 로그인 실패 시 사용자에게 알림

---

## 🔍 원인 분석

### 1. 오류 발생 메커니즘 (상세)

#### 단계별 오류 발생 과정

**1단계: 로그인 시도**
- 사용자가 설정 패널에서 "구글플레이 로그인" 버튼 클릭
- `CloudSaveManager.SignIn()` 메서드 호출
- `PlayGamesPlatform.Instance.Authenticate()` 실행

**2단계: Android 네이티브 인증**
- `AndroidClient.Authenticate(isAutoSignIn: false)` 호출
- Android의 `GamesSignInClient.signIn()` 메서드 호출
- Google Play Games 서비스와 통신 시도

**3단계: 인증 결과 확인**
- `authenticationResult.isAuthenticated()` 호출
- 결과가 `false`로 반환됨

**4단계: 오류 처리**
- `AndroidClient.SignInOnResult(isAuthenticated: false)` 실행
- `AuthStateLock` 락을 획득한 후 오류 로그 출력
- `SignInStatus.Canceled` 상태로 콜백 호출

**5단계: 콜백 처리**
- `CloudSaveManager`의 콜백에서 실패 상태 확인
- "수동 로그인 결과: False, 상태: Canceled" 로그 출력
- 사용자에게 실패 메시지 표시

#### 왜 `isAuthenticated`가 `false`인가?

`AndroidClient.cs`의 `SignInOnResult()` 메서드에서 `isAuthenticated`가 `false`로 반환되는 이유:

1. **사용자가 로그인 다이얼로그에서 취소**
   - Google Play Games 로그인 UI가 표시되었으나 사용자가 "취소" 버튼 클릭
   - 또는 뒤로가기 버튼으로 다이얼로그 닫음

2. **기기에 Google 계정이 로그인되어 있지 않음**
   - 기기 설정 > 계정에 Google 계정이 없음
   - Google 계정이 있으나 로그아웃 상태

3. **Google Play Games 앱이 설치되어 있지 않음**
   - 기기에 Google Play Games 앱이 설치되지 않음
   - 앱이 설치되어 있으나 버전이 너무 낮거나 손상됨

4. **Google Play Games 서비스가 비활성화됨**
   - 기기 설정에서 Google Play Games 서비스가 비활성화됨
   - 서비스가 일시적으로 사용 불가능한 상태

5. **네트워크 연결 문제**
   - 인터넷 연결이 없거나 불안정함
   - 방화벽이나 보안 설정으로 인해 Google 서비스 접근 차단

6. **앱 서명 키 불일치**
   - 개발 빌드와 릴리즈 빌드의 서명 키가 다름
   - Google Play Console에 등록된 서명 키와 일치하지 않음

7. **OAuth 클라이언트 ID 설정 오류**
   - Unity 프로젝트의 Google Play Games 설정이 잘못됨
   - Google Play Console의 OAuth 클라이언트 ID와 불일치

### 2. 코드 흐름 (상세)

```
[사용자 액션]
  └─ SettingsPanel.OnClickGoogleLogin() 호출
      └─ CloudSaveManager.Instance.SignIn(callback) 호출
          │
          ├─ [조건 확인] IsAuthenticated == true?
          │   └─ 이미 로그인됨 → callback(true) 즉시 반환
          │
          └─ [로그인 시도] PlayGamesPlatform.Instance.Authenticate(callback)
              │
              └─ PlayGamesPlatform.Authenticate()
                  └─ AndroidClient.Authenticate(isAutoSignIn: false, callback)
                      │
                      ├─ [조건 확인] mAuthState == Authenticated?
                      │   └─ 이미 인증됨 → callback(SignInStatus.Success) 즉시 반환
                      │
                      └─ [네이티브 호출] getGamesSignInClient().signIn()
                          │
                          ├─ [성공] AndroidTaskUtils.AddOnSuccessListener()
                          │   └─ authenticationResult.isAuthenticated() 확인
                          │       │
                          │       ├─ true → SignInOnResult(true, callback)
                          │       │   └─ getCurrentPlayer() 호출
                          │       │       └─ callback(SignInStatus.Success)
                          │       │
                          │       └─ false → SignInOnResult(false, callback) ⚠️ 여기서 오류 발생!
                          │           └─ Logger.e("Returning an error code.")
                          │           └─ callback(SignInStatus.Canceled)
                          │
                          └─ [실패] AndroidTaskUtils.AddOnFailureListener()
                              └─ Logger.e("Authentication failed - " + exception)
                              └─ callback(SignInStatus.InternalError)

[콜백 처리]
  └─ CloudSaveManager.SignIn()의 callback 실행
      └─ status == SignInStatus.Canceled 확인
          └─ Debug.Log("수동 로그인 결과: False, 상태: Canceled") ⚠️ 두 번째 오류 로그
          └─ callback(false) 호출
              └─ SettingsPanel에서 실패 메시지 표시
```

### 3. 오류 발생 시점별 상세 설명

#### 시점 1: 네이티브 인증 실패 (21:39:22.744)
- **시간**: `2025-12-07 21:39:22.744`
- **스레드**: `10617` (메인 프로세스), `10692` (워커 스레드)
- **위치**: `AndroidClient.SignInOnResult()` 내부
- **상황**: 
  - `isAuthenticated`가 `false`로 확인됨
  - `AuthStateLock` 락을 획득한 상태에서 오류 로그 출력
  - `SignInStatus.Canceled` 상태로 콜백 준비

#### 시점 2: Unity 콜백 처리 (21:39:22.778)
- **시간**: `2025-12-07 21:39:22.778` (약 34ms 후)
- **스레드**: `10617` (메인 프로세스), `10692` (워커 스레드)
- **위치**: `CloudSaveManager.SignIn()`의 익명 콜백
- **상황**:
  - `PlayGamesHelperObject.Update()`를 통해 Unity 메인 스레드로 콜백 전달
  - `CloudSaveManager`에서 로그인 실패 상태 확인
  - 사용자에게 실패 메시지 표시 준비

---

## ✅ 적용된 개선 사항

### 1. 상세한 에러 로깅 추가

**CloudSaveManager.cs**에 다음 개선사항을 추가했습니다:

- **상태별 상세 메시지**: 각 `SignInStatus`에 대한 명확한 설명
- **실패 원인 안내**: Canceled와 InternalError 상태별 가능한 원인 목록
- **디버깅 정보**: 로그인 성공 시 SavedGameClient 초기화 확인 로그

```csharp
string statusMessage = status switch
{
    SignInStatus.Success => "성공",
    SignInStatus.Canceled => "취소됨 (사용자가 로그인을 취소했거나 Google Play Games가 설정되지 않음)",
    SignInStatus.InternalError => "내부 오류 (네트워크 오류 또는 Google Play Games 서비스 문제)",
    _ => "알 수 없는 상태"
};
```

### 2. 사용자 친화적 오류 메시지

**SettingsPanel.cs**에서 로그인 실패 시 더 자세한 안내 메시지를 제공합니다:

```csharp
string errorMessage = LocalizationManager.GetText(
    "로그인 실패. Google Play Games가 설치되어 있고 Google 계정이 로그인되어 있는지 확인해주세요.",
    "Login failed. Please make sure Google Play Games is installed and you are signed in with a Google account."
);
```

---

## 🛠️ 문제 해결 가이드

### 사용자가 로그인 실패 시 확인 사항

1. **Google Play Games 앱 설치 확인**
   - 기기에 Google Play Games 앱이 설치되어 있는지 확인
   - Google Play 스토어에서 설치 가능

2. **Google 계정 로그인 확인**
   - 기기 설정 > 계정에서 Google 계정이 로그인되어 있는지 확인

3. **네트워크 연결 확인**
   - 인터넷 연결이 정상인지 확인
   - Wi-Fi 또는 모바일 데이터 연결 확인

4. **Google Play Games 서비스 활성화**
   - 기기 설정 > 앱 > Google Play Games 서비스가 활성화되어 있는지 확인

5. **앱 ID 설정 확인**
   - Unity 프로젝트의 Google Play Games 설정이 올바른지 확인
   - 현재 설정: App ID `275737405566`

### 개발자 확인 사항

1. **Google Play Console 설정**
   - 게임이 Google Play Console에 올바르게 등록되어 있는지 확인
   - OAuth 클라이언트 ID가 올바르게 설정되어 있는지 확인

2. **빌드 설정**
   - 앱 서명 키가 Google Play Console에 등록되어 있는지 확인
   - 패키지 이름이 일치하는지 확인 (`com.MaybrSoft.girls_evolution`)

3. **테스트 환경**
   - 테스트 기기가 Google Play Games를 지원하는지 확인
   - 에뮬레이터에서는 제한적으로 작동할 수 있음

---

## 📊 로그인 상태별 처리

### SignInStatus.Success
- ✅ 로그인 성공
- `savedGameClient` 초기화
- 클라우드 저장/로드 기능 사용 가능

### SignInStatus.Canceled
- ⚠️ 로그인 취소 또는 설정 문제
- 가능한 원인:
  - 사용자가 로그인 다이얼로그 취소
  - Google Play Games 미설치
  - Google 계정 미로그인
- **처리**: 로컬 저장은 정상 작동, 클라우드 기능만 비활성화

### SignInStatus.InternalError
- ❌ 내부 오류
- 가능한 원인:
  - 네트워크 오류
  - Google Play Games 서비스 일시적 오류
  - 앱 ID 설정 오류
- **처리**: 재시도 권장, 로컬 저장은 정상 작동

---

## 🔄 현재 동작 방식

### 자동 로그인 (최초 실행)
1. 게임 최초 실행 시 자동으로 로그인 시도
2. 실패해도 게임은 정상 진행 (로컬 저장 사용)
3. 사용자가 나중에 설정에서 수동 로그인 가능

### 수동 로그인
1. 설정 패널에서 "구글플레이 로그인" 버튼 클릭
2. 로그인 시도
3. 성공/실패 메시지 표시
4. 실패 시 원인 안내 메시지 제공

### 오프라인 환경 처리
- 로그인 실패 시에도 로컬 저장은 정상 작동
- 클라우드 저장 기능만 비활성화
- 네트워크 연결 후 재시도 가능

---

## 📝 참고 사항

- Google Play Games SDK 2.1.0 사용 중
- 로그인 실패는 게임 플레이에 치명적이지 않음 (로컬 저장 사용)
- 클라우드 저장은 선택적 기능으로 설계됨
- 사용자가 원할 때 언제든지 수동 로그인 시도 가능

---

**작성일**: 2025-12-07  
**분석 대상**: Google Play Games 로그인 오류  
**상태**: 분석 완료 및 개선 사항 적용 완료

