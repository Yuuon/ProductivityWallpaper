# ProductivityWallpaper - AGENTS.md

> 本项目背景、技术规范与开发指南，供 AI Agent 使用。

---

## 1. 项目背景

ProductivityWallpaper 是一个基于 WPF + .NET 8 的桌面互动壁纸应用。

核心功能是将视频（MP4/WebM）或网页挂载到 Windows 桌面图标层下方（WorkerW），实现类似"动态壁纸"的效果，并叠加一层透明 UI 用于捕捉鼠标交互，模拟"看板娘"体验。

---

## 2. 技术栈

| 组件 | 技术 |
|------|------|
| 框架 | WPF (.NET 8) |
| 设计模式 | MVVM (CommunityToolkit.Mvvm) |
| 依赖注入 | Microsoft.Extensions.DependencyInjection |
| 媒体播放 | LibVLCSharp.WPF (基于 VLC) |
| 底层交互 | P/Invoke (User32.dll) |

---

## 3. 架构设计

### 3.1 三层窗口叠加模式

```
┌─────────────────────────────────────────────────────────┐
│  InteractiveUiWindow (UI Layer)                        │ ← 全透明，最顶层
│  - 热区 (Button)                                        │
│  - 气泡文本                                             │
│  - 鼠标点击响应                                         │
├─────────────────────────────────────────────────────────┤
│  _actionVideoWindow (Action Layer)                     │ ← 交互时创建
│  - 播放特定动作视频                                     │
│  - 播放结束后销毁/隐藏                                  │
├─────────────────────────────────────────────────────────┤
│  _idleVideoWindow (Idle Layer)                         │ ← 最底层
│  - 循环播放待机视频                                     │
└─────────────────────────────────────────────────────────┘
         ↓
    WorkerW (Windows 桌面图标层下方)
```

### 3.2 输入处理

- `MouseHookService`: 使用全局鼠标钩子 (WH_MOUSE_LL)
- 原因: 挂载后的窗口难以直接接收桌面鼠标消息
- 流程: 钩子捕获点击坐标 → `InteractiveUiWindow.SimulateClickIfHit` 碰撞检测

---

## 4. 关键规范 (必须遵守)

### 4.1 窗口初始化防闪烁规则 ⭐ CRITICAL

**问题**: WPF 窗口在 `Show()` 瞬间会以默认尺寸（如 800x450）在屏幕左上角渲染一帧黑色画面，导致视觉闪烁。

**解决方案** (必须按此顺序):

```csharp
// 1. 创建窗口
var window = new Window();

// 2. 缩地成寸
window.Width = 1;
window.Height = 1;

// 3. 移至屏外
window.Left = -32000;
window.Top = -32000;

// 4. 显示 (此时用户不可见)
window.Show();

// 5. 挂载到 WorkerW
Win32Api.SetParent(window.Handle, workerwHandle);

// 6. 等待视频缓冲
await Task.Delay(100);

// 7. 恢复尺寸
Win32Api.SetWindowPos(window.Handle, ..., screenWidth, screenHeight, ...);
```

**禁止**: 
- 不要直接 `Show()` 再调整大小
- 不要用 `Opacity=0` 来隐藏 VideoView (Airspace 问题导致透明度对视频无效)

### 4.2 视频透明度限制

- `LibVLCSharp.WPF.VideoView` 基于 `HwndHost`
- **视频层不支持透明背景**
- WebM 透明视频无法叠加在桌面壁纸上
- **视频必须自带背景**

### 4.3 交互层级规则

- `InteractiveUiWindow` 必须始终保持在 Z序最顶层
- 所有 UI 元素（文本气泡、提示）必须绘制在 `InteractiveUiWindow` 中
- 不要尝试在视频窗口中绘制 UI

---

## 5. 代码结构

```
ProductivityWallpaper/
├── App.xaml                  # 应用入口
├── App.xaml.cs               # DI 配置
├── MainWindow.xaml           # 主窗口（托盘/控制面板）
├── MainWindow.xaml.cs
├── Converters/               # 值转换器
├── Models/
│   └── InteractiveConfig.cs  # 热区配置模型
├── Services/
│   ├── WallpaperService.cs   # 核心逻辑（窗口管理、挂载、播放）⭐
│   └── MouseHookService.cs   # 全局鼠标钩子
├── ViewModels/
│   └── (ViewModels)
├── Views/
│   ├── InteractiveUiWindow.xaml   # 透明交互层 ⭐
│   └── InteractiveUiWindow.xaml.cs
└── Properties/
```

### 关键文件说明

| 文件 | 职责 |
|------|------|
| `Services/WallpaperService.cs` | 核心逻辑：窗口创建/销毁、WorkerW 挂载、视频播放控制、状态管理 |
| `Services/MouseHookService.cs` | 全局鼠标钩子安装/卸载、坐标捕获、事件分发 |
| `Views/InteractiveUiWindow.xaml` | 透明交互层 XAML：热区布局、气泡模板 |
| `Models/InteractiveConfig.cs` | 热区定义（坐标、大小、触发行为） |

---

## 6. 已开发功能

### 6.1 增强鼠标交互模块 ✅

#### 多热区点击反馈
- `InteractionTrigger` 新增 `TriggerType` 枚举 (Head/Body/Special)
- 每个热区可独立配置 `ActionVideo` 和 `Audio`
- `WallpaperService` 在点击时根据热区配置播放对应的视频和音频

#### 鼠标悬停反馈 (Hover)
- `InteractionTrigger` 支持 `HoverText` 和 `HoverDelayMs` 配置
- `InteractiveUiWindow` 实现悬停逻辑：
  - 鼠标停留在热区超过 500ms (可配置) 显示 UI 气泡
  - 气泡自动定位，避免超出边界
  - 鼠标移开时气泡消失
  - 事件：`OnTriggerHoverStart` / `OnTriggerHoverEnd`
- 悬停仅显示 UI 气泡，不打断背景视频

#### 快速横扫检测 (Fast Sweep)
- `MouseHookService` 记录最近 5 个鼠标坐标点和时间戳
- 计算鼠标移动速度向量 (像素/秒)
- 速度超过阈值（默认 3000 像素/秒）触发 `OnMouseSweep` 事件
- `InteractiveConfig` 配置 `SweepActionVideo` 和 `SweepSpeedThreshold`
- `WallpaperService` 订阅横扫事件，触发播放特效视频
- 如果当前正在播放 Action 视频则忽略横扫

## 7. Roadmap (待开发)

1. **系统感知**
   - 监听锁屏/解锁
   - 网络断开/重连
   - 电源/电量状态
   - 播放对应视频响应

2. **媒体扩展**
   - FFmpeg 生成视频缩略图
   - 支持 GIF/WebP/HEIC 静态展示

---

## 7. 常见陷阱速查

| 问题 | 原因 | 解决 |
|------|------|------|
| 窗口初始化闪烁 | Show() 默认尺寸渲染 | 按 4.1 规则初始化 |
| 视频透明无效 | HwndHost Airspace 限制 | 视频必须自带背景 |
| 热区点击无响应 | UI 层被遮挡 | 确保 InteractiveUiWindow 最顶层 |
| 鼠标消息丢失 | WorkerW 挂载后焦点问题 | 使用全局鼠标钩子 |

---

## 8. 构建与运行

```bash
# 构建
dotnet build

# 运行
dotnet run
```

**依赖**: 
- .NET 8 SDK
- VLC 运行库 (LibVLCSharp 需要)

---

*Last updated: 2026-02-15*
