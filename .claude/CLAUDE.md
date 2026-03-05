# PoE2 GoldFish Overlay

## 프로젝트 개요
Path of Exile 2용 WPF 오버레이 앱 (C#, .NET 8, Windows)

## 구조
```
PoE2Overlay/
├── Core/
│   ├── AppSettings.cs        # 앱 설정 (System.Text.Json 저장/로드)
│   ├── GameFocusManager.cs   # 게임 포커스 감지/복원
│   ├── HotkeyManager.cs      # 전역 단축키 (WH_KEYBOARD_LL)
│   ├── OverlayBase.cs        # 드래그/리사이즈/Topmost 공통 기반
│   └── OverlayHelper.cs      # Win32 SetWindowPos Z-order 유지
├── Features/
│   ├── Memo/                 # 메모 오버레이 기능
│   └── Screenshot/           # 스크린샷 기능
├── MainWindow.xaml.cs        # 메인 트레이 아이콘 앱
└── PoE2Overlay.csproj        # .NET 8, WPF
```

## 기술 스택
- C# / .NET 8 / WPF
- 트레이 아이콘 기반 오버레이
- 전역 핫키로 오버레이 토글

## 주요 기능
1. **Memo**: 게임 중 메모 작성/조회 오버레이
2. **Screenshot**: 스크린샷 폴더 감시, 썸네일 갤러리, 호버 미리보기

## 개발 환경
- Visual Studio / Windows 11
- 빌드: `dotnet build` 또는 VS에서 F5
