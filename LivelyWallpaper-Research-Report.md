# Lively Wallpaper 技术研究报告

> 对比分析 [Lively Wallpaper](https://github.com/rocksdanister/lively) 与 ProductivityWallpaper 的核心技术差异

---

## 1. 项目概览

| 维度 | Lively Wallpaper | ProductivityWallpaper |
|------|-----------------|----------------------|
| 语言/框架 | C# / WPF + WinUI 3 | C# / WPF (.NET 8) |
| 架构模式 | 多进程 (主进程 + 独立播放器进程) | 单进程 (所有窗口在同一进程) |
| 支持媒体 | Video, GIF, HTML/Web, Shaders, Games, 图片 | Video (MP4/WebM), 图片, GIF |
| 视频播放 | 独立进程: VLC / mpv / WMF / LibVLC | 进程内 LibVLCSharp.WPF (HwndHost) |
| 网页渲染 | CefSharp / WebView2 (独立进程) | 不支持 |
| 多显示器 | 完整支持 (每屏独立壁纸/跨屏) | 仅主屏 |
| 性能管理 | 全屏应用自动暂停 (0% CPU/GPU) | 无 |
| 许可证 | Ms-PL | - |

---

## 2. 核心注入技术对比

### 2.1 WorkerW 发现 — 相同的基础原理

两个项目都使用 **相同的底层技术** 将窗口注入到桌面图标层之下：

```
1. 找到 Progman (Program Manager) 窗口
2. 发送 0x052C 消息 → 触发 Windows 创建 WorkerW
3. 枚举顶层窗口，找到包含 SHELLDLL_DefView 的窗口
4. 取其下一个兄弟窗口 WorkerW 作为壁纸渲染目标
5. 使用 SetParent() 将播放器窗口挂载为 WorkerW 的子窗口
```

### 2.2 Windows 11 "Raised Desktop" — Lively 的关键突破 ⭐

**这是两个项目最大的技术差距。**

Windows 11 某些版本引入了 "Raised Desktop" 模式，**彻底改变了桌面窗口层级结构**：

#### 传统模式 (Win10 / Win11 早期)
```
Progman → 包含 SHELLDLL_DefView (桌面图标)
WorkerW → 壁纸渲染目标 (顶层兄弟窗口，由 0x052C 创建)
```

#### Raised Desktop 模式 (Win11 新版本)
```
Progman (WS_EX_NOREDIRECTIONBITMAP — 无 GDI 内容!)
  ├── SHELLDLL_DefView (WS_EX_LAYERED — 图标层透明!)
  │     └── FolderView (SysListView32)
  └── WorkerW (子窗口，非顶层!)
```

**微软官方说明 (Lively 源码注释引用):**

> "When the desktop is split out from the list view window (the 'raised desktop'), we no longer create multiple top-level HWNDs. Instead, the top-level Progman window is now created with `WS_EX_NOREDIRECTIONBITMAP` (so there is no GDI content for that window at all) and the shell DefView child window is a `WS_EX_LAYERED` child window."

> "If your application forces the 'raised desktop' state, it will now need to create its own `WS_EX_LAYERED` child HWND that is z-ordered under the DefView window but above the WorkerW window. This window should likely be a `SetLayeredWindowAttributes(bAlpha=0xFF)` window so that you can do DX blt presents to it and not suffer performance issues."

#### Lively 的处理方式

```csharp
// 检测是否为 raised desktop 模式
isRaisedDesktopWithLayeredShellView = WindowUtil.HasExtendedStyle(
    progman, NativeMethods.WindowStyles.WS_EX_NOREDIRECTIONBITMAP);

// Raised Desktop: 挂载到 Progman (而非 WorkerW!)
if (isRaisedDesktopWithLayeredShellView) {
    WindowUtil.SetWindowStyle(hwnd, WS_CHILD);
    WindowUtil.SetWindowTransparency(hwnd, 255);  // WS_EX_LAYERED + alpha=0xFF
    SetParent(hwnd, progman);                      // 挂到 Progman
    // Z-Order: 在 DefView 之下 (壁纸在图标后面)
    SetWindowPos(hwnd, shellDLL_DefView, ...);
    EnsureWorkerWZOrder();
} else {
    // 经典模式: 挂载到 WorkerW
    SetParent(hwnd, workerW);
}
```

#### ProductivityWallpaper 的现状

当前项目 **没有检测 raised desktop 模式**，始终使用经典 WorkerW 注入。在 Win11 新版本上可能导致：
- 壁纸不可见 (Progman 无 GDI 重定向)
- WPF 渲染内容丢失
- 窗口层级错乱

---

## 3. 播放器架构 — 进程隔离 vs 进程内

### 3.1 Lively: 独立进程架构 ⭐⭐

**这是 Lively 最重要的架构决策**，也是 ProductivityWallpaper 当前最大的技术痛点根源。

Lively 将每种播放器实现为 **独立的可执行文件**：

| 播放器 | 独立进程 | 通信方式 |
|--------|---------|---------|
| `Lively.Player.Vlc` | ✅ VLC.exe 进程 | 标准输入/输出 (stdin/stdout JSON IPC) |
| `Lively.Player.Wmf` | ✅ WMF 进程 | stdin/stdout |
| `Lively.Player.WebView2` | ✅ WebView2 进程 | stdin/stdout + named pipe |
| `Lively.Player.CefSharp` | ✅ CefSharp 进程 | stdin/stdout |
| mpv player | ✅ mpv.exe 进程 | stdin/stdout JSON IPC |

#### 工作流程

```
主进程 (WinDesktopCore)
  ↓ Process.Start(PlayerExe, args)
  ↓ 等待子进程报告 HWND (msg_hwnd)
  ↓ 获取子进程窗口句柄
  ↓ BorderlessWinStyle(handle)    ← 去边框
  ↓ RemoveWindowFromTaskbar(handle) ← 隐藏任务栏
  ↓ SetParent(handle, workerW)    ← 注入桌面
  ↓ SetWindowPos(...)             ← 调整位置/大小
```

#### 为什么进程隔离很重要

1. **彻底避免 VLC 原生线程冲突**: VLC 的渲染线程、回调线程在独立进程中，不会干扰主进程的 UI 线程
2. **崩溃隔离**: 播放器崩溃不会影响主进程 (只需重启播放器)
3. **DirectX 渲染独立**: 每个播放器有自己的 DirectX 上下文，不存在 WPF Airspace 问题
4. **内存隔离**: 播放器进程的内存泄漏不影响主进程
5. **窗口句柄独立**: 每个播放器的 HWND 是原生窗口，不是 WPF 的 HwndHost

### 3.2 ProductivityWallpaper: 进程内架构

当前项目在 **同一进程** 中使用 `LibVLCSharp.WPF.VideoView` (基于 HwndHost):

```
单一进程
  ├── WPF UI 线程
  ├── LibVLC 原生渲染线程 (同一进程)
  ├── VLC 回调线程 (同一进程)
  └── VLC 音频线程 (同一进程)
```

#### 这导致的问题

| 问题 | 原因 | Lively 无此问题的原因 |
|------|------|---------------------|
| AccessViolationException | VLC 原生线程与 WPF 线程竞争 | 独立进程，kill 即可 |
| HwndHost Airspace 限制 | WPF 无法在 HwndHost 上方叠加内容 | 不使用 WPF 渲染，原生窗口 |
| 视频边缘透明 | HwndHost 大小不匹配 | 原生窗口自适应 |
| 第二次点击弹出独立窗口 | SetParent 后窗口状态异常 | 每次新进程新窗口 |
| 图片不可见 | WPF DirectX 在 WorkerW 中失效 | 使用 IDesktopWallpaper COM 或原生渲染 |

---

## 4. 静态图片壁纸 — 完全不同的方法

### 4.1 Lively: IDesktopWallpaper COM 接口

Lively 的 `PictureWinAPI` 不做任何窗口注入，而是 **直接使用 Windows COM 接口**：

```csharp
IDesktopWallpaper desktop = new DesktopWallpaperClass();
desktop.SetPosition(DesktopWallpaperPosition.Fill);
desktop.SetWallpaper(displayDeviceId, filePath);  // 原生 API，Windows 自己渲染
```

优势：
- **零性能开销** — Windows 原生壁纸管道
- **完美 DPI 处理** — 系统自动处理
- **多显示器原生支持** — 每个屏幕可独立设置
- **HDR 支持** — Windows 原生管道支持
- 无需 SetParent、WorkerW 注入

### 4.2 ProductivityWallpaper: GDI 手动绘制

当前项目创建 WPF Window → 注入 WorkerW → 用 GDI WM_PAINT 手动绘制图片：

```csharp
// WPF Image 控件在 WorkerW 中不可见 (DWM 问题)
// 所以使用 GDI+ 在 WM_PAINT 中手动绘制
using (var g = Graphics.FromHdc(ps.hdc))
{
    g.DrawImage(bitmap, drawX, drawY, drawW, drawH);
}
```

问题：
- GDI+ 绘制性能不如系统原生
- 需要手动处理 DPI、缩放、重绘
- WPF 的 Image 控件完全不起作用（浪费资源）

---

## 5. DWM Thumbnail — Lively 的高级技术

Lively 有一个 `DwmThumbnailPlayer`，使用 **DWM Thumbnail API**：

```csharp
// 创建一个空白窗口
var blankWindow = new BlankWindow();

// 使用 DWM 缩略图 API 将源窗口内容"镜像"到目标窗口
var dwmThumbnail = new DwmThumbnailWrapper(sourceHwnd, targetHwnd);
dwmThumbnail.Show();
dwmThumbnail.Update(sourceRect, destRect);
```

DWM Thumbnail API (`DwmRegisterThumbnail`) 允许将一个窗口的 DWM 合成内容高效地"投影"到另一个窗口上，完全由 GPU 完成，零 CPU 拷贝。

ProductivityWallpaper 没有使用此技术。

---

## 6. 性能管理 — Lively 的暂停系统

### Lively 的 Playback 系统

```
全屏应用运行时 → 自动暂停壁纸 (0% CPU/GPU)
桌面可见时 → 恢复播放
可自定义规则 → 特定应用打开时暂停

实现原理:
- 监控前台窗口变化 (WinEventHook)
- 检测窗口是否覆盖显示器 (像素级网格检测)
- 向播放器发送 Pause/Play IPC 消息
- 支持按显示器独立暂停
```

### ProductivityWallpaper

当前没有实现性能管理/自动暂停系统。视频壁纸始终消耗 GPU 资源。

---

## 7. 窗口生命周期管理对比

### Lively: 健壮的恢复机制

```
WorkerW 被销毁?
  ↓ WindowEventHook 检测到 EVENT_OBJECT_DESTROY
  ↓ 重新 SetupDesktopLayer()
  ↓ 重新注入所有壁纸

Explorer 崩溃重启?
  ↓ WM_TASKBARCREATED 消息触发
  ↓ 检测 PID 变化确认崩溃
  ↓ ResetWallpaperAsync() 完整重建

WorkerW 状态缓存:
  ↓ 使用 Hook 而非轮询
  ↓ 实时监控 WorkerW 状态
```

### ProductivityWallpaper

使用 `_cachedWorkerW` 缓存，但：
- 没有 Hook 监控 WorkerW 销毁
- 没有 Explorer 崩溃恢复机制
- 依赖手动重试 (cache invalidation + retry)

---

## 8. Windows 11 兼容性关键点

### 8.1 Raised Desktop (WS_EX_NOREDIRECTIONBITMAP)

| 行为 | Lively | ProductivityWallpaper |
|------|--------|----------------------|
| 检测 raised desktop | ✅ 检查 Progman 的 WS_EX_NOREDIRECTIONBITMAP | ❌ 未检测 |
| 适配注入目标 | ✅ Progman (raised) / WorkerW (classic) | ❌ 始终 WorkerW |
| WS_EX_LAYERED 处理 | ✅ 为壁纸窗口添加 WS_EX_LAYERED + alpha=0xFF | ❌ 未处理 |
| Z-Order 管理 | ✅ 插入 DefView 之下，WorkerW 之上 | ❌ 使用 HWND_TOP |
| WorkerW 查找 (Win11) | ✅ 作为 Progman 的子窗口查找 | ✅ 多策略查找 (A/B/C) |

### 8.2 HDR 壁纸支持

Lively 的 IDesktopWallpaper 自动支持 HDR，因为使用 Windows 原生壁纸管道。
ProductivityWallpaper 不支持 HDR。

---

## 9. 对 ProductivityWallpaper 的改进建议

### 9.1 紧急 (解决当前 Bug)

#### P0: 检测并适配 Win11 Raised Desktop

```
问题: 图片不可见、视频边缘透明
原因: Win11 raised desktop 模式下，WorkerW 的行为完全不同
方案: 
  1. 检测 Progman 是否有 WS_EX_NOREDIRECTIONBITMAP
  2. 如果是 raised desktop:
     - 挂载到 Progman (而非 WorkerW)
     - 添加 WS_CHILD + WS_EX_LAYERED
     - SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA)
     - Z-Order 放在 SHELLDLL_DefView 之下
  3. 否则保持经典 WorkerW 注入
```

#### P0: 第二次点击视频弹出独立窗口

```
问题: 第二次点击触发的视频在独立窗口播放，而非壁纸层
根因分析:
  - SetParent 后 WPF Window 的内部状态可能被污染
  - 首次注入时 WorkerW 缓存有效
  - 清理后 WorkerW 句柄可能过期或 HwndSource 状态不一致
  
Lively 为什么没有这个问题:
  - 每次播放都启动新进程、新窗口
  - 进程结束 = 窗口完全销毁，无状态残留
  
建议方案:
  1. 确保每次 action video 使用全新 Window 实例 (不复用)
  2. 验证 InjectActionLayer 中 SetParent 的返回值
  3. 在 StopAndClose 完成后强制 _cachedWorkerW = IntPtr.Zero
  4. 长期: 考虑迁移到独立进程播放器
```

### 9.2 中期改进

#### 图片壁纸使用 IDesktopWallpaper COM

```
参考 Lively 的 PictureWinAPI:
  - 使用 IDesktopWallpaper COM 接口设置静态图片
  - 完全不需要 WorkerW 注入
  - 系统原生渲染，完美 DPI/HDR 支持
  - 多显示器开箱即用
```

#### 性能管理 (全屏应用自动暂停)

```
参考 Lively 的 Playback 系统:
  - 监控前台窗口变化
  - 全屏应用运行时暂停壁纸
  - 恢复到桌面时自动播放
  - 大幅降低游戏时的 GPU 消耗
```

#### WorkerW 生命周期监控

```
参考 Lively 的 WindowEventHook:
  - Hook WorkerW 的 EVENT_OBJECT_DESTROY
  - 自动重建桌面层 + 重新注入壁纸
  - 监听 WM_TASKBARCREATED 处理 Explorer 崩溃
```

### 9.3 长期架构改进

#### 独立进程播放器架构

```
这是解决 VLC 线程安全问题的终极方案:

当前架构:
  [主进程] ← WPF UI + VLC (同一进程，线程冲突)

目标架构:
  [主进程] ← WPF UI + 壁纸管理
       ↕ IPC (stdin/stdout 或 named pipe)
  [播放器进程] ← VLC/mpv 原生渲染 (独立进程)
  
优势:
  - 彻底消除 AccessViolationException
  - 消除 HwndHost Airspace 问题
  - 播放器崩溃不影响主应用
  - 可随时 kill/restart 播放器
  - 原生 DirectX 渲染，无 WPF 干扰
```

#### DWM Thumbnail 技术

```
用于高效的窗口内容镜像:
  - DwmRegisterThumbnail API
  - GPU 加速，零 CPU 拷贝
  - 适合预览/缩略图场景
```

---

## 10. 技术差异总结矩阵

| 技术领域 | Lively | ProductivityWallpaper | 差距 |
|---------|--------|----------------------|------|
| 桌面注入 | WorkerW + Raised Desktop 双模式 | 仅 WorkerW | 🔴 高 |
| 播放器隔离 | 独立进程 | 进程内 | 🔴 高 |
| VLC 线程安全 | 进程隔离，天然安全 | 复杂的手动同步 | 🔴 高 |
| 静态图片 | IDesktopWallpaper COM | GDI 手动绘制 | 🟡 中 |
| 窗口恢复 | Hook + 自动重建 | 手动缓存/重试 | 🟡 中 |
| 性能暂停 | 全屏检测 + 自动暂停 | 无 | 🟡 中 |
| DPI 处理 | MapWindowPoints + 物理像素 | GetSystemMetrics 物理像素 | 🟢 低 |
| 多显示器 | 完整支持 | 仅主屏 | 🟡 中 |
| Web 壁纸 | CefSharp / WebView2 | 不支持 | 🟢 规划外 |
| 鼠标交互 | RawInput | 全局鼠标钩子 | 🟢 各有优劣 |

---

## 11. 优先行动建议

1. **🔴 立即**: 添加 Win11 Raised Desktop 检测与适配（根因修复图片不可见和视频透明边缘）
2. **🔴 立即**: 修复 action video 第二次注入失败（确保完全销毁旧窗口 + WorkerW 状态一致）
3. **🟡 短期**: 图片壁纸改用 IDesktopWallpaper COM 接口
4. **🟡 短期**: 添加 WorkerW 销毁监听和自动恢复
5. **🟡 中期**: 实现全屏应用自动暂停
6. **🔵 长期**: 迁移到独立进程播放器架构

---

*报告生成日期: 2026-04-13*
*基于 Lively Wallpaper commit: 9d996c3 (master)*
*基于 ProductivityWallpaper commit: 3a048e6*
