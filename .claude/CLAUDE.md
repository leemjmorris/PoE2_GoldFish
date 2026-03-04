# PoE2 GoldFish Overlay

## 프로젝트 개요
Path of Exile 2용 WPF 오버레이 앱 (C#, .NET 8, Windows)

## 구조
```
PoE2Overlay/
├── Core/
│   ├── AppSettings.cs        # 앱 설정 (JSON 저장/로드)
│   ├── ClipboardListener.cs  # 클립보드 감지
│   └── HotkeyManager.cs      # 전역 단축키 관리
├── Features/
│   ├── Memo/                 # 메모 오버레이 기능
│   ├── Screenshot/           # 스크린샷 기능
│   └── Trade/                # 거래소 검색 기능
│       ├── Models/           # ParsedItem, TradeSearchRequest/Response
│       └── Services/         # ItemParser, StatIdResolver, TradeApiClient
├── MainWindow.xaml.cs        # 메인 트레이 아이콘 앱
└── PoE2Overlay.csproj        # .NET 8, WPF
```

## 기술 스택
- C# / .NET 8 / WPF
- 트레이 아이콘 기반 오버레이
- PoE2 공식 Trade API 사용
- 전역 핫키로 오버레이 토글

## 주요 기능
1. **Memo**: 게임 중 메모 작성/조회 오버레이
2. **Screenshot**: 화면 캡처 및 미리보기
3. **Trade**: 아이템 클립보드 파싱 → 거래소 자동 검색

## 개발 환경
- Visual Studio / Windows 11
- 빌드: `dotnet build` 또는 VS에서 F5
