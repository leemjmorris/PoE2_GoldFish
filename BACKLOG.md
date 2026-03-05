# PoE2 GoldFish — 개선 백로그

> 코드 리뷰에서 도출된 개선 항목들. 중요도 순으로 정렬.

---

## 🟡 UX/기능

### 1. Settings UI 미구현
**파일:** `MainWindow.xaml.cs` — `OnSettingsClick()`

현재 "Settings - coming soon!" MessageBox만 표시됨.
스크린샷 디렉토리 선택, 오버레이 투명도 등 설정 화면 구현 필요.

---

### 2. Escape 키로 오버레이 닫기 미지원
**파일:** `OverlayBase.cs`

현재 X 버튼 또는 핫키 토글로만 닫을 수 있음.
게임 중 Escape로 빠르게 닫는 기능 추가 권장.

---

### 3. 썸네일 클릭으로 기본 뷰어 열기 없음
**파일:** `Features/Screenshot/ScreenshotOverlay.xaml(.cs)`

클릭 시 OS 기본 이미지 뷰어로 파일 열기:
```csharp
Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
```

---

## 🔵 장기 / 참조 오픈소스 벤치마킹

### 4. client.txt 모니터링
XileHUD 방식 — Client.txt 파싱으로 존 진입 이벤트 감지.
스크린샷 자동 분류(존/액트별) 또는 레벨링 가이드 연동 기반.

### 5. ~~패시브 스킬 트리 오버레이~~ ✅ v0.6 완료
- PoB2 공유 URL / 빌드 코드 입력 → 노드 디코딩 (PobDecoder)
- WPF Canvas 전체 트리 렌더링, 줌/패닝 지원
- 할당 노드 금색 하이라이팅

---

## 구현 완료 이력

| 버전 | 주요 내용 |
|------|-----------|
| v0.1 | 메모 오버레이 (WPF 인프라) |
| v0.2 | 스크린샷 오버레이 |
| v0.3 | Z-order, DI, OverlayBase 추상화, GameFocusManager |
| v0.4 | Trade 기능 제거, 코드베이스 정리 |
| v0.5 | 데드 코드 제거, 패키지 정리, 버그 수정 (FileSystemWatcher, Toggle) |
| v0.6 | 패시브 스킬 트리 오버레이 (PoB2 디코딩, WPF Canvas 렌더링, 줌/패닝, 하이라이팅) |
