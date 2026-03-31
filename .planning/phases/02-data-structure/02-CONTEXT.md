# Phase 02: Data Structure - Context (Locked Decisions) - REVISED

**Gathered:** 2026-03-15  
**Revised:** 2026-03-15 - Reference by absolute path locally, copy only on export  
**Revised:** 2026-03-15 - **Manual save primary, auto-save as backup (5-min interval)**  
**Status:** Decisions Locked - Ready for Implementation  
**Scope:** Theme package persistence, import workflow, widget separation

---

<domain>
## Phase Boundary

Establish complete data persistence layer for ProductivityWallpaper. Connect Phase 1 UI with actual file operations: import resources by reference (absolute paths), save/load themes, export packaged themes with copied files, and clearly separate theme data from global widget settings.

**In-Scope:**
- Theme manifest model (theme.json) with UUID-based resource references
- Import workflow - **reference by absolute path** (no file copy during editing)
- Export theme package - **copy files** to theme folder with relative paths
- Thumbnail generation (stored in app temp, not theme folder)
- Save strategies (immediate vs delayed with debouncing)
- ThemeService for load/save/validate/export
- Clear boundary: Theme data vs Global widget settings

**Out-of-Scope:**
- Widget configuration (separate global config)
- Steam Workshop integration (Phase 3)
- Resource streaming or advanced optimizations
- Theme versioning/migration

**Key Principle:**
- **Local Editing Phase:** Theme references media files by **absolute path** (no copying)
- **Export Phase:** "Export Theme Package" creates complete folder with copied files and **relative paths**

</domain>

---

<decisions>
## Implementation Decisions

### 1. Two-Phase Resource Management (REVISED)

**Phase A: Local Editing (Reference Mode)**
- Media files stay in original locations
- Theme.json stores **absolute file paths**
- Thumbnails generated and stored in **app temp folder** (not theme folder)
- No file copying during normal editing
- Fast import - just register the reference

**Phase B: Export Package (Bundle Mode)**
- "Export Theme Package" command (manual action)
- Creates new folder: `{ExportLocation}/{ThemeName}/`
- **Copies all referenced files** into subfolders (/images/, /videos/, /audio/)
- Updates theme.json to use **relative paths** pointing to copied files
- Generates thumbnails and saves to /thumbnails/
- Creates complete, portable theme package

**Resource Entry Structure:**
```csharp
public class ResourceEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Hash { get; set; } = string.Empty;  // For deduplication
    public string Type { get; set; } = string.Empty;  // image, video, audio
    
    // LOCAL MODE (absolute path)
    public string SourcePath { get; set; } = string.Empty;  // C:\Users\...\photo.jpg
    
    // EXPORT MODE (relative path)
    public string? ExportPath { get; set; }  // images/photo.jpg (null until exported)
    
    public string OriginalName { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
```

### 2. Import Workflow (REVISED)

**Import Process (No File Copy):**
1. User selects file via OpenFileDialog
2. Compute fast hash (CRC32)
3. Check if hash already exists in current theme
   - Yes: Reference existing entry (skip duplicate)
   - No: Create new ResourceEntry with **absolute path**
4. Generate thumbnail → save to **app temp folder** (`%Temp%/ProductivityWallpaper/Thumbnails/`)
5. Add to theme's resource registry
6. **No file copy performed**

**Thumbnail Storage:**
- Local editing: `%Temp%/ProductivityWallpaper/Thumbnails/{UUID}_thumb.jpg`
- Export: Copy thumbnails to `/thumbnails/` folder in export

**Missing File Handling:**
- If referenced file is deleted/moved: Show warning icon in UI
- On theme load: Validate all paths, mark missing files
- User can "repair" by re-importing or updating path

### 3. Save Strategy (REVISED - Manual Primary, Auto Backup)

**Primary Save Method: Manual Save Button**

**UI Placement:**
- Location: Top of left navigator bar
- Position: Right side (back button is on left)
- Button: "Save" with icon (floppy disk or checkmark)
- State: Enabled when `IsDirty = true`, Disabled when `IsDirty = false`
- Visual feedback: Color change when enabled (use AccentBrush)

**Manual Save Behavior:**
- User clicks Save button → Immediate save to theme.json
- Shows brief success indicator (checkmark animation or toast)
- Sets `IsDirty = false`
- **This is the PRIMARY save method**

**Data Dirty Tracking:**
```csharp
public bool IsDirty { get; private set; }

// Mark dirty on any change
partial void OnPropertyChanged(string propertyName)
{
    if (IsTrackedProperty(propertyName))
    {
        IsDirty = true;
    }
}

// Tracked changes:
// - Import resource
// - Delete media reference
// - Rename scheme
// - Property changes (DisplayMode, PlaybackMode, etc.)
// - Order index changes
// - Resource metadata edits
```

**Leave-Page Confirmation Dialog:**
- Trigger: User clicks back button or navigates away from Creator page
- Condition: Only show if `IsDirty = true`
- Dialog style: Use theme colors (BackgroundBlockBrush, AccentBrush)
- Dialog content:
  - Title: "Unsaved Changes"
  - Message: "You have unsaved changes. Save before leaving?"
  - Buttons: ["Save"] ["Don't Save"] ["Cancel"]
- Actions:
  - "Save": Save theme.json, then navigate
  - "Don't Save": Discard changes, navigate
  - "Cancel": Stay on Creator page

**Auto-Save (Backup Only):**
- Frequency: Every 5 minutes
- Condition: Only runs if `IsDirty = true`
- Behavior: Silent save in background
- User notification: None (silent backup)
- Purpose: Recovery from crash/force quit
- Does NOT reset `IsDirty` flag (user still needs to manually save)

**Auto-Save Implementation:**
```csharp
private Timer _autoSaveTimer;

private void OnAutoSaveTimerElapsed(object? sender, ElapsedEventArgs e)
{
    if (IsDirty)
    {
        // Silent backup save
        SaveTheme(backup: true);
        // IsDirty remains true - user still needs to manual save
    }
}
```

**Save Location:**
- Theme config: `%AppData%/ProductivityWallpaper/Themes/{ThemeName}/theme.json`
- Backup auto-saves: `%AppData%/ProductivityWallpaper/Themes/{ThemeName}/theme.json.backup`
- References absolute paths to user's media files
- Lightweight - just JSON, no media copying

**Save vs Export Distinction:**
- **Save:** Updates JSON only, fast, manual button
- **Export:** Creates full package, copies files, slower, separate button
- User workflow: Edit → Save (manual) → Export when ready to share

### 4. Export Theme Package Function (NEW)

**Export Workflow:**
1. User clicks "Export Theme Package" button
2. Show folder browser dialog for export location
3. Create folder structure:
   ```
   /{ExportLocation}/{ThemeName}/
     theme.json
     /images/
     /videos/
     /audio/
     /thumbnails/
   ```
4. For each referenced resource:
   - Copy file from `SourcePath` to appropriate subfolder
   - Use original filename (sanitized)
   - Generate `ExportPath` (e.g., "images/photo.jpg")
5. Generate fresh thumbnails in `/thumbnails/`
6. Save theme.json with:
   - All `ExportPath` values populated
   - Optional: remove or keep `SourcePath` (keep for traceability)
7. Show success dialog: "Theme exported to {path}"

**Export vs Save Distinction:**
- **Save:** Updates JSON only, absolute paths, fast
- **Export:** Creates full package, copies files, relative paths, slower
- User workflow: Edit locally (fast) → Export when ready to share (complete package)

### 5. Hash Deduplication (REVISED)

**Purpose:** Prevent importing same file twice in same theme

**Process:**
1. Compute hash on import
2. Check against existing resources in theme
3. If match found: reference existing entry (don't add duplicate)
4. If new: add to registry

**Hash Storage:**
- Store hash in ResourceEntry.Hash
- Use CRC32 for speed: `System.IO.Hashing.Crc32`

**Note:** Hash is NOT used for file comparison across themes (local editing mode keeps original paths)

### 6. Widget Settings Boundary (UNCHANGED)

**Complete Separation Principle:**
- Theme packages contain ZERO widget settings
- Clock, Pomodoro, Anniversary are global user preferences
- Widgets have their own config file (separate from themes)

**Widget Config Location:**
- `widgets_config.json` in app data folder
- Completely independent save/load cycle
- Theme service never touches widget config

</decisions>

---

<code_context>
## Existing Code Analysis

### Reusable Assets

**ConfigService** (`Services/ConfigService.cs`):
- JSON persistence pattern
- Pattern: Load → Modify → Save
- Use for theme.json operations

**WallpaperService** (`Services/WallpaperService.cs`):
- `TryLoadMetadata` - library metadata loading
- Thumbnail generation logic
- Reuse for export thumbnail generation

**Existing ViewModels**:
- `DesktopBackgroundViewModel.ImportMedia()` - has file dialog, needs to change from copy to reference
- All ViewModels use `ObservableCollection<MediaItemModel>`

**Models to Modify:**
- `MediaItemModel` - add Hash, change from file management to reference management
- `SchemeModel` - resource ID references (already planned)
- Need new: `ThemeManifest`, `ResourceEntry`, `ThemeService`

### Integration Points

**File Path Resolution:**
```csharp
public string GetResolvedPath(ResourceEntry resource)
{
    // Check if in export mode (has ExportPath)
    if (!string.IsNullOrEmpty(resource.ExportPath))
    {
        // Export mode: relative to theme folder
        return Path.Combine(_themeFolder, resource.ExportPath);
    }
    
    // Local mode: absolute path
    return resource.SourcePath;
}
```

**Thumbnail Path Resolution:**
```csharp
public string GetThumbnailPath(string resourceId, bool isExport = false)
{
    if (isExport)
    {
        return Path.Combine(_exportFolder, "thumbnails", $"{resourceId}_thumb.jpg");
    }
    
    // Local: temp folder
    return Path.Combine(Path.GetTempPath(), "ProductivityWallpaper", "Thumbnails", $"{resourceId}_thumb.jpg");
}
```

</code_context>

---

<specifics>
## Specific Implementation Details

### Import Command (REVISED)
```csharp
[RelayCommand]
private void ImportMedia()
{
    var dialog = new OpenFileDialog { Multiselect = true, ... };
    if (dialog.ShowDialog() == true)
    {
        foreach (var filePath in dialog.FileNames)
        {
            // Check for duplicate (by hash)
            var hash = ComputeHash(filePath);
            if (_themeService.HasResource(hash))
            {
                // Reference existing
                var existing = _themeService.GetResourceByHash(hash);
                AddReferenceToScheme(existing.Id);
            }
            else
            {
                // Create new reference entry
                var resource = new ResourceEntry
                {
                    SourcePath = filePath,  // ABSOLUTE PATH
                    Hash = hash,
                    Type = GetMediaType(filePath),
                    OriginalName = Path.GetFileName(filePath)
                };
                
                // Generate thumbnail to TEMP
                resource.ThumbnailPath = GenerateThumbnailToTemp(filePath, resource.Id);
                
                _themeService.AddResource(resource);
                AddReferenceToScheme(resource.Id);
            }
        }
        
        // Save theme.json (lightweight - just metadata)
        _themeService.SaveTheme();
    }
}
```

### Export Command (NEW)
```csharp
[RelayCommand]
private async Task ExportThemePackage()
{
    var dialog = new FolderBrowserDialog();
    if (dialog.ShowDialog() == true)
    {
        var exportPath = Path.Combine(dialog.SelectedPath, ThemeName);
        
        // Show progress dialog
        var progress = new ProgressDialog();
        
        await Task.Run(() =>
        {
            // Create folder structure
            Directory.CreateDirectory(Path.Combine(exportPath, "images"));
            Directory.CreateDirectory(Path.Combine(exportPath, "videos"));
            Directory.CreateDirectory(Path.Combine(exportPath, "audio"));
            Directory.CreateDirectory(Path.Combine(exportPath, "thumbnails"));
            
            // Copy all referenced files
            foreach (var resource in _themeService.Resources)
            {
                var subfolder = resource.Type.ToLower() + "s";  // images, videos, audio
                var destFilename = SanitizeFilename(resource.OriginalName);
                var destPath = Path.Combine(exportPath, subfolder, destFilename);
                
                File.Copy(resource.SourcePath, destPath, overwrite: true);
                
                // Set ExportPath for theme.json
                resource.ExportPath = $"{subfolder}/{destFilename}";
                
                // Generate thumbnail in export folder
                var thumbPath = Path.Combine(exportPath, "thumbnails", $"{resource.Id}_thumb.jpg");
                GenerateThumbnail(resource.SourcePath, thumbPath);
                
                progress.Report($"Exported: {resource.OriginalName}");
            }
            
            // Save theme.json with relative paths
            _themeService.ExportTheme(exportPath);
        });
        
        progress.Close();
        MessageBox.Show($"Theme exported to: {exportPath}");
    }
}
```

### Hash Calculation (Fast)
```csharp
public static string ComputeFastHash(string filePath)
{
    using var stream = File.OpenRead(filePath);
    var hash = System.IO.Hashing.Crc32.HashToUInt32(stream);
    return $"crc32:{hash:X8}";
}
```

### Missing File Detection
```csharp
public void ValidateTheme()
{
    foreach (var resource in Resources)
    {
        if (!File.Exists(resource.SourcePath))
        {
            resource.Status = ResourceStatus.Missing;
            resource.StatusMessage = $"File not found: {resource.SourcePath}";
        }
    }
}
```

</specifics>

---

<deferred>
## Deferred to Phase 3+

- **Async Import:** Currently sync (fast enough for references), async queue in future
- **Auto-Repair Missing Files:** Prompt user to locate moved files
- **Resource Deduplication Across Themes:** Global hash registry
- **Incremental Export:** Only export changed files
- **Export Compression:** ZIP export option
- **Theme Dependencies:** One theme extending another
- **Steam Workshop Integration:** Upload/download mechanics

</deferred>

---

<implementation_checklist>
## Implementation Checklist

### New Models
- [ ] `ThemeManifest` - root theme.json structure
- [ ] `ResourceEntry` - with SourcePath (absolute) and ExportPath (relative)
- [ ] `ThemeResourceLibrary` - collection + lookup by ID and Hash

### Modified Models
- [ ] `MediaItemModel` - transition to reference-based (not file management)
- [ ] `SchemeModel` - resource ID references
- [ ] `CreatorViewModel` - Add `IsDirty` tracking and property change monitoring
  - Implement dirty tracking for all theme modifications
  - Reset dirty flag on manual save
  - Do NOT reset on auto-save (backup only)

### New Services
- [ ] `ThemeService` - LoadTheme(), SaveTheme(), ValidateTheme(), ExportTheme()
- [ ] `ThumbnailService` - Generate to temp (local) or theme folder (export)
- [ ] `ImportService` - Add reference (not copy), hash deduplication
- [ ] `AutoSaveTimer` - 5-minute interval, backup save if dirty

### UI Updates
- [ ] **Save Button** - Top of left navigator bar (right side)
  - Enabled/disabled based on `IsDirty` state
  - Visual feedback using AccentBrush when enabled
  - Click triggers immediate save
- [ ] **Data Dirty Tracking** - Monitor all theme changes
  - Track property changes, imports, deletes, renames
  - Update `IsDirty` flag
  - Enable/disable save button accordingly
- [ ] **Leave-Page Confirmation Dialog** - Styled with theme colors
  - Show when `IsDirty = true` and user tries to navigate away
  - Buttons: [Save] [Don't Save] [Cancel]
  - Use BackgroundBlockBrush, AccentBrush for styling
- [ ] Import dialog - fast, no progress needed (just reference)
- [ ] Export Theme Package button + folder browser
- [ ] Export Progress Dialog with file copy progress
- [ ] Missing file warning indicators in UI
- [ ] "Repair" option for missing files

### File Operations
- [ ] Theme folder creation (config only, lightweight)
- [ ] Export folder creation with subfolders
- [ ] File copy on export with progress reporting
- [ ] Thumbnail generation (temp for local, export folder for bundle)
- [ ] Path resolution logic (absolute vs relative)

</implementation_checklist>

---

*Phase: 02-data-structure*  
*Context created: 2026-03-15*  
**REVISED: 2026-03-15 - Reference by absolute path locally, copy only on export**  
**REVISED: 2026-03-15 - Manual save primary with dirty tracking, auto-save as backup (5-min interval)**  
*Decisions locked and ready for implementation*
