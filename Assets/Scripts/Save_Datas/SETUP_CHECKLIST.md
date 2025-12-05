# Google Play Games 설정 체크리스트

## ✅ Google Play Console 설정

### 기본 설정
- [ ] Google Play Console 접속
- [ ] 게임 서비스 생성 또는 선택
- [ ] `게임 서비스` > `설정 및 관리` > `설정` 이동

### OAuth 2.0 클라이언트 ID 설정
- [ ] Android 앱 연결
- [ ] SHA-1 인증서 지문 등록 (프로덕션 키스토어)
- [ ] SHA-1 인증서 지문 등록 (디버그 키스토어, 선택사항)
- [ ] Android Resources 다운로드

### 업적 생성
- [ ] 업적 1: 레벨 25 달성 생성 및 게시
- [ ] 업적 2: 클릭 1,000회 생성 및 게시
- [ ] 업적 3: 1시간 플레이 생성 및 게시
- [ ] 업적 4: 강화 10레벨 생성 및 게시
- [ ] 업적 5: 1계층 발견 생성 및 게시
- [ ] 각 업적 ID 복사

### 테스트 설정
- [ ] 테스트 계정 추가 (Gmail 주소)
- [ ] 테스트 기기에서 Google Play Games 앱 로그인 확인

---

## ✅ Unity 프로젝트 설정

### Google Play Games SDK 설치
- [ ] Unity Package Manager에서 SDK 설치
- [ ] 또는 수동 설치 완료

### 게임 ID 설정
- [ ] `Window` > `Google Play Games` > `Setup` 열기
- [ ] Android Resources XML 붙여넣기
- [ ] "Web App Client ID (Optional)" 필드 비워두기
- [ ] Android 패키지 이름 확인
- [ ] "Setup" 버튼 클릭

### 씬 설정
- [ ] `CloudSaveManager` GameObject 생성
- [ ] `CloudSaveManager.cs` 스크립트 추가
- [ ] `AchievementManager` GameObject 생성
- [ ] `AchievementManager.cs` 스크립트 추가
- [ ] `SaveManager` GameObject 확인
- [ ] `PlayStatsTracker` GameObject 확인

### SaveManager Inspector 설정
- [ ] `Enable Cloud Save`: ✅ 체크
- [ ] `Auto Cloud Save`: ✅ 체크
- [ ] `Sync On Start`: ✅ 체크 (선택사항)

### AchievementManager Inspector 설정
- [ ] `Achievement Level 25`: 업적 ID 입력
- [ ] `Achievement Clicks 1000`: 업적 ID 입력
- [ ] `Achievement Play Time 1 Hour`: 업적 ID 입력
- [ ] `Achievement Upgrade 10`: 업적 ID 입력
- [ ] `Achievement Tier 1`: 업적 ID 입력

---

## ✅ 빌드 설정

### Android 빌드 설정
- [ ] `File` > `Build Settings` > `Android` 선택
- [ ] `Player Settings` 확인:
  - [ ] Package Name 설정
  - [ ] Minimum API Level: 21 이상
  - [ ] Target API Level: 최신 권장
  - [ ] Internet Access: Required

### Keystore 설정
- [ ] Keystore 생성 또는 기존 키스토어 사용
- [ ] SHA-1 인증서 지문 확인
- [ ] Google Play Console에 SHA-1 등록 확인

---

## ✅ 테스트

### 실제 기기 테스트
- [ ] 개발 빌드 생성
- [ ] 테스트 기기에 설치
- [ ] Google Play Games 로그인 확인
- [ ] 클라우드 저장 테스트
- [ ] 업적 달성 테스트
- [ ] 업적 UI 표시 테스트

### 로그 확인
- [ ] Unity Console에서 `[CloudSaveManager]` 로그 확인
- [ ] Unity Console에서 `[AchievementManager]` 로그 확인
- [ ] Unity Console에서 `[SaveManager]` 로그 확인

---

## ✅ 기능 확인

### 클라우드 저장
- [ ] 게임 시작 시 자동 로그인 확인
- [ ] 수동 저장 버튼 동작 확인
- [ ] 클라우드 저장 성공 확인
- [ ] 다른 기기에서 클라우드 로드 확인

### 업적 시스템
- [ ] 레벨 25 달성 시 업적 해제 확인
- [ ] 클릭 1,000회 달성 시 업적 해제 확인
- [ ] 1시간 플레이 달성 시 업적 해제 확인
- [ ] 강화 10레벨 달성 시 업적 해제 확인
- [ ] 1계층 발견 시 업적 해제 확인
- [ ] 업적 UI 표시 확인

### 새로 시작하기
- [ ] 새로 시작하기 버튼 동작 확인
- [ ] 로컬 저장 삭제 확인
- [ ] 클라우드 저장 삭제 확인
- [ ] DOTween 에러 없이 씬 리로드 확인

---

## 📝 참고 문서

- `GOOGLE_PLAY_GAMES_SETUP.md`: SDK 설정 상세 가이드
- `ACHIEVEMENT_SETUP.md`: 업적 설정 상세 가이드
- `SAVE_SYSTEM_QUICK_GUIDE.md`: 저장 시스템 빠른 가이드
- `EDITOR_SETUP_GUIDE.md`: Unity 에디터 설정 체크리스트
