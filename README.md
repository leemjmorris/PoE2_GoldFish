# PoE2 GoldFish

Path of Exile 2 in-game overlay tool built with WPF (.NET 8.0).

## Features

### Memo (F2)
- In-game notepad overlay
- Auto-save every 5 seconds
- Persists across sessions

### Screenshot Viewer (F3)
- Watch a folder for screenshots (png/jpg/jpeg/bmp)
- Thumbnail gallery with mouse-hover full-size preview
- Click-through preview window (no focus steal)

## Tech Stack
- **Framework**: WPF, .NET 8.0
- **Language**: C#
- **Keyboard Hook**: `SetWindowsHookEx(WH_KEYBOARD_LL)` — OS-level global hook
- **Overlay**: `Topmost=True`, `ShowActivated=False` — renders on top without stealing game focus
- **Admin**: `requireAdministrator` manifest for PoE2 compatibility

## How to Build
```bash
dotnet build
```

## How to Run
```bash
dotnet run --project PoE2Overlay
```
Or run `PoE2Overlay.exe` from `bin/Debug/net8.0-windows/`.
UAC prompt will appear (admin required for game overlay).

## Usage
1. Launch the app — system tray icon appears
2. **F2**: Toggle memo overlay
3. **F3**: Toggle screenshot viewer
4. Right-click tray icon for menu (Memo / Screenshot / Exit)

## Project Structure
```
PoE2Overlay/
├── Core/
│   ├── AppSettings.cs          # Singleton settings (JSON persistence)
│   ├── GameFocusManager.cs     # Focus capture/restore for game window
│   ├── HotkeyManager.cs        # WH_KEYBOARD_LL global keyboard hook
│   ├── OverlayBase.cs          # Abstract base: drag, resize, topmost
│   └── OverlayHelper.cs        # Win32 SetWindowPos topmost assertion
├── Features/
│   ├── Memo/                   # Notepad overlay + auto-save
│   └── Screenshot/             # Thumbnail gallery + preview
├── Resources/Styles.xaml       # Dark theme styles
├── MainWindow.xaml             # System tray host
└── app.manifest                # Admin privilege requirement
```

## License
MIT
