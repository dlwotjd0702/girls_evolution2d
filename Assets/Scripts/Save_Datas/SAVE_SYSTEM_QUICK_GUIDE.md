# 저장 시스템 빠른 가이드

## 저장 시스템이 어떻게 작동하나요?

게임은 **로컬 저장(오프라인)**과 **온라인 저장(클라우드)** 두 가지 방식으로 저장됩니다.

### 기본 동작

1. **로컬 저장**: 항상 작동 (기기 내부 파일)
2. **온라인 저장**: 선택사항 (Google Play Games 클라우드)

## 온라인 설정이 필요한가요?

### 온라인 저장을 사용하려면

**추가 설정 필요:**
1. ✅ Google Play Games SDK 설정 (업적과 동일)
2. ✅ CloudSaveManager GameObject 생성
3. ✅ SaveManager Inspector에서 옵션 체크

**업적 설정이 완료되었다면:**
- Google Play Games SDK 설정은 이미 완료됨
- CloudSaveManager GameObject만 추가하면 됨

### 온라인 저장 없이 사용하려면

**추가 설정 불필요:**
- 로컬 저장만 사용
- SaveManager의 `Enable Cloud Save` 체크 해제하면 됨

## Unity 에디터에서 설정하는 방법

### 1. SaveManager GameObject 확인

**씬에 이미 있다면:**
- Inspector에서 다음 옵션만 확인:

```
Cloud Save Settings:
✅ Enable Cloud Save: 체크 (온라인 저장 사용 시)
✅ Auto Cloud Save: 체크 (자동 업로드)
✅ Sync On Start: 체크 (시작 시 동기화)
```

**씬에 없다면:**
1. Hierarchy에서 우클릭 → `Create Empty`
2. 이름: `SaveManager`
3. `SaveManager.cs` 스크립트 추가
4. Inspector에서 위 옵션 설정

### 2. CloudSaveManager GameObject 확인

**업적 설정이 완료되었다면:**
- Google Play Games SDK는 이미 설정됨
- CloudSaveManager GameObject만 추가하면 됨

**추가 방법:**
1. Hierarchy에서 우클릭 → `Create Empty`
2. 이름: `CloudSaveManager`
3. `CloudSaveManager.cs` 스크립트 추가
4. **추가 설정 불필요** (자동으로 동작)

### 3. Google Play Games SDK 설정 (이미 완료했다면 건너뛰기)

**업적 설정 시 이미 완료했다면:**
- ✅ Android Resources XML 붙여넣기 완료
- ✅ 게임 ID 설정 완료

**아직 안 했다면:**
1. `Window` > `Google Play Games` > `Setup`
2. Android Resources XML 붙여넣기
3. "Web App Client ID (Optional)" 필드는 비워둠

## 저장 시스템 동작

### 게임 시작 시

```
1. 로컬 저장 로드 (우선)
   └─> 있으면 → 게임 시작
   
2. 로컬 저장이 없으면
   └─> 클라우드에서 복구 시도
       └─> 성공하면 → 로컬에 저장 → 게임 시작
```

### 게임 플레이 중

```
1. 1분마다 자동 저장 (로컬)
2. 설정에 따라 클라우드에도 자동 업로드
3. 환생/레벨 달성 시 즉시 저장
```

### 수동 저장 버튼

```
설정 패널 → 수동 저장 버튼 클릭
  └─> 1. 로컬 저장 (항상)
  └─> 2. 로그인 체크
      └─> 로그인 안되어 있으면 → 자동 로그인 시도
      └─> 로그인 성공하면 → 클라우드에도 저장
```

## 설정 체크리스트

### 온라인 저장 사용 시

- [ ] SaveManager GameObject 존재
- [ ] CloudSaveManager GameObject 존재
- [ ] Google Play Games SDK 설정 완료 (Android Resources XML)
- [ ] SaveManager Inspector 설정:
  - [ ] `Enable Cloud Save`: ✅ 체크
  - [ ] `Auto Cloud Save`: ✅ 체크
  - [ ] `Sync On Start`: ✅ 체크

### 오프라인 전용 (온라인 저장 사용 안 함)

- [ ] SaveManager GameObject 존재
- [ ] SaveManager Inspector 설정:
  - [ ] `Enable Cloud Save`: ❌ 체크 해제

## 결론

### 온라인 저장 설정이 필요한가요?

**업적 설정이 완료되었다면:**
- ✅ Google Play Games SDK 설정: **완료됨**
- ✅ CloudSaveManager GameObject: **추가 필요**
- ✅ SaveManager 옵션: **체크만 하면 됨**

**별도 설정이 필요한 것:**
- ❌ 없음 (업적과 동일한 Google Play Games SDK 사용)

### 저장은 어떻게 되나요?

1. **로컬 저장**: 항상 자동 (1분마다)
2. **온라인 저장**: 옵션 (설정에 따라 자동 또는 수동)

**온라인 저장 사용 시:**
- 로컬 + 클라우드 모두 저장
- 기기 간 자동 동기화
- 데이터 손실 위험 낮음

**오프라인 전용:**
- 로컬 저장만 사용
- 인터넷 불필요
- 기기 간 동기화 불가능

## 참고

- **자세한 가이드**: `SAVE_SYSTEM_GUIDE.md` 참고
- **에디터 설정**: `EDITOR_SETUP_GUIDE.md` 참고
