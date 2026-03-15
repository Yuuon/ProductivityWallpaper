# Phase 02: Data Structure - Context

**Gathered:** 2026-03-15  
**Status:** Ready for planning

<domain>
## Phase Boundary

Establish complete data structures for theme packages and local widget settings. This phase defines how themes are organized (folder structure, manifest format), how schemes reference resources, system event configurations, click region actions, and the separation between theme data vs user settings. All structures must be serializable to JSON for persistence and sharing via Steam Workshop.

</domain>

<decisions>
## Implementation Decisions

### Theme Package Structure
- **Single manifest file:** `theme.json` at root — self-contained configuration
- **Resource organization:** By media type — `/images/`, `/videos/`, `/audio/` subfolders
- **Thumbnails:** Packaged in `/thumbnails/` folder, referenced by filename
- **Minimal metadata:** Theme requires name, author, version, targetAppVersion for compatibility checking

### Scheme-Resource Relationship
- **Reference by filename:** Resources stored once per theme, schemes reference by filename (no duplication)
- **Pre-computed metadata:** Duration, resolution, format stored in theme.json (computed at build time in Creator)
- **ID-based references:** Schemes use media IDs (e.g., `{"visualMediaId": "media-123", "audioMediaIds": ["media-456"]}`) not embedded objects
- **Graceful degradation:** Missing resources logged as warnings, scheme continues with available resources

### System Event Configuration
- **Extend FeatureType enum:** Add SessionLock, SessionUnlock, NetworkDisconnect, NetworkReconnect, PowerLow, PowerCharging alongside existing Shutdown, BootRestart, ScreenWake
- **Same multi-scheme support:** System events have expandable navigation with multiple schemes per event (like Desktop Background)
- **Event queuing:** System events queue and play after current user interaction completes
- **Silent fallback:** No video configured = nothing plays (no error)

### Click Region Actions
- **Multiple regions per type:** No TriggerType enum needed; create separate ClickRegionModel instances for Head/Body/Special
- **Hover is UI-only:** Hover shows text bubble (HoverText, HoverDelayMs), not media actions
- **Sweep deferred:** Fast sweep detection mentioned in AGENTS.md but out of scope for this phase
- **Click supports sequences:** One video + up to 5 audio files, same structure as Desktop Background media list

### Local Widget Settings
- **Theme vs User boundary:** Theme provides visual style definitions (ClockStyleModel, etc.); User configures active style, opacity, format
- **Pomodoro settings:** Default global (work/break durations, DND mode), settings switch allows per-theme override
- **Anniversary data:** User data only (personal events), never in theme
- **Style identification:** UUID-based IDs for styles to handle theme updates and conflicts

### Claude's Discretion
- Exact thumbnail dimensions and compression
- Resource ID generation strategy (UUID vs sequential vs hash-based)
- JSON serialization options (casing, null handling)
- Error message wording for missing resources

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- **SchemeModel**: Base scheme with FeatureType enum (9 types) — extend with resource references
- **MediaItemModel**: Media metadata with MediaFileType, DisplayMode, PlaybackMode — use as template for theme.json resource entries
- **ClickRegionModel**: Position/size with VisualContent + AudioContent (max 5) — refactor to use ID-based references
- **ConfigService**: JSON persistence pattern — extend for theme.json loading/saving

### Established Patterns
- **MVVM with CommunityToolkit.Mvvm**: ObservableObject/ObservableProperty for all models
- **Enum-based categorization**: FeatureType for 9 feature types — extend for system events
- **Percentage coordinates**: ClickRegionModel uses 0-100% for screen-relative positioning — continue this pattern
- **ObservableCollection**: MediaItem lists are observable — theme.json will deserialize to these

### Integration Points
- **CreatorView theme export**: Creator builds theme packages — needs to generate theme.json, compute metadata, package thumbnails
- **WallpaperService theme loading**: Loads and applies active theme — needs theme.json parser, resource resolution
- **Existing config files**: `config.wallpaper` per scheme — migrate to unified theme.json structure
- **App.xaml.cs DI**: Register new ThemeService, ThemeLoader for dependency injection

### Structural Changes Needed
1. **New Models:**
   - `ThemeManifest` (root theme.json structure)
   - `ResourceEntry` (metadata for each media file)
   - `ThemeResourceLibrary` (collection of resources with lookup by ID)

2. **Modified Models:**
   - `ClickRegionModel`: Replace MediaItemModel properties with media ID strings
   - `SchemeModel`: Add resource reference lists instead of inline media
   - `FeatureType` enum: Add 6 new system event types

3. **New Services:**
   - `ThemeService`: Load/save/validate theme packages
   - `ResourceResolver`: Map media IDs to file paths within theme folder

</code_context>

<specifics>
## Specific Ideas

- Theme structure inspired by Steam Workshop format: manifest + resources + thumbnails in single folder
- Resource organization by type (`/images/`, `/videos/`, `/audio/`) keeps theme packages browsable
- Graceful degradation for missing resources ensures themes don't break if files are removed
- UUID-based style IDs survive theme updates and allow cross-theme style references

</specifics>

<deferred>
## Deferred Ideas

- **Fast Sweep Actions**: AGENTS.md mentions sweep detection but deferred to future phase
- **Resource Deduplication**: Hash-based deduplication across themes (future optimization)
- **Theme Dependencies**: One theme can require/extend another (complex feature, Phase 3+)
- **Resource Streaming**: On-demand download of large video files (Phase 3+)
- **Theme Version Migration**: Automatic upgrade of old theme formats (when format changes)
- **Per-Region Sweep**: Currently global sweep setting, could be per-region in future

</deferred>

---

*Phase: 02-data-structure*  
*Context gathered: 2026-03-15*
