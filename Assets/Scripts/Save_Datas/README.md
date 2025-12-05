# Google Play Games 연동 문서

이 폴더에는 Google Play Games SDK를 사용한 클라우드 저장 및 업적 연동에 관한 문서들이 있습니다.

## 📚 문서 목록

### 메인 가이드
- **`GOOGLE_PLAY_GAMES_SETUP.md`** ⭐
  - 전체 설정 가이드 (SDK 설치부터 Unity 설정까지)
  - 가장 먼저 읽어야 할 문서

### 상세 가이드
- **`ACHIEVEMENT_SETUP.md`**
  - 업적 생성 및 ID 설정 상세 가이드
  - Google Play Console에서 업적 생성 방법
  - Unity에서 업적 ID 입력 방법

- **`SAVE_SYSTEM_GUIDE.md`**
  - 저장 시스템 동작 가이드
  - 로컬 저장 vs 클라우드 저장
  - 설정 시나리오

- **`SAVE_SYSTEM_QUICK_GUIDE.md`**
  - 저장 시스템 빠른 가이드
  - 간단한 설정 방법

- **`CLOUD_SAVE_AUTO_SETUP.md`**
  - 클라우드 자동 저장 설정 가이드

- **`CLOUD_SAVE_MECHANISM.md`**
  - 클라우드 저장 메커니즘 상세 설명

- **`OFFLINE_ENVIRONMENT_CHECK.md`**
  - 오프라인 환경 검수 보고서

- **`EDITOR_SETUP_GUIDE.md`**
  - Unity 에디터 설정 체크리스트

## 🚀 빠른 시작

### 1단계: 기본 설정
1. `GOOGLE_PLAY_GAMES_SETUP.md` 읽기
2. Google Play Console에서 게임 서비스 설정
3. SHA-1 인증서 지문 등록
4. Unity에서 Android Resources XML 붙여넣기

### 2단계: 업적 설정
1. `ACHIEVEMENT_SETUP.md` 읽기
2. Google Play Console에서 업적 5개 생성
3. Unity에서 업적 ID 입력

### 3단계: 클라우드 저장 설정
1. `SAVE_SYSTEM_QUICK_GUIDE.md` 읽기
2. CloudSaveManager GameObject 생성
3. SaveManager Inspector 옵션 체크

## 📋 체크리스트

### Google Play Console 설정
- [ ] 게임 서비스 생성
- [ ] SHA-1 인증서 지문 등록
- [ ] Android Resources 다운로드
- [ ] 업적 5개 생성 및 게시
- [ ] 업적 ID 복사

### Unity 프로젝트 설정
- [ ] Google Play Games SDK 설치
- [ ] Android Resources XML 붙여넣기
- [ ] 업적 ID 입력 (Inspector)
- [ ] CloudSaveManager GameObject 생성
- [ ] AchievementManager GameObject 생성
- [ ] SaveManager 클라우드 저장 옵션 활성화

### 테스트
- [ ] 실제 기기에서 로그인 테스트
- [ ] 클라우드 저장/로드 테스트
- [ ] 업적 달성 테스트
- [ ] 업적 UI 표시 테스트

## 🔗 관련 파일

### 스크립트
- `CloudSaveManager.cs` - 클라우드 저장 관리
- `AchievementManager.cs` - 업적 관리
- `SaveManager.cs` - 저장 관리 (클라우드 통합)

## ❓ 자주 묻는 질문

**Q: 업적 ID는 어디서 확인하나요?**
A: Google Play Console > 게임 서비스 > 성취도에서 확인할 수 있습니다. 자세한 내용은 `ACHIEVEMENT_SETUP.md` 참고.

**Q: 클라우드 저장이 실패하면 어떻게 되나요?**
A: 로컬 저장과 병행되므로 클라우드 저장 실패 시에도 로컬 데이터는 안전합니다.

**Q: 에디터에서 테스트할 수 있나요?**
A: 아니요. Google Play Games SDK는 Android 빌드에서만 동작합니다. 에디터에서는 로그 메시지만 확인 가능합니다.
