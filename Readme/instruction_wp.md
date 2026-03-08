# ProductivityWallpaper 项目开发上下文汇总

## 1. 项目简介
这是一个基于 WPF + .NET 8 的桌面互动壁纸应用。
核心功能是将视频（MP4/WebM）或网页挂载到 Windows 桌面图标层下方（WorkerW），实现类似“动态壁纸”的效果，并叠加一层透明 UI 用于捕捉鼠标交互，模拟“看板娘”体验。

## 2. 核心技术栈
- **框架**: WPF (.NET 8), MVVM (CommunityToolkit.Mvvm)
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **媒体播放**: LibVLCSharp.WPF (基于 VLC)
- **底层交互**: P/Invoke (User32.dll) 用于窗口挂载 (SetParent) 和消息拦截。

## 3. 关键架构设计
应用采用“三层窗口叠加”模式：
1.  **底层 (Idle Layer)**: `_idleVideoWindow`。循环播放待机视频。挂载于 WorkerW，Z序最低。
2.  **动作层 (Action Layer)**: `_actionVideoWindow`。仅在触发交互时创建，播放特定动作视频（如点击反馈）。播放结束后销毁或隐藏。
3.  **交互层 (UI Layer)**: `InteractiveUiWindow`。全透明窗口，挂载于 WorkerW 最上层。用于绘制热区（Button）、气泡文本，并响应鼠标点击。
4.  **输入拦截**: `MouseHookService` 使用全局鼠标钩子 (WH_MOUSE_LL)，因为挂载后的窗口难以直接接收桌面鼠标消息。钩子捕获点击坐标后，调用 `InteractiveUiWindow.SimulateClickIfHit` 进行碰撞检测。

## 4. 必须遵守的“铁律”与已知坑
### (1) 窗口初始化防闪烁规则 (CRITICAL)
**问题**: WPF 窗口在 `Show()` 瞬间会以默认尺寸（如 800x450）在屏幕左上角渲染一帧黑色画面，导致视觉闪烁。
**解决方案**: 严禁直接 Show。必须遵循以下初始化流程：
1.  `new Window()`。
2.  设置 `Width=1, Height=1` (缩地成寸)。
3.  设置 `Left=-32000, Top=-32000` (移至屏外)。
4.  `Show()` (此时用户不可见)。
5.  执行 `Win32Api.SetParent` 挂载到 WorkerW。
6.  等待视频缓冲 (100ms)。
7.  使用 `Win32Api.SetWindowPos` 瞬间将窗口尺寸修改为屏幕分辨率 (`SystemParameters.PrimaryScreenWidth`)。
**禁止**: 不要试图用 Opacity=0 来隐藏 VideoView (HwndHost)，因为 Airspace 问题导致透明度对视频无效。

### (2) 视频透明度限制
由于使用 `LibVLCSharp.WPF.VideoView` (HwndHost)，视频层**不支持透明背景**。
- **不可行**: WebM 透明视频叠加在桌面壁纸上。
- **现状**: 视频必须自带背景。

### (3) 交互层级
- `InteractiveUiWindow` 必须始终保持在 Z序最顶层，否则用户无法点击热区。
- 所有的文本气泡、UI 提示都必须绘制在 `InteractiveUiWindow` 中，而不是视频窗口中。

## 5. 待开发功能列表 (Roadmap)
1.  **交互增强**: 区分头部/身体点击反馈；鼠标悬停显示气泡；快速横扫(Sweep)检测。
2.  **系统感知**: 监听锁屏/解锁、网络断开/重连、电源/电量状态，并播放对应视频。
3.  **媒体扩展**: 集成 FFmpeg 生成视频缩略图；支持 GIF/WebP/HEIC 静态展示。

## 6. 代码文件结构
- `Services/WallpaperService.cs`: 核心逻辑（窗口管理、挂载、播放控制）。
- `Services/MouseHookService.cs`: 全局鼠标钩子。
- `Views/InteractiveUiWindow.xaml`: 透明交互层。
- `Models/InteractiveConfig.cs`: 定义热区坐标和触发行为。