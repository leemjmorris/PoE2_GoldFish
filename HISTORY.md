# PoE2 GoldFish — 개발 히스토리

## v0.6 — 2026-03-05
**패시브 스킬 트리 오버레이 구현 (PoB2 빌드 코드 디코딩 + WPF Canvas 시각화)**

### 새 기능
| 항목 | 내용 |
|------|------|
| 패시브 트리 오버레이 | `F4` 핫키 / 트레이 메뉴 "Passive Tree (F4)" |
| PoB2 빌드 코드 디코딩 | pobb.in URL 또는 PoB2 직접 코드 → base64url decode → zlib inflate → XML 파싱 → 노드 ID 목록 |
| 트리 전체 렌더링 | WPF Canvas에 노드 (Ellipse) + 연결선 (Line) 렌더링 |
| 줌 & 패닝 | 마우스 휠 줌, 드래그 패닝 (MatrixTransform) |
| 할당 노드 하이라이팅 | PoB2 빌드 로드 시 할당된 노드 금색으로 표시 |
| 노드 타입 색상 구분 | 키스톤(주황), 노터블(노랑), 마스터리(파랑), 클래스시작(빨강), 일반(회색) |

### 신규 파일
| 파일 | 역할 |
|------|------|
| `Features/PassiveTree/Services/PobDecoder.cs` | PoB share URL/코드 디코딩 서비스 |
| `Features/PassiveTree/Services/PassiveTreeService.cs` | tree.json EmbeddedResource 로드 및 노드 위치 계산 |
| `Features/PassiveTree/PassiveTreeOverlay.xaml(.cs)` | 오버레이 UI + 렌더링 로직 |
| `Resources/PassiveTree/tree.json` | PathOfBuilding-PoE2 v0.4 트리 데이터 (EmbeddedResource) |

### 수정 파일
- `AppSettings.cs` — PassiveTree 창 위치/크기 필드 추가
- `MainWindow.xaml` — "Passive Tree (F4)" 트레이 메뉴 항목 추가
- `MainWindow.xaml.cs` — F4 핫키 등록, `_passiveTreeOverlay` 관리
- `PoE2Overlay.csproj` — `tree.json` EmbeddedResource 항목 추가

### 데이터 소스
- `tree.json`: [PathOfBuilding-PoE2](https://github.com/PathOfBuildingCommunity/PathOfBuilding-PoE2) `src/TreeData/0_4/tree.json` (GGG 공식 포맷, nodes/groups/constants)

---

## v0.5 — 2026-03-05
**오픈소스 심층 분석 후 정리 (Trade 제거 확정, 코드베이스 정리)**

### 변경 내용
| 항목 | 변경 사항 |
|------|-----------|
| Trade 제거 확정 | 기능 방향 결정에 따라 Trade 관련 잔재 전면 정리 |
| GameLanguage.cs 삭제 | Trade 전용 다국어 파싱 코드 — 데드 코드 제거 |
| Styles.xaml 정리 | 미사용 스타일 4개 제거 (OverlayCheckBox, OverlayTextInput, OverlayTabControl, OverlayTabItem) |
| 패키지 정리 | `Newtonsoft.Json` → `System.Text.Json` 교체, `Microsoft.Extensions.Logging` / `Serilog.Extensions.Logging` 제거 |
| 버그 수정 | `ScreenshotOverlay.Toggle()` — 토글 시 프리뷰 창 숨김 누락 수정 |
| 버그 수정 | `ScreenshotService.OnFileCreated()` — FileSystemWatcher 경쟁 조건 (Task.Delay + InvokeAsync) |
| 문서 전면 업데이트 | README, PROJECT.md, BACKLOG.md, HISTORY.md, CLAUDE.md — Trade 참조 전부 제거 |

---

## v0.4 — 2026-03-04
**Trade 기능 제거 및 아키텍처 개선**

### 변경 내용
- `Features/Trade/` 전체 제거
- `Core/OverlayBase.cs` 추가 — 드래그/리사이즈/Topmost 공통 기반 추상화
- `Core/GameFocusManager.cs` 추가 — 포커스 캡처/복원
- `Core/OverlayHelper.cs` — Z-order Win32 헬퍼
- DI 컨테이너 유지 (`Microsoft.Extensions.DependencyInjection`)
- Serilog 파일 로깅 추가

---

## v0.3 — 2026-03-04 (오후)
**오픈소스 비교 분석 기반 개선 (poe_overlay, Sidekick 참고)**

### 개선 내용
| 항목 | 변경 사항 |
|------|-----------|
| Z-order 강화 | 신규 `Core/OverlayHelper.cs` — Win32 `SetWindowPos(HWND_TOPMOST)` |
| DI 도입 | `Microsoft.Extensions.DependencyInjection` |
| 아이템 클래스 확장 | `Flask`, `Rune`, `Waystone`, `Tablet` 파싱 지원 |

---

## v0.2 — 2026-03-04 (새벽)
**버그 수정 및 Trade 안정성 개선**

### 수정 내용
- `HotkeyManager.HookCallback`에 try-catch 추가
- `MainWindow`: 구식 `keybd_event` → `SendInput` API로 교체

---

## v0.1 — 2026-03-04 (초기)
**초기 구현 (2개 기능 완성)**

### 구현된 기능
| 기능 | 핫키 | 설명 |
|------|------|------|
| 메모장 오버레이 | `F2` | 인게임 메모 작성, 자동 저장(5초), 영속 복원 |
| 스크린샷 뷰어 | `F3` | 지정 디렉토리 감시, 썸네일 목록, 마우스 호버 시 확대 |

### 핵심 아키텍처
- `WH_KEYBOARD_LL` 기반 전역 키보드 훅 (`HotkeyManager`)
- `ShowActivated=False` 오버레이 — 게임 포커스 탈취 없음
- 관리자 권한 매니페스트 (PoE2 호환)
- WPF 스타일 리소스 (`Resources/Styles.xaml`) — 다크 테마
- JSON 설정 영속화 (`AppSettings`)

---

## 참고 자료
- [Sidekick](https://github.com/Sidekick-Poe/Sidekick) — WPF 오버레이 구조, Z-order
- [poe_overlay (XileHUD)](https://github.com/XileHUD/poe_overlay) — client.txt 모니터링, 패시브 트리

## 다음 목표
- **client.txt 모니터링**: 존 진입 이벤트 감지, 스크린샷 자동 분류
- **UX 백로그**: Settings UI, Escape 닫기, 썸네일 클릭 OS 뷰어 열기
