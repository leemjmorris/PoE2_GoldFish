# PoE2 GoldFish — 개발 히스토리

## v0.3 — 2026-03-04 (오후)
**오픈소스 비교 분석 기반 개선 (poe_overlay, Sidekick 참고)**

### 개선 내용
| 항목 | 변경 사항 |
|------|-----------|
| 클립보드 복원 | `Ctrl+D` 시세 조회 전 원본 클립보드 저장 → 조회 완료 후 200ms 뒤 복원 |
| Z-order 강화 | 신규 `Core/OverlayHelper.cs` — Win32 `SetWindowPos(HWND_TOPMOST)` 로 게임이 포커스를 가져가도 오버레이 Z-order 유지 |
| DI 도입 | `Microsoft.Extensions.DependencyInjection` — `StatIdResolver`, `TradeApiClient`를 싱글톤으로 컨테이너 관리 |
| 퍼지 매칭 | `StatIdResolver`: exact match 실패 시 Jaccard 유사도(임계값 0.6) fallback으로 stat ID 탐색 |
| 아이템 클래스 확장 | `Flask`, `Rune`, `Waystone`, `Tablet` 파싱 지원. `ClassifyByItemClass()` 추가 |

### 수정된 파일
- `PoE2Overlay.csproj` — DI 패키지 추가
- `App.xaml` / `App.xaml.cs` — DI 컨테이너 구성, `StartupUri` 제거
- `Core/OverlayHelper.cs` — **신규**
- `MainWindow.xaml.cs` — 클립보드 복원, DI 생성자
- `Features/Memo/MemoOverlay.xaml.cs` — `OnDeactivated` Z-order
- `Features/Screenshot/ScreenshotOverlay.xaml.cs` — `OnDeactivated` Z-order
- `Features/Trade/TradeOverlay.xaml.cs` — `OnDeactivated` Z-order, DI 생성자
- `Features/Trade/Services/StatIdResolver.cs` — 퍼지 매칭
- `Features/Trade/Services/ItemParser.cs` — 아이템 클래스 확장
- `Features/Trade/Models/ParsedItem.cs` — `ItemRarity` 확장

---

## v0.2 — 2026-03-04 (새벽)
**버그 수정 및 Trade 안정성 개선**

### 수정 내용
- `HotkeyManager.HookCallback`에 try-catch 추가 — 예외 발생 시 키보드 훅 체인 끊김 방지
- 음수 모드 값 처리 수정 (예: `-5% Fire Resistance` min/max 필터)
- 가격 중앙값 계산을 통화 종류별(Divine / Chaos 등)로 분리
- `StatIdResolver`: `SemaphoreSlim` 스레드 안전성 추가, `EnsureLoadedAsync()` 추가
- `ItemParser`: `double.Parse`에 `CultureInfo.InvariantCulture` 적용
- `MainWindow`: 구식 `keybd_event` → `SendInput` API로 교체
- Trade 오버레이 트리거: `ClipboardListener` 방식 → `Ctrl+D` 핫키 방식으로 변경 (Sidekick 방식)

---

## v0.1 — 2026-03-04 (초기)
**초기 구현 (3개 기능 완성)**

### 구현된 기능
| 기능 | 핫키 | 설명 |
|------|------|------|
| 메모장 오버레이 | `F2` | 인게임 메모 작성, 자동 저장(5초), 영속 복원 |
| 스크린샷 뷰어 | `F3` | 지정 디렉토리 감시, 썸네일 목록, 마우스 호버 시 확대 |
| 아이템 시세 조회 | `Ctrl+D` | 클립보드 자동 파싱 → PoE2 Trade API 검색 |

### 핵심 아키텍처
- `WH_KEYBOARD_LL` 기반 전역 키보드 훅 (`HotkeyManager`)
- `ShowActivated=False` 오버레이 — 게임 포커스 탈취 없음
- 관리자 권한 매니페스트 (PoE2 호환)
- WPF 스타일 리소스 (`Resources/Styles.xaml`) — 다크 테마
- JSON 설정 영속화 (`AppSettings`)

### 생성된 파일 (29개)
```
PoE2Overlay/
├── Core/
│   ├── AppSettings.cs
│   ├── ClipboardListener.cs
│   └── HotkeyManager.cs
├── Features/
│   ├── Memo/
│   │   ├── MemoOverlay.xaml(.cs)
│   │   └── MemoService.cs
│   ├── Screenshot/
│   │   ├── ImagePreviewWindow.xaml(.cs)
│   │   ├── ScreenshotOverlay.xaml(.cs)
│   │   └── ScreenshotService.cs
│   └── Trade/
│       ├── Models/
│       │   ├── ParsedItem.cs
│       │   ├── TradeSearchRequest.cs
│       │   └── TradeSearchResponse.cs
│       ├── Services/
│       │   ├── ItemParser.cs
│       │   ├── StatIdResolver.cs
│       │   └── TradeApiClient.cs
│       └── TradeOverlay.xaml(.cs)
├── Resources/
│   └── Styles.xaml
├── App.xaml(.cs)
├── MainWindow.xaml(.cs)
├── PoE2Overlay.csproj
└── app.manifest
```

---

## 참고 자료
- [Sidekick](https://github.com/Sidekick-Poe/Sidekick) — 아이템 시세, Ctrl+D 방식, WPF 구조
- [poe_overlay (XileHUD)](https://github.com/XileHUD/poe_overlay) — Z-order 관리, 오버레이 구조

## 다음 목표
- **4단계**: 패시브 스킬 트리 오버레이 (PoB2 URL 디코딩 + WPF Canvas 시각화)
