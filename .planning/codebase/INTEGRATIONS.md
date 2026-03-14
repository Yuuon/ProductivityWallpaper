# External Integrations

**Analysis Date:** 2026-03-14

## APIs & External Services

**Media Processing:**
- **VideoLAN LibVLC** - Media playback engine
  - SDK: `LibVLCSharp` + `VideoLAN.LibVLC.Windows`
  - Used for: Video playback, audio playback, video duration parsing
  - Location: `Services/WallpaperService.cs` (line 5: `using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;`)

**No External Web APIs Detected:**
- No HTTP clients found
- No REST API integrations
- No cloud services

## Data Storage

**Databases:**
- None - Uses JSON file-based storage only

**File Storage:**
- Local filesystem for all data persistence
  - `user_config.json` - Application settings (`ConfigService.cs` line 14)
  - `config.wallpaper` - Interactive wallpaper configurations per folder (`WallpaperService.cs` line 26)
  - `.wp_meta.json` - Cached library metadata (`WallpaperService.cs` line 25)
  - `Resources/Languages/*.json` - Localization strings (`LocalizationService.cs` line 10)
  - `.wp_cache/` folder - Generated thumbnails

**Caching:**
- In-memory cache via `LibVLC` instances (not shared across restarts)
- Thumbnail cache in `.wp_cache/` subdirectories

## Authentication & Identity

**Auth Provider:**
- None - Local desktop application with no user authentication

## Platform-Specific Integrations

**Windows P/Invoke (Win32 API):**

All P/Invoke declarations centralized in `Services/Win32Api.cs` (146 lines):

**Window Management:**
- `user32.dll::FindWindow` - Find Progman/WorkerW windows
- `user32.dll::FindWindowEx` - Enumerate child windows
- `user32.dll::SetParent` - Inject windows into desktop layer
- `user32.dll::SetWindowPos` - Z-order control
- `user32.dll::GetWindowLong` / `SetWindowLong` - Window style modification
- `user32.dll::ShowWindow` - Window visibility control

**System Integration:**
- `user32.dll::SystemParametersInfo` - Set desktop wallpaper (SPI_SETDESKWALLPAPER)
- `user32.dll::SendMessageTimeout` - Communicate with Progman for WorkerW setup
- `user32.dll::EnumWindows` - Enumerate all windows

**Global Input Hooks:**
- `user32.dll::SetWindowsHookEx` - Install low-level mouse hook (WH_MOUSE_LL = 14)
- `user32.dll::UnhookWindowsHookEx` - Remove hook
- `user32.dll::CallNextHookEx` - Chain hooks
- `kernel32.dll::GetModuleHandle` - Get module handle for hook installation

**Shell Thumbnail API:**
- `shell32.dll::SHCreateItemFromParsingName` - Get IShellItem from file path
- `IShellItem` / `IShellItemImageFactory` - Windows Shell thumbnail generation

**Windows Registry:**
- `Microsoft.Win32.Registry` - AutoColorization settings (`WallpaperService.cs` line 450)

## Media Sources and Formats

**Supported Video Formats:**
- MP4 (MPEG-4)
- WebM
- MKV
- All formats supported by VLC 3.0.23

**Supported Image Formats:**
- JPG/JPEG
- PNG
- BMP

**Audio Support:**
- Any format supported by VLC (MP3, WAV, OGG, etc.)

**Thumbnail Generation:**
- Shell API for video thumbnails (`Win32Api.cs` line 97-144)
- System.Drawing for image thumbnails (`WallpaperService.cs` line 598-629)

## AI/ML Integrations

**Not Currently Implemented:**
- AI Studio view exists (`Views/AiToolView.xaml`) but is navigation-disabled
- No AI service integrations detected
- No ML model loading
- No inference engines

## Monitoring & Observability

**Error Tracking:**
- None - Uses `try/catch` with silent failure pattern
- Debug output via `System.Diagnostics.Debug.WriteLine`

**Logs:**
- Console/Debug output only
- No structured logging framework

## CI/CD & Deployment

**Hosting:**
- Desktop application (no server hosting)
- Single-file deployment possible via `dotnet publish`

**CI Pipeline:**
- None detected

## Environment Configuration

**Required Configuration:**
- VLC runtime (bundled via NuGet)
- Windows desktop composition enabled
- User must have permission to create global mouse hooks

**Optional Configuration:**
- Media library path (defaults to My Pictures folder)
- Language preference (auto-detected from system culture)

**No Secrets:**
- No API keys
- No connection strings
- No authentication tokens

---

*Integration audit: 2026-03-14*
