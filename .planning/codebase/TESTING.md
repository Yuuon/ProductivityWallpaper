# Testing Patterns

**Analysis Date:** 2026-03-14

## Test Framework Status

**Current State: NO AUTOMATED TESTS**

The codebase has no automated testing infrastructure. No test projects, test files, or testing frameworks were detected.

### Missing Testing Infrastructure
- No test project (`.csproj` with test references)
- No test files (`*.test.cs`, `*.spec.cs`, `*Tests.cs`)
- No test configuration files (`xunit.runner.json`, `testsettings.json`)
- No mocking frameworks (Moq, NSubstitute, FakeItEasy)
- No test runners configured in the solution

## Recommended Testing Framework

For future test implementation, the project should use:

### Primary Stack
- **Test Framework:** xUnit.net (standard for .NET)
- **Assertion Library:** FluentAssertions (readable assertions)
- **Mocking:** Moq or NSubstitute (for service mocking)
- **Test Runner:** `dotnet test` with Visual Studio Test Explorer

### Project Configuration
```xml
<!-- ProductivityWallpaper.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ProductivityWallpaper\ProductivityWallpaper.csproj" />
  </ItemGroup>
</Project>
```

## Areas Requiring Test Coverage

### High Priority (Critical Business Logic)

#### 1. ConfigService (`Services/ConfigService.cs`)
- JSON serialization/deserialization
- Default value handling
- File I/O error handling
- Config migration scenarios

```csharp
[Fact]
public void Load_ShouldCreateDefaultConfig_WhenFileDoesNotExist()
{
    // Arrange
    // Act
    // Assert
}
```

#### 2. MouseHookService (`Services/MouseHookService.cs`)
- Sweep speed calculation algorithm
- Position history management
- Cooldown logic
- Thread safety of `_lockObject`

#### 3. Win32Api Wrappers (`Services/Win32Api.cs`)
- Cannot unit test P/Invoke directly (system dependency)
- Create abstraction interface for mocking

### Medium Priority (ViewModel Logic)

#### 4. ViewModels
- `WallpaperViewModel`:
  - `CanUseAutoTheme` computed property
  - Monitor initialization
  - Command execution

- `CreatorViewModel`:
  - State transitions (`IsWelcomePage`, `IsCreatingPage`)
  - Feature selection logic

- `MainViewModel`:
  - Navigation commands

### Low Priority (UI and Integration)

#### 5. WallpaperService (`Services/WallpaperService.cs`)
**Note:** Heavy integration with VLC, Win32 APIs - requires:
- Abstract `IVideoPlayer` interface for mocking
- Abstract `IWin32Wrapper` for window operations
- Integration tests (not unit tests)

## Testing Patterns for MVVM

### ViewModel Test Pattern
```csharp
public class WallpaperViewModelTests
{
    private readonly Mock<WallpaperService> _wallpaperServiceMock;
    private readonly Mock<ConfigService> _configServiceMock;
    private readonly WallpaperViewModel _viewModel;

    public WallpaperViewModelTests()
    {
        _wallpaperServiceMock = new Mock<WallpaperService>();
        _configServiceMock = new Mock<ConfigService>();
        _viewModel = new WallpaperViewModel(
            _wallpaperServiceMock.Object,
            _configServiceMock.Object
        );
    }

    [Fact]
    public void CanUseAutoTheme_ShouldReturnFalse_WhenSelectedMediaIsVideo()
    {
        // Arrange
        _viewModel.SelectedMedia = new MediaItem { Type = MediaType.Video };

        // Act
        var result = _viewModel.CanUseAutoTheme;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanUseAutoTheme_ShouldReturnTrue_WhenSelectedMediaIsImage()
    {
        // Arrange
        _viewModel.SelectedMedia = new MediaItem { Type = MediaType.Image };

        // Act
        var result = _viewModel.CanUseAutoTheme;

        // Assert
        result.Should().BeTrue();
    }
}
```

### Async Test Pattern
```csharp
[Fact]
public async Task RefreshMetadataAsync_ShouldReturnItems_WhenFolderExists()
{
    // Arrange
    var folderPath = "C:\\Test\\Wallpapers";
    // Setup mock file system or use temporary directory

    // Act
    var result = await _service.RefreshMetadataAsync(folderPath);

    // Assert
    result.Should().NotBeNull();
}
```

## Manual Testing Procedures

### Interactive Wallpaper Testing
1. **Window Initialization Test:**
   - Apply interactive wallpaper
   - Verify no flicker during initialization
   - Confirm 1x1 pixel startup technique works

2. **Click Detection Test:**
   - Click each defined trigger zone
   - Verify correct action video plays
   - Verify audio plays (if configured)

3. **Hover Detection Test:**
   - Hover over trigger for configured delay (500ms default)
   - Verify bubble appears with correct text
   - Move mouse away - verify bubble disappears

4. **Sweep Detection Test:**
   - Move mouse rapidly across screen (>3000 px/s)
   - Verify sweep video triggers
   - Verify cooldown prevents double-trigger

5. **Layer Order Test:**
   - Apply wallpaper
   - Verify UI layer stays on top
   - Verify desktop icons remain clickable
   - Verify action video appears between idle and UI

### Video Playback Testing
1. **Format Support:**
   - Test MP4 files
   - Test WebM files
   - Test MKV files

2. **Multi-Monitor:**
   - Test on primary monitor
   - Test on secondary monitor
   - Test monitor switching

3. **Lifecycle:**
   - Apply wallpaper
   - Change to different wallpaper
   - Verify cleanup (no ghost windows in WorkerW)

### Configuration Testing
1. **First Launch:**
   - Delete `user_config.json`
   - Launch app
   - Verify defaults applied
   - Verify file recreated

2. **Path Persistence:**
   - Select library folder
   - Close app
   - Relaunch
   - Verify folder remembered

## Areas Lacking Tests (Risk Assessment)

### Critical Risk (No Tests + Complex Logic)
| Component | Lines | Risk | Why Untestable |
|-----------|-------|------|----------------|
| `WallpaperService.cs` | 654 | HIGH | Heavy P/Invoke, VLC integration |
| `MouseHookService.cs` | 188 | HIGH | Global hooks, threading |

### Medium Risk (No Tests + Business Logic)
| Component | Lines | Risk | Gap |
|-----------|-------|------|-----|
| `InteractiveUiWindow.xaml.cs` | 306 | MEDIUM | UI code, event handling |
| `VideoPlayerWindow.xaml.cs` | 42 | MEDIUM | VLC lifecycle |
| `ConfigService.cs` | 66 | MEDIUM | File I/O, serialization |

### Lower Risk (No Tests + Simple Logic)
| Component | Lines | Risk |
|-----------|-------|------|
| ViewModels | 85-170 each | LOW |
| Converters | ~28 each | LOW |
| Models | ~50 | LOW |

## Test Coverage Goals

### Phase 1: Unit Test Core Logic
Target: `Services/` folder (excluding Win32/P/Invoke)
- `ConfigService` - 100% coverage
- `MouseHookService.CalculateSweepSpeed()` - 100% coverage
- Helper methods in `WallpaperService` - 80% coverage

### Phase 2: ViewModel Tests
Target: `ViewModels/` folder
- Property change notifications
- Command execution
- Computed properties

### Phase 3: Integration Tests
- End-to-end wallpaper application flow
- Configuration save/load roundtrip
- Service lifecycle (dispose patterns)

## Testing Challenges

### 1. WPF UI Testing
**Problem:** WPF requires STA thread, difficult to automate
**Solutions:**
- Use `AppDomain` with STA thread for tests
- Consider `Microsoft.TestFx` or `xunit.stafact`
- Focus on ViewModel testing, minimize UI testing

### 2. Win32 API Dependencies
**Problem:** Cannot unit test P/Invoke calls
**Solution:** Create abstraction layer:
```csharp
public interface IWindowManager
{
    IntPtr FindWorkerW();
    void SetParent(IntPtr child, IntPtr parent);
    // ... etc
}
```

### 3. VLC Dependencies
**Problem:** LibVLC requires native libraries
**Solution:** Abstract video playback:
```csharp
public interface IVideoPlayer : IDisposable
{
    void Play(string path);
    void Stop();
    bool IsPlaying { get; }
}
```

### 4. Global Mouse Hooks
**Problem:** System-wide hooks affect all tests
**Solution:** Extract interface, mock in tests:
```csharp
public interface IMouseHook
{
    event Action<Point> OnMouseClick;
    void Start();
    void Stop();
}
```

## CI/CD Integration

When tests are added, include in build:
```yaml
# .github/workflows/build.yml
- name: Test
  run: dotnet test --verbosity normal
```

---

*Testing analysis: 2026-03-14 - NO AUTOMATED TESTS CURRENTLY EXIST*
