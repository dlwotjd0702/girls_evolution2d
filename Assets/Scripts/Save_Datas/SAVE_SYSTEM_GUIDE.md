# 저장 시스템 동작 가이드

## 저장 시스템 개요

게임은 **로컬 저장**과 **온라인 저장(클라우드)** 두 가지 방식으로 동작합니다.

### 저장 방식 비교

| 항목 | 로컬 저장 | 온라인 저장 (클라우드) |
|------|----------|---------------------|
| **동작 위치** | 기기 내부 파일 | Google Play Games 클라우드 |
| **필수 여부** | ✅ 항상 활성화 | ⚙️ 옵션 (설정 가능) |
| **인터넷 필요** | ❌ 불필요 | ✅ 필요 |
| **기기 간 동기화** | ❌ 불가능 | ✅ 가능 |
| **데이터 손실 위험** | ⚠️ 기기 변경/삭제 시 손실 | ✅ 낮음 |

## 저장 동작 플로우

### 1. 게임 시작 시 (로드)

```
게임 시작
  └─> SaveManager.Start()
      └─> LoadGameWithCloudRecovery()
          │
          ├─> 1. 로컬 저장 로드 시도
          │   ├─> 성공 → 게임 시작
          │   └─> 실패 → 다음 단계
          │
          └─> 2. 클라우드 저장 확인 (enableCloudSave가 true일 때만)
              ├─> 로컬 저장이 없으면 → 클라우드에서 복구
              │   └─> 복구 성공 → 로컬에 저장 → 게임 시작
              │
              └─> 로컬 저장이 있으면 → 플레이 시간 비교
                  ├─> 클라우드가 더 길면 → SaveConflictPanel 표시
                  │   ├─> 클라우드 선택 → 클라우드 데이터 사용
                  │   └─> 로컬 선택 → 로컬 데이터 사용
                  └─> 로컬이 더 길거나 같으면 → 로컬 사용
```

### 2. 게임 플레이 중 (저장)

#### 자동 저장
- **주기**: 1분마다 자동 저장
- **위치**: 로컬 저장 (항상)
- **클라우드**: 설정에 따라 자동 업로드 (`autoCloudSave` 옵션)

#### 수동 저장
- **방법**: 설정 패널에서 "수동 저장" 버튼 클릭
- **동작**:
  1. 로컬 저장 (항상)
  2. 로그인 체크
  3. 로그인 안되어 있으면 → 자동 로그인 시도
  4. 로그인 성공하면 → 클라우드에도 저장

#### 특정 상황에서 자동 저장
- 앱 일시정지 시 (`OnApplicationPause`)
- 앱 종료 시 (`OnApplicationQuit`)
- 환생 완료 시
- 최고 레벨 달성 시

## Unity 에디터 설정

### 필수 설정 (온라인 저장 사용 시)

#### 1. SaveManager GameObject 설정

1. **씬에 GameObject 생성**
   - Hierarchy에서 우클릭 → `Create Empty`
   - 이름: `SaveManager`
   - `SaveManager.cs` 스크립트 추가

2. **Inspector 설정**
   ```
   Auto Save Interval: 60 (초)
   
   Cloud Save Settings:
   ✅ Enable Cloud Save: 체크 (온라인 저장 사용)
   ✅ Auto Cloud Save: 체크 (자동 업로드)
   ✅ Sync On Start: 체크 (시작 시 동기화)
   ```

3. **설정 설명**
   - **Enable Cloud Save**: 클라우드 저장 기능 활성화
     - `false`면 로컬 저장만 사용
     - `true`면 로컬 + 클라우드 저장
   - **Auto Cloud Save**: 로컬 저장 후 자동으로 클라우드에도 업로드
     - `false`면 수동 저장 버튼으로만 업로드
   - **Sync On Start**: 게임 시작 시 클라우드와 동기화
     - 플레이 시간 비교 및 충돌 해결

#### 2. CloudSaveManager GameObject 설정

1. **씬에 GameObject 생성**
   - Hierarchy에서 우클릭 → `Create Empty`
   - 이름: `CloudSaveManager`
   - `CloudSaveManager.cs` 스크립트 추가

2. **추가 설정 불필요**
   - 자동으로 Google Play Games SDK 초기화
   - 자동으로 로그인 시도

#### 3. Google Play Games SDK 설정 (한 번만)

1. **Window → Google Play Games → Setup**
2. **Android Resources XML 붙여넣기**
   - Google Play Console에서 복사한 XML 붙여넣기
   - "Web App Client ID (Optional)" 필드는 비워둠

**자세한 설정 방법**: `GOOGLE_PLAY_GAMES_SETUP.md` 참고

### 선택 설정

#### 온라인 저장을 사용하지 않는 경우

**SaveManager Inspector 설정:**
```
Enable Cloud Save: ❌ 체크 해제
```

- 로컬 저장만 사용
- CloudSaveManager GameObject는 필요 없음
- 인터넷 없어도 정상 동작

## 저장 위치

### 로컬 저장
- **경로**: 
  - 에디터: `Assets/../EditorSaves/save.json`
  - 빌드: `Application.persistentDataPath/save.json`
- **백업 파일**: `save_backup.json` (자동 생성)
- **PlayerPrefs**: 호환성을 위해 병행 저장

### 클라우드 저장
- **위치**: Google Play Games 클라우드
- **파일명**: `girls_evolution_save`
- **접근**: Google 계정으로 로그인 필요

## 설정 시나리오

### 시나리오 1: 온라인 저장만 사용 (권장)
```
SaveManager 설정:
✅ Enable Cloud Save: true
✅ Auto Cloud Save: true
✅ Sync On Start: true

동작:
- 로컬 저장 + 클라우드 저장 병행
- 게임 시작 시 클라우드 동기화
- 기기 간 자동 동기화
```

### 시나리오 2: 온라인 저장 + 수동 업로드
```
SaveManager 설정:
✅ Enable Cloud Save: true
❌ Auto Cloud Save: false
✅ Sync On Start: true

동작:
- 로컬 저장은 자동
- 클라우드 저장은 수동 저장 버튼으로만
- 시작 시에는 동기화만 (업로드는 안 함)
```

### 시나리오 3: 오프라인 전용
```
SaveManager 설정:
❌ Enable Cloud Save: false

동작:
- 로컬 저장만 사용
- 인터넷 없어도 정상 동작
- 기기 간 동기화 불가능
```

## 자동 동작 요약

### 게임 시작 시
1. ✅ 로컬 저장 로드 (우선)
2. ✅ 클라우드 저장 확인
3. ✅ 로컬이 없으면 클라우드에서 복구
4. ✅ 클라우드가 더 최신이면 선택 패널 표시

### 게임 플레이 중
1. ✅ 1분마다 자동 저장 (로컬)
2. ✅ 자동 저장 시 클라우드 업로드 (옵션)
3. ✅ 환생/레벨 달성 시 즉시 저장

### 게임 종료 시
1. ✅ 로컬 저장
2. ✅ 클라우드 업로드 (옵션)

## 문제 해결

### 온라인 저장이 작동하지 않는 경우

1. **CloudSaveManager GameObject 확인**
   - 씬에 `CloudSaveManager` GameObject가 있는지 확인
   - 스크립트가 추가되어 있는지 확인

2. **SaveManager 설정 확인**
   - `Enable Cloud Save`가 체크되어 있는지 확인
   - Inspector에서 설정 확인

3. **Google Play Games SDK 설정 확인**
   - `Window > Google Play Games > Setup`에서 XML이 설정되어 있는지 확인
   - Android 패키지 이름이 일치하는지 확인

4. **로그인 상태 확인**
   - 실제 기기에서 Google Play Games 로그인 확인
   - 테스트 계정이 Google Play Console에 등록되어 있는지 확인

### 오프라인에서도 작동하나요?

✅ **네, 완전히 안전합니다!**
- 로컬 저장은 항상 작동 (인터넷 불필요)
- 클라우드 저장 실패해도 로컬 저장은 성공
- 예외 발생하지 않음
- 인터넷이 없어도 게임 플레이 가능

## 참고 문서

- `GOOGLE_PLAY_GAMES_SETUP.md`: Google Play Games SDK 설정 상세 가이드
- `EDITOR_SETUP_GUIDE.md`: Unity 에디터 설정 체크리스트
- `OFFLINE_ENVIRONMENT_CHECK.md`: 오프라인 환경 검수 보고서
