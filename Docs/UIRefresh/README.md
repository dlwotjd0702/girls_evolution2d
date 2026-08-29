# UI / 도감 작업 기록

## 최종 결과 — 2026-08-29: 192/192장 완료

**1–24단계 × 단계별 추가 스킨 4종 × SD/LD = 96세트, 192장**을 제작·개별 검수·Unity 등록했다. 기본 외형은 수량에 포함하지 않으며 25단계 이후는 이번 스킨 작업 범위에서 제외했다. 미등록 0세트, 재검수 대기 0세트다. 아래의 중간 수량과 검수 대기 문구는 해당 작업 시점의 이력이다.

- 전체 결과와 192개 PNG의 경로·해시·GUID·투명 픽셀 검사: `skin-192-completion.json`
- 전체 현재 이미지 모음: `FinalSkinReview/catalog-01.jpg`부터 `catalog-06.jpg`까지, 각각 4개 단계/32장
- 초기 20세트 최종 검수: `ExistingSkinReview/initial-review.json`
- 최종 Unity 편집 모드 회귀 검사: **1049개 통과**, 02:09:54 (`skin-192-regression-checks.txt`)
- 등록 상태: `skin-install-status.txt` — 96세트 등록, 미등록/재검수 대기 0

192장 모두 실제 RGBA 투명 배경, 캔버스 테두리 투명, 반투명 가장자리, 고유 이미지와 고유 Sprite GUID를 확인했다. 한글·영문 이름은 각각 96개이며 중복이 없다. 단계별 기본 외형 및 SD/LD의 머리·눈·의상·소품을 비교했고, 머리카락·팔·무기 사이의 흰 배경은 밝고 어두운 배경에서 확인했다. 흰 옷과 머리·금속 하이라이트는 보존했다. 기존 LD 6장의 913–975px 정사각형 캔버스는 불필요한 재샘플링을 피하기 위해 그대로 유지했고, 나머지 LD 90장은 1024px, SD 96장은 512px다.

초기 스킨 중 `lv05-b-LD`는 SD와 동일하게 후드를 올렸고, `moon-patrol-SD`의 발광 검은 LD와 같은 화면 오른쪽에 오도록 수정했다. 이 두 재작화의 원본·전체 프롬프트·승인·메타 보존 기록은 `SkinBatch17/replacements.json`에 있다. 배경 정리와 재작화 기록은 구분한다. 별도로 초기 스킨 15장의 흰 잔여물 14,619픽셀은 RGB와 크기를 바꾸지 않고 알파만 정리했다 (`existing-alpha-18.json`, `SkinBatch18/ExistingAlpha/report.json`).

최종 재임포트와 도감 편집 미리보기를 마친 뒤 원래 화면으로 복원했다. 마지막 등록 직후 씬과 최종 씬은 동일하고, 임시 미리보기 오브젝트는 저장되지 않았다. 실제 `EditorSaves/save.json`은 작업 전 SHA256 `55531C17BAF405C188DA20129310FD65007E2B6C2D8757611FEFDB0D9771F408`과 동일하다. 기존 변경사항을 되돌리지 않았다. Play Mode, 실제 기기 빌드, 광고 및 결제 실행은 하지 않았다.

재검증은 프로젝트 루트에서 `python Docs/UIRefresh/verify_skin_catalog.py`로 실행한다. 개별 원본/알파 처리 이력은 `python Docs/UIRefresh/verify_skin_batch.py --batch NN`으로 확인한다(01–16). 픽셀 검사는 시각적 검수를 대신하지 않는다.

## 디자인 기준 — 2026-08-28 사용자 피드백 반영

게임은 **미소녀 닌자** 수집/합성 게임이다. 초기 UI처럼 기능을 즉시 인지할 수 있어야 한다.

- 원래 여성 닌자 실루엣, 두루마리, 자동소환·자동합성, 상점·소환·도감의 익숙한 그림과 한글 표기를 유지한다.
- 장식이나 색감 통일을 이유로 기존 아이콘 의미를 바꾸지 않는다.
- 자동소환·자동합성은 이미지 안에 기능명과 ON/OFF가 이미 있으므로 별도 텍스트나 받침 배경을 추가하지 않는다.
- 초당수익은 기존 돈 아이콘과 숫자를 묶어 화면 가운데에 정렬한다. 숫자 길이가 바뀌어도 함께 가운데를 유지한다. 배율은 `(×100%)`, `(×150%)`, `(×200%)`처럼 표시하며 실제 수익 계산은 바꾸지 않는다.
- 원래 배치를 기준으로 실제 휴대폰 크기에서 글자, 대비, 터치 영역을 먼저 확인한다.
- 하단 버튼은 초기 씬의 바닥 피벗, 개별 스케일과 크기를 보존한다. 동일 크기로 일괄 정규화하지 않는다. 두루마리 배경과 충전 Fill의 RectTransform 및 Preserve Aspect는 동일해야 한다.
- 패널 모서리는 부드럽게 하되 아이콘 실루엣이나 글자를 잘라내지 않는다.
- 광고 보상은 상품 탭 상단에 유지한다. 메인에 수익 2배 버튼을 다시 넣지 않는다.
- 피버는 간단한 FEVER 표시와 큰 트랙만, 인원수는 왼쪽, 초당수익 뒤에 전체 배율 표시.

## 실험안의 처리

`Assets/Art/UI/Refresh`의 atlas 스타일은 사용자 피드백에 따라 채택하지 않았다.
`UIAndCollectionRefresh.Apply`는 실험 기록용이며 메뉴에서 제거했다.
**하단 배치만 복구: Tools → Girls Evolution → UI and Collection → Restore Original Bottom Navigation (Ctrl+Alt+U).**
**자동 버튼 추가 라벨 제거·수익 아이콘 복구만: 같은 메뉴의 Fix Main Icons.**
**초당수익만 가운데 정렬·백분율 표시: Tools → Girls Evolution → Fix Centered Income.**
`Restore Readability`는 넓은 범위의 UI 마이그레이션이므로 일부 수정에 사용하지 않는다.
이는 현재 실험용 Sprite 참조만 초기 리소스로 복구한다. 씬 전체를 Git 상태로 되돌리지 않는다.
`original-refs.json`은 HEAD에서 추출한 참조 목록이며, 구매 콜백·성장 로직·스킨·세이브 데이터에는 적용하지 않는다.

UI 실험 이미지는 내장 imagegen으로 생성했으며 프롬프트는 `uiRefreshPrompts.json`, `ui-v2-prompts.json`, `UIV2/*.json`에 남겨 두었다.

## 스킨 진행

사용자 확정 목표는 **기본 외형과 별도로 1–24단계 × 스킨 외형 4종 × SD/LD = 192개 리소스(96세트)**다.
현재 제작 목록은 `skins-96-plan.json`, 생성 원본 경로는 `Generated/*.json`에 있다. `skins-48-plan.json`은 이전 수량의 기록이며 설치 도구는 더 이상 읽지 않는다.
25단계 이후 LD 및 기존 연결은 변경하지 않는다.
생성됐다는 사실만으로 게임 적용/알파 정리/검수가 완료된 것으로 취급하지 않는다.
시인성 복구를 우선 진행한 뒤 남은 스킨 제작과 도감 밸런스를 완료해야 한다.

## 2026-08-28 패널 복구

`Tools → Girls Evolution → Repair Panel Layouts` (`Ctrl+Alt+I`)는 상점, 소환, 도감의 배치만 수정한다. 초기 하단 버튼과 수익 줄의 배치 및 저장 데이터는 변경하지 않는다.

- 상점 구매·강화·MAX 상태 Sprite 참조까지 초기 리소스로 복구했다. 현재 Image만 바꾸면 컨트롤러 Refresh가 폐기한 아틀라스로 되돌리는 문제가 있었다.
- 자동 소환/합성 컨트롤러는 상점 밖 하단 버튼에 있으므로 상점 구매 버튼 참조를 통해 찾아 갱신한다.
- 소환 버튼은 글자가 없는 기존 둥근 프레임에 기능명과 가격을 배치해 그림 안의 글자와 중첩되지 않도록 했다.
- 목록은 위에서 아래로 늘어나며 RectMask2D로 잘린다. 도감 상세는 탭 위에 표시하고 기존 중복 텍스트/닫기 버튼을 숨긴다.
- 미해금 카드와 상세 강제 호출을 모두 차단한다. 도감을 닫으면 상세도 닫힌다.
- 피버 표시를 0.1초 주기에서 매 프레임 LateUpdate로 분리했다. 충전 조건이나 수익 계산은 바꾸지 않았다.

검증은 Unity 편집 모드의 화면 미리보기와 분리된 PreviewScene 회귀검사다. 실제 광고/결제와 기기 플레이 검증을 대신하지 않는다. 최신 검사 결과는 `Logs/progression-checks.txt`에 기록된다.

### 준비된 스킨의 등록

`Tools → Girls Evolution → UI and Collection → Install Prepared Skins`는 SD/LD 파일이 모두 준비되고 제작 목록의 `qualityStatus`가 `approved`인 신규 항목만 등록한다. 기존 등록 스킨은 재검수 중이어도 삭제하지 않는다. 기존 4종의 ID, 조건, +5% 효과는 보존한다. 신규 항목은 단계 발견과 누적 활동 조건을 함께 요구하며, 네 종류 수집 효과를 +2%씩 나눈다. 장착으로 수집 효과가 중복 증가하지 않는다.

최신 등록/누락 목록: `skin-install-status.txt`. 현재 등록은 64세트·128장으로, 32세트·64장이 추가로 필요하다. 생성 원본만 있는 항목이나 미등록 PNG를 완료로 세지 않는다. 기존 20세트는 별도 재검수 대상이며 등록 수량을 최종 품질 승인 수량으로 간주하지 않는다.

### 2026-08-28 첫 스킨 보완 묶음

신규 등록 4세트: 1단계 `lv01-c` 대나무 수련, `lv01-d` 새벽 수행, 9단계 `lv09-b` 홍련 봉인, 10단계 `lv10-a` 달빛 항해. 1단계는 이제 기본 외형 외에 스킨 4종의 SD/LD가 모두 있다. 생성 프롬프트의 대숲 수련은 대나무 수련의 제작 가칭이다.

1단계 2종은 이번에 내장 imagegen으로 SD/LD를 각각 새로 생성했다. 9·10단계 2종은 기존 미등록 생성본을 검수·정리했다. 각 단계의 원본 SD/LD를 비교했고, 생성된 체크무늬·흰 배경은 사용자 승인에 따라 로컬에서 알파만 정리한 뒤 신규 스프라이트 캔버스에 맞췄다. 기존 캐릭터 이미지를 새 스킨으로 덮어쓰지 않았다.

- 전체 입력 프롬프트·원본 경로·검수 상태: `skin-batch-01.json`
- 게임 적용 PNG: `Assets/Art/Characters/Skins/{id}-SD.png`, `{id}-LD.png`
- 보관 원본/검수본: `SkinBatch01/Source`, `SkinBatch01/Ready`
- SD/LD 비교: `SkinBatch01/review-sheet.jpg`
- 픽셀·원화·메타데이터·세이브 검사: `SkinBatch01/verification.json`
- 이번에 수정한 기존 LD 10장: `Docs/ArtReview/AlphaRepair/README.md`

신규 파일 준비는 `prepare_skin_batch.py`에서 별도로 진행하고, 개별 시각 검수 후에만 `--apply`한다. 과거 `prepare_skins.py`는 기존 파일을 일괄 덮어쓰므로 이번 작업에 사용하지 않았다. Unity 등록은 `Install Prepared Skins` (`Ctrl+Alt+G`), 회귀 검사는 `Run Progression Checks` (`Ctrl+Alt+T`)다. Play Mode나 실제 사용자 세이브를 사용하지 않는다.

첫 묶음 검증 결과: Unity 편집 모드 회귀 검사 **574개 통과**, 픽셀·보존 검사 **20개 통과**. 320×568 Game View에서 1단계 스킨 4개가 순서대로 표시되고 미해금 카드가 어둡게 표시되는 것을 확인했다. 한 단계의 스킨 4개를 차례로 장착하고 저장/로드해도 해당 SD/LD와 소유권·수집 효과가 유지되는 검사도 포함했다. 실제 기기 플레이나 광고/결제 실행 검증은 하지 않았다.

### 2026-08-28 두 번째 스킨 보완 묶음

2단계 `lv02-c` 수묵 서찰, `lv02-d` 복숭아꽃 소풍과 3단계 `lv03-c` 먹빛 비전, `lv03-d` 금빛 봉인을 추가했다. 총 4세트·8장이며, 1·2·3단계에 각각 스킨 4종의 SD/LD가 등록되어 있다.

각 단계의 기존 SD/LD를 참고하여 내장 imagegen으로 의상을 생성했다. 3단계 LD 초안의 중복 두루마리통은 생성 도구로 수정하여 SD와 동일하게 한 개만 남겼다. 투명화는 사용자 승인 범위에서 Python으로 알파만 처리했다. 흰 소매·허리띠·눈 하이라이트는 보존하고, 확인된 배경 틈과 작은 잔여물만 제거했다. LD 가장자리의 옅은 테두리는 원본 해상도에서 0.8px 알파 페이드로 완화했다. 캔버스 정렬 전 RGB는 생성 원본과 동일하며, 기존 캐릭터 파일을 덮어쓰지 않았다.

- 전체 생성·수정 프롬프트, 원본·미채택 초안 경로, 개별 승인: `skin-batch-02.json`
- 4종 SD/LD 비교: `SkinBatch02/review-sheet.jpg`
- 보관 원본·투명화·최종 파일: `SkinBatch02/Source`, `Cutouts`, `Ready`
- 신규 PNG: `Assets/Art/Characters/Skins/lv02-c-*`, `lv02-d-*`, `lv03-c-*`, `lv03-d-*`
- 알파·기존 리소스·저장 데이터 보존 검사: `SkinBatch02/verification.json`

`prepare_skin_batch.py --batch 02`는 검수용 준비만 하며, 승인 후 `--apply`로 신규 파일만 복사한다. 기존 파일이 다른 해시이면 덮어쓰지 않고 중단한다. `verify_skin_batch.py --batch 02`는 신규 8장뿐 아니라 작업 전 저장한 기존 아트·메타·세이브 139개 해시도 대조한다. 첫 묶음 기본 동작은 `--batch 01`로 유지한다.

두 번째 묶음 검증: Unity 편집 모드 **604개 통과** (`skin-192-regression-checks.txt`, 20:53:56), Batch02 픽셀·보존 검사 **21개 통과**, Batch01 재검사 **20개 통과**. 2·3단계의 스킨 4개씩 장착·저장·불러오기, SD/LD 연결, 소유권과 수집 효과 유지, 잠금 조건의 카드 내 표시를 포함했다. Play Mode와 실제 사용자 세이브는 사용하지 않았고 기존 UI 배치도 변경하지 않았다. 실제 휴대폰 실행 검증은 별도로 필요하다.

### 2026-08-28 세 번째 스킨 보완 묶음

4단계 `lv04-c` 청록 그림자, `lv04-d` 자홍 매듭과 5단계 `lv05-c` 백매 호위, `lv05-d` 청풍 숲길을 추가했다. 신규 4세트·8장이며, 1–5단계에 각각 스킨 4종의 SD/LD가 등록되어 있다. 25단계 이후는 변경하지 않았다.

기존 SD·LD를 참고해 내장 imagegen으로 제작했다. 5단계 SD의 갈색/황갈색 눈은 해당 LD의 녹색 눈과 맞도록 생성 도구로 수정했다. 생성·수정 프롬프트와 미채택 초안 경로는 `skin-batch-03.json`에 보관했다. 색상 맞춤은 신규 스킨에만 적용했고 기존 얼굴을 재작화하지 않았다.

투명화는 사용자 승인에 따라 Python으로 알파만 처리했다. 실제 RGBA, 밝고 어두운 배경 합성, 포니테일·팔·칼집 사이의 빈틈을 확인했다. 백매 호위의 흰 망토와 칼날·눈 하이라이트는 불투명하게 보존했다. 원본 해상도에서 0.8px 가장자리 알파 페이드를 적용하고, 귀 뒤의 회백색 조각과 두 픽셀 잔여물을 개별 제거했다. 캔버스 정렬 전 RGB가 원본과 동일함을 검사했다.

기존 4·5단계 스킨 8장도 밝고 어두운 배경에서 확인했다. 이 중 `lv04-a-SD`, `lv05-a-SD/LD`, `lv05-b-SD/LD` 5장의 흰 잔여물을 추가 정리했다. 기존 5장의 RGB와 크기는 그대로이며 알파만 변경했다. `lv04-a-LD`, `lv04-b-SD/LD` 3장은 이번에 수정하지 않았다. 알파 검수는 기존 20세트 전체의 그림 품질 승인을 뜻하지 않는다.

- 신규 SD/LD 비교: `SkinBatch03/review-sheet.jpg`
- 기존 이미지 알파 수정 전후: `SkinBatch03/ExistingAlpha/alpha-review-sheet.jpg`
- 신규 원본·투명화·최종 파일: `SkinBatch03/Source`, `Cutouts`, `Ready`
- 신규 게임 PNG: `Assets/Art/Characters/Skins/lv04-c-*`, `lv04-d-*`, `lv05-c-*`, `lv05-d-*`
- 기존 5장 보정 설정: `existing-alpha-03.json`; 백업·마스크·결과는 `SkinBatch03/ExistingAlpha`
- 픽셀·내부 빈틈·보존 검사: `SkinBatch03/verification.json`

기존 보정 도구는 `repair_existing_skin_alpha.py --batch 03`로 준비하고, 검수 승인 후에만 `--apply`한다. 원본 해시가 달라지면 중단한다. 보존 검사는 이전 스냅샷을 덮어쓰지 않으며, 기록된 수정 전후 해시와 RGB 동일성·알파 감소를 검증한 변경만 허용한다. 작업 전 155개 파일 중 150개는 동일하고, 위 5개 PNG만 알파 수정 이력으로 검증한다. UI 배치·성장 수치·실제 사용자 세이브는 변경하지 않았다.

검증 결과: Unity 편집 모드 회귀 검사 **634개 통과** (`SkinBatch03/progression-checks.txt`, 21:28:37), 이번 묶음 픽셀·보존 검사 **34개 통과**, 이전 두 묶음 재검사 **25개·26개 통과**. 검사 수에는 새로 추가한 기존 5장 알파 수정 이력 검증이 포함된다. 4·5단계 스킨 장착·저장·불러오기, 도감 잠금 조건, SD/LD 연결, 수집 효과 중복 방지를 확인했다. `scene-preservation.json`은 씬 변경이 새 스킨 4개 정의와 수집 수량 표시뿐임을 확인한다. Play Mode·광고·결제·실제 기기 실행은 하지 않았다.

### 부자연스러운 보정본 거부

사용자가 지적한 `exec-1cc8f458-86be-4aaa-897c-9a5389a1b6fb.png`는 미적용 시험본이며 이후 제작 참고 이미지로도 사용하지 않는다. 얼굴·신체 비율·자세·선화가 바뀌는 배경 제거는 불합격이다. 흰 잔여물을 지우기 위해 전신을 재작화하지 않는다. 새 스킨은 각 단계의 기존 SD와 LD를 함께 비교하고, 동일한 의상·액세서리·무기를 유지하며 자연스러운 비율과 자세를 검수한 뒤 적용한다.

### 2026-08-28 네 번째 스킨 보완 묶음

6단계 홍련 검무·월백 검무, 7단계 단풍 걸음·남빛 순찰의 SD/LD 8장을 등록했다. 1–7단계는 기본 외형 외 스킨 네 종류가 모두 연결됐다. `skin-batch-04.json`에 내장 imagegen 프롬프트와 원본 경로 및 개별 알파 검수 지점을 기록했다. 검수 중 발가락 하이라이트를 건드리는 임시 처리본은 적용하지 않고 원본에서 다시 정리했다. 최종본은 배경과 연결된 그림자만 제거하며 발가락·검날·피부 불투명 검사를 통과했다.

`SkinBatch04/verification.json`: 픽셀·보존 검사 34개 통과, 기존 171개 원화·메타·세이브 파일 바이트 일치. `SkinBatch04/progression-checks.txt`: Unity 편집 모드 회귀 검사 664개 통과. `scene-preservation.json`: 신규 스킨 네 항목과 도감 수량 외 씬 내용 동일. 전체 192장 작업은 계속 진행 중이다.

### 2026-08-28 다섯 번째 스킨 보완 묶음

8단계 금빛 무도·홍백 꽃놀이, 9단계 설묵 서약·자운 기록 SD/LD 8장을 등록했다. 금빛 무도의 LD 초안에서 누락된 장갑과 다른 발목 끈을 생성 도구로 맞췄다. 흰 비단·꽃·겨울 의상과 책 하이라이트를 보존하며 배경 알파만 정리했다. `skin-batch-05.json`에 생성 및 수정 프롬프트를 기록했다.

`SkinBatch05/verification.json`: 픽셀·보존 검사 34개 통과, 기존 187개 파일 동일. `progression-checks.txt`: Unity 694개 통과. 씬 변경은 새 스킨 네 항목과 수집 수량뿐이다.

### 2026-08-28 여섯 번째 스킨 보완 묶음

기존 미등록 원화 8세트·16장을 검수하여 등록했다: 10단계 태양 수호, 11단계 청풍 무희·적야 전령, 12단계 설원의 꽃, 13단계 금빛 등불·유리 안개, 14단계 자수정 서약·청해 맹세. 이 시점의 총 등록은 48세트·96장이다.

두 캐릭터가 함께 있는 원본은 알파 마스크로 분리했다. 청해 맹세의 긴 LD 검이 중앙선을 넘어가는 부분은 빈 공간을 따라 마스크 경계를 정했고, 경계가 불투명 픽셀에 닿으면 중단하도록 검사했다. 머리카락·소매 사이의 흰 틈을 제거하고, 흰 의상·금속·피부는 지정 좌표에서 불투명함을 확인했다. 생성 당시 정확한 호출 프롬프트가 없는 과거 원화는 계획 프롬프트와 실제 호출 기록을 구분해 `skin-batch-06.json`에 명시했다.

`SkinBatch06/verification.json`: 픽셀·보존 검사 50개 통과. `progression-checks.txt`: Unity 734개 통과. `scene-preservation.json`: 신규 스킨 8개 정의와 도감 수량 이외 씬 내용 동일. 기존 원화·메타·세이브는 변경하지 않았다.

### 2026-08-28 일곱 번째 스킨 보완 묶음

10단계 비취 비술·자정 안개, 11단계 등꽃 밀행·청설 연무 SD/LD 8장을 새로 제작하여 등록했다. 등꽃 밀행 SD에 불필요하게 추가된 두 번째 봉은 생성 도구로 제거한 후 LD 기준으로 사용했다. 기본 외형의 얼굴·머리·소품과 각 SD/LD 의상 구조를 대조했다. 흰 머리끈 틈·손가락 사이·긴 머리 사이의 배경만 알파로 제거했고, 흰 스카프 무늬와 금속·눈·불꽃의 밝은 부분은 보존했다.

`skin-batch-07.json`에 전체 제작·수정 프롬프트, 채택/미채택 원본과 검수 좌표를 기록했다. `SkinBatch07/review-sheet.jpg`는 밝고 어두운 배경의 SD/LD 대조본이다. 픽셀·보존 검사 34개, Unity 편집 모드 회귀 검사 764개를 통과했다. 씬은 신규 스킨 네 정의와 수집 수량 외 동일하다. 총 등록은 52세트·104장이고, 1–11단계는 각각 스킨 네 종류가 모두 연결됐다.

### 2026-08-28 여덟 번째 스킨 보완 묶음

12단계 `lv12-c/d`, 13단계 `lv13-c/d` SD/LD 8장을 제작하여 등록했다. 실제 이름은 `skins-96-plan.json`의 고정 항목을 사용한다. 밝고 어두운 배경에서 머리카락·팔·검 사이 배경을 확인하고 알파만 정리했으며, 노란 머리 하이라이트·흰 의상·검날은 보존했다. 전체 프롬프트와 알파 검사 좌표는 `skin-batch-08.json`에 기록했다.

`SkinBatch08/verification.json`: 픽셀·보존 검사 34개 통과, 기존 255개 파일 보존. `progression-checks.txt`: Unity 794개 통과(23:46:06). 씬 변경은 신규 스킨 네 정의와 수집 수량뿐이다. 총 56세트·112장이 등록됐고 1–13단계에는 스킨 네 종류가 모두 연결됐다. 초기 20세트의 전체 재검수는 아직 진행 중이다.

### 2026-08-29 아홉 번째 스킨 보완 묶음

14단계 `lv14-c/d`, 15단계 `lv15-a/b` SD/LD 8장을 등록했다. `skin-batch-09.json`에 전체 프롬프트와 원본 및 알파 검수 좌표를 기록했다. 머리·팔·칼집 사이의 흰 배경을 제거하고 흰 옷과 금색 장식은 보존했다. 얇은 검 끝은 자연스러운 반투명 가장자리를 유지했다.

`SkinBatch09/review-sheet.jpg`에서 밝고 어두운 배경으로 대조했다. 픽셀·보존 검사 34개, Unity 편집 모드 819개 통과(00:00:14). 씬은 신규 스킨 네 정의와 수집 수량 외 동일하며, 기존 271개 아트·메타·세이브 파일도 보존됐다. 총 60세트·120장이 등록됐고 1–14단계에 스킨 네 종류가 연결됐다.

### 2026-08-29 열 번째 스킨 보완 묶음

15단계 `lv15-c/d`, 16단계 `lv16-a/b` SD/LD 8장을 등록했다. LD의 누락된 붉은 허리 장식과 SD의 불필요한 세 번째 칼을 생성 도구로 수정한 뒤 적용했다. 프롬프트와 미채택 원본은 `skin-batch-10.json`에 보관했다. 밝고 어두운 배경 비교는 `SkinBatch10/review-sheet.jpg`에 있다.

초기 스킨 중 `lv02-b-SD/LD`, `lv06-b-SD`, `lv08-a-SD/LD`, `lv08-b-SD/LD`, `lv09-a-SD` 8장의 확인된 배경 틈과 외곽 잔여물도 알파만 정리했다. 흰 망토·꽃 장식·머리 하이라이트·얼굴은 보존했다. `existing-alpha-10.json`과 `SkinBatch10/ExistingAlpha`에 백업, 수정 마스크, 검사 좌표, 변경 이력을 기록했다. 기존 그림의 전체 품질 승인과 알파 보정 승인은 구분한다.

픽셀·보존 검사 42개, Unity 편집 모드 회귀 검사 844개 통과(00:17:53). 작업 전 287개 파일 중 위 8개는 검증된 알파 수정 이력으로 확인하고 나머지는 동일하다. 씬 변경은 신규 스킨 네 정의와 도감 수량뿐이다. 총 64세트·128장 등록, 1–15단계 스킨 네 종류 연결 완료. 192장 전체 작업은 계속 진행 중이다.


### 2026-08-29 Batch11: 136/192 images registered

Registered lv16-c/d and lv17-b/c SD/LD after identity, outfit and negative-space review. Full prompts, reviewed hole IDs and foreground/background pixel probes are in skin-batch-11.json. Pixel/preservation checks: 42 PASS; Unity edit-mode regression: 869 PASS (00:39:10). Scene preservation check confirms only four skin records and collection count changed. All 303 pre-batch art/meta/save files are unchanged. Levels1-16 now each have four skin slots. Initial20 pairs still require final reinspection.


### 2026-08-29 Batch12: 146/192 images registered

Registered lv17-d and lv18-a/b/c/d SD/LD. Corrected lv17-d LD extra blade and unwanted backdrop; corrected lv18-a LD oversized sleeves to match SD. Rejected originals and full edit prompts remain in skin-batch-12.json and SkinBatch12/Source. Alpha inspection included hair/arm/cord gaps and closeups distinguishing silver collar highlights from white background. Pixel/preservation:46 PASS. Unity edit-mode:904 PASS (00:53:09). All319 pre-batch art/meta/save files unchanged; scene differs only in five skin definitions and collection count. Levels1-18 each have four skin slots. Initial20 pairs still await final overall reinspection.


### 2026-08-29 Batch13–16: 마지막 46장 등록

Batch13은 19단계 4종과 20단계 2종을 추가해 158장, Batch14는 20단계 나머지 2종과 21단계 4종을 추가해 170장, Batch15는 22단계 4종과 23단계 2종을 추가해 182장, Batch16은 23단계 나머지 1종과 24단계 4종을 추가해 최종 192장에 도달했다. 각 묶음의 `skin-batch-NN.json`에 전체 프롬프트, 채택 원본, 검수 좌표와 승인 사유를 기록했다. `SkinBatchNN/Source`, `Cutouts`, `Ready`에 원본부터 최종 PNG까지 보관한다.

각 단계의 기존 머리·눈·무기 정체성을 유지했다. 21단계 SD의 불필요한 검, 23단계 SD의 의상 구분, 누락된 의상 디테일은 생성 도구로 수정했다. 투명화는 알파만 처리했고, 특히 20·23단계 흰 머리와 24단계 흰 의상은 배경 후보에서 제외하여 보존했다. 머리·팔·망토·무기 사이의 작은 틈까지 원본 확대와 선택 마스크로 확인했다.

Unity 회귀 검사는 등록 순서대로 939, 979, 1014, 1049개 통과했다. 각 `scene-preservation.json`은 추가 스킨 정의와 도감 수량 이외 씬 내용이 동일함을 확인한다. 최종 보정 이력을 반영해 01–16 묶음을 전부 재검증했고 모두 통과했다. Batch13의 `review-sheet.jpg`도 마지막 알파 정리 결과로 갱신했다.

### 2026-08-29 Batch17–18: 초기 스킨 최종 보완

Batch17의 후드/검 위치 수정 2장과 Batch18의 알파 보완 15장은 기존 파일을 승인된 결과로 교체한 것이므로 192장 수량을 늘리지 않는다. 기존 PNG 백업과 수정 전후 해시는 각각 `SkinBatch17/ReplacedOriginals`, `SkinBatch18/ExistingAlpha/Originals` 및 보고서에 남겼다. `verify_skin_batch.py`는 이 승인된 수정 이력을 검증한 경우에만 보호 파일 해시의 변경을 허용한다.

초기 20세트의 전신 및 상세 확대 검수를 마쳤으며 현재 PNG 해시와 승인 결과를 `ExistingSkinReview/initial-review.json`에 기록했다. 검수 전 상세 이미지와 수정 후 이미지는 혼동하지 않도록 별도로 보관한다. 최신 전체 모습은 `FinalSkinReview/catalog-01.jpg`부터 `catalog-06.jpg`까지다.

### 2026-08-29 오프라인 보상창·도감/필드 표시 수정

오프라인 보상창의 일반 Transform 부모를 동일한 fileID의 RectTransform으로 보정하고, 전체 화면 차단막과 기존 보라/금색 둥근 패널 안에 제목·시간·금액·돈 아이콘·광고/일반 수령 버튼을 분리 배치했다. 버튼은 800×180 기준이며 광고 준비 상태와 쿠폰 제목도 텍스트로 표시한다. 보상 계산과 광고 지급 규칙은 변경하지 않았다.

도감 닌자/스킨 카드는 LD를 사용한다. 1–24단계 필드는 별도 SD 전용 조회를 사용하며, 파일명의 숫자가 아닌 캐릭터 단계로 장착 스킨을 찾는다. 발견 연출 도중 변경한 외형도 입력 잠금이 풀릴 때 SD로 적용한다. 기존 첫 발견 LD 연출과 25단계 이후 LD 전용 동작은 유지했다. 미해금 배지는 250×260으로 확대하고 원본 자물쇠 그림의 큰 여백은 UI 마스크로 제외했으며, ‘미해금’ 문구와 클릭 차단을 유지했다.

실제 필드에서 확인한 1단계 `lv01-a/b` SD는 기존 SD에 비해 몸이 길고 머리가 작았다. 앞선 검수에서 놓친 비율을 `SkinBatch19`에서 다시 그려 보정했다. 의상·머리·무기는 대응 LD에 맞췄으며 밝고 어두운 배경에서 투명화 결과를 확인했다. 전체 프롬프트/검수 좌표는 `skin-batch-19.json`, 교체 전 원본과 해시는 `SkinBatch19/replacements.json`에 보관했다. 이전 완료 보고서와 전체 대조표는 당시 기록이며, 이 두 SD의 최신 상태는 Batch19와 `RewardSkinFix/art-verification.json`이 우선한다. 총 192장은 유지되고 나머지 190장과 192개 메타 파일은 그대로다.

`RewardSkinFix/verification.json`: Unity 편집 모드 1,271개 통과. 실제 도감 컨트롤러의 96개 LD 카드, 실제 Image의 96개 대응 SD, 잠금 해제 전 클릭 차단, 일반 보상 단일 지급, 팝업 영역 겹침/범위/긴 시간 표시를 검사했다. 320×568 미리보기에서 팝업과 잠금 배지를 확인했다. 씬의 기존 블록 변경은 오프라인 팝업 내부에 한정되며 스킨 정의 96개와 해금 조건/혜택은 동일하다. 도감 미리보기로 계산된 임시 콘텐츠 높이도 복원했다.

작업 시작 시 사용자가 실행 중이던 Play 세션을 종료한 뒤 편집했다. 종료 시 정상 저장된 파일을 기준으로 이후 세이브 해시는 동일하며 세이브를 직접 수정하지 않았다. 검증 중 Play를 새로 시작하지 않았고 Android 실기기 및 실제 광고 재생은 확인하지 않았다.
