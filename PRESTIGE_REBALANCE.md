# 환생 포인트 상점 리밸런싱 결과

## 목표
- 총 12개 항목 만렙 비용: **50만 포인트**
- 골드 강화와 환생 업그레이드를 함께 고려
- 첫 환생까지의 플레이타임 고려
- 환생 강화 없이 골드 풀강화 기준으로 인구수 30 달성

## 변경 사항

### 1. 골드 강화 캡 조정
- **fieldMaxUpgradeCap**: 10 → **12**
- 인구수 계산: 8 (기본) + 12 * 2 = **32** (목표 30 달성)

### 2. 환생 업그레이드 레벨 캡
- **만렙**: 50 → **25**
- Unity Inspector에서 `PrestigeShopPanelController`의 각 `EntryUI.levelCap`을 **25**로 설정 필요

### 3. 환생 업그레이드 비용 곡선 조정

#### Core 항목 (4개)
- **Income**: base=70, grow=1.2
- **TwoStep**: base=70, grow=1.2
- **StartGold**: base=70, grow=1.2
- **PrestigeGain**: base=70, grow=1.2

#### Plus 항목 (8개)
- **PlusManualSpawnMax**: base=70, grow=1.1
- **PlusManualSpawnSpeed**: base=70, grow=1.1
- **PlusAutoMergeSpeed**: base=70, grow=1.2
- **PlusAutoSpawnSpeed**: base=70, grow=1.2
- **PlusFieldMax**: base=70, grow=1.1
- **PlusClickBonus**: base=70, grow=1.1
- **PlusOfflineReward**: base=70, grow=1.1
- **PlusOfflineMaxTime**: base=70, grow=1.1

## 예상 비용 (만렙 25 기준)
- Core 항목 (base=70, grow=1.2): 각 약 33,000 포인트
- Plus 항목 (base=70, grow=1.1): 각 약 7,000 포인트
- Plus 항목 (base=70, grow=1.2): 각 약 33,000 포인트
- **총 예상 비용**: 약 50만 포인트

## Unity Inspector 설정 필요
1. `PrestigeShopPanelController` 컴포넌트 선택
2. `entries` 리스트의 각 항목에서 `levelCap`을 **25**로 설정
3. 모든 12개 항목에 적용

## 검증
게임 실행 후 환생 포인트 상점에서 각 항목의 만렙 비용을 확인하여 총 50만 포인트에 근접하는지 확인 필요.
