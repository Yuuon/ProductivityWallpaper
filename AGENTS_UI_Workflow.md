# UI 工作流规范 - AGENTS_UI_Workflow.md

> Figma 设计图转 WPF UI 的批量化开发规范与工作流程指南。
> 
> 基于实际项目分析：`exampleWindow.xaml` (Uno导出参考) + `page_workshop_default.svg` (设计基准)

---

## 1. 核心原则

### 1.1 数据源优先级（严格遵循）

| 优先级 | 来源 | 用途 | 可信度 |
|:------:|------|------|:------:|
| ⭐⭐⭐ | **SVG 图片** | 颜色、尺寸、位置、圆角 | 100% |
| ⭐⭐ | **文字说明** | 动态内容、交互逻辑、数据绑定 | 100% |
| ⭐ | **XAML 导出** | 仅参考命名规范、层级结构 | 50% |

**⚠️ 重要约束**：
- XAML 导出（Uno Platform 插件）使用 `utu:AutoLayout` + 绝对定位，与 WPF 不兼容
- **禁止**直接复制导出的 XAML 布局代码
- **必须**手动将绝对坐标转换为 WPF 的 `Grid`/`Canvas` 系统

---

## 2. 颜色系统（基于 SVG 实际值）

### 2.1 主色渐变

| 属性 | 值 |
|------|-----|
| 起点颜色 | `#4586FF` (蓝色) |
| 终点颜色 | `#FF90A9` (粉色) |
| 方向 | 水平 (`StartPoint="0,0.5" EndPoint="1,0.5"`) |
| 用途 | Logo、主按钮、标题高亮、进度条 |

```xml
<LinearGradientBrush x:Key="PrimaryGradientBrush" StartPoint="0,0.5" EndPoint="1,0.5">
    <GradientStop Offset="0" Color="#4586FF"/>
    <GradientStop Offset="1" Color="#FF90A9"/>
</LinearGradientBrush>
```

### 2.2 辅助色

| 名称 | 色值 | 用途 |
|------|------|------|
| 辅助色1 (Accent1) | `#6F7CFF` | 次级按钮、边框、链接 |
| 辅助色2 (Accent2) | `#F98FAB` | 标签选中态、悬停强调 |
| 图标色 (Icon) | `#A4A3B6` | SVG 图标、搜索图标、设置图标 |

### 2.3 背景色

| 名称 | 色值 | 透明度 | SVG 中的使用 |
|------|------|--------|--------------|
| 主背景色 | `#0B1828` | 100% | `rect` 全屏填充 |
| 背景色块(透明) | `#605F77` | 25% (`fill-opacity="0.25"`) | 搜索框、筛选器、标签背景 |
| 背景色块(不透明) | `#605F77` | 100% | 侧边栏（如使用） |
| 背景线条/边框 | `#2E2C4A` | 100% | 分隔线、卡片边框 |

### 2.4 文本色

| 名称 | 色值 | 用途 |
|------|------|------|
| 主文本 (TextPrimary) | `#FEFFFF` | 标题、正文、按钮文字 |
| 辅助文本 (TextSecondary) | `#FEFFFF` @ 70% 或 `#A4A3B6` | 占位符、次要信息、文件大小 |

### 2.5 资源字典定义（Resources/Theme.xaml）

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <!-- 主渐变 -->
    <LinearGradientBrush x:Key="PrimaryGradientBrush" StartPoint="0,0.5" EndPoint="1,0.5">
        <GradientStop Offset="0" Color="#4586FF"/>
        <GradientStop Offset="1" Color="#FF90A9"/>
    </LinearGradientBrush>
    
    <!-- 辅助色 -->
    <SolidColorBrush x:Key="Accent1Brush" Color="#6F7CFF"/>
    <SolidColorBrush x:Key="Accent2Brush" Color="#F98FAB"/>
    <SolidColorBrush x:Key="IconBrush" Color="#A4A3B6"/>
    
    <!-- 背景色 -->
    <SolidColorBrush x:Key="BackgroundMainBrush" Color="#0B1828"/>
    <SolidColorBrush x:Key="BackgroundBlockBrush" Color="#605F77" Opacity="0.25"/>
    <SolidColorBrush x:Key="BackgroundBlockSolidBrush" Color="#605F77"/>
    <SolidColorBrush x:Key="BorderLineBrush" Color="#2E2C4A"/>
    
    <!-- 文本色 -->
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="#FEFFFF"/>
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="#A4A3B6"/>
</ResourceDictionary>
```

---

## 3. 布局系统转换规则（关键）

### 3.1 SVG 坐标系 → WPF 布局

**SVG 画布基准**：
- 标准画布尺寸：`1440×810`（宽×高）
- 所有 SVG 中的 `x`、`y`、`width`、`height` 基于此坐标系

**关键区域识别**（以 `page_workshop_default.svg` 为例）：

```
SVG 坐标系 (1440×810)
├─ 顶部栏区域: Y=0~98
│   ├─ Logo: X=42, Y=42, 32×32, rx=10 (渐变填充)
│   ├─ 产品名: X=82, Y=42 (文本)
│   ├─ 导航项: X=448/672/896, Y=42 (文本)
│   └─ 图标按钮: X=1317/1373, Y=42, 24×24
│
├─ 水平分隔线: Y=98, X=0~1440, stroke=#2E2C4A
│
├─ 搜索/筛选区: Y=122~162
│   ├─ 搜索框: X=42, W=640, H=40, rx=15
│   ├─ 分辨率筛选: X=706, W=144
│   └─ 排序筛选: X=874, W=128
│
├─ 左侧内容区 (网格): X=42~1038, Y=186~
│   └─ 3×3 卡片网格: 316×178, rx=9.5/14.5, 间距=16
│
└─ 右侧详情区: X=1038~1440
    ├─ 滚动指示条: X=1038, W=8, rx=4
    ├─ 大图预览: X=1080, 316×178
    ├─ 元信息: X=1080~1396, Y=380~450
    ├─ 标签云: X=1088~1396, Y=486~630 (胶囊按钮)
    └─ 操作按钮: X=1080, Y=662/722, 316×44, rx=22
```

### 3.2 坐标转换公式

**提取内容区（排除顶部栏）**：

```csharp
// 输入: SVG 原始坐标 (svgX, svgY)
// 输出: WPF 相对坐标 (wpfX, wpfY)

// 顶部栏高度 = 98px (水平分隔线位置)
// 内容区起始 Y = 122 (搜索框位置)

if (svgY < 98) {
    // 顶部栏元素 → 跳过或由 MainWindow 处理
    return Skip;
}

// 内容区相对坐标
wpfX = svgX;  // 保持 X 不变（或根据新布局调整）
wpfY = svgY - 98;  // 减去顶部栏高度

// 如果新页面不需要搜索区
if (svgY >= 122 && svgY < 186) {
    wpfY = svgY - 122;  // 从内容卡片区域开始
}
```

### 3.3 WPF 布局选择策略
n
| SVG 模式 | WPF 布局 | 示例 |
|----------|----------|------|
| 网格状排列 | `Grid` + `UniformGrid`/`ItemsControl` | 9个卡片 → `ItemsControl` + `WrapPanel` |
| 列表 | `ListBox`/`ListView` + 自定义 `ItemTemplate` | 标签列表 |
| 固定位置 | `Canvas` 或 `Grid` 绝对定位 | 右侧详情区 |
| 水平排列 | `StackPanel` Orientation="Horizontal" | 导航按钮 |
| 垂直排列 | `StackPanel` Orientation="Vertical" | 按钮组 |
| 填充剩余空间 | `Grid` 的 `*` 行/列定义 | 主内容区 |

### 3.4 批量化布局转换规则

**规则 1: 卡片网格自动识别**
```
SVG 特征:
- 多个 <rect> 或 <image> 相同尺寸
- 规律性间隔 (ΔX ≈ ΔY)
- X 坐标序列: 42, 374, 706 (间隔 332 = 316+16)

WPF 转换:
<ItemsControl ItemsSource="{Binding Cards}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <UniformGrid Columns="3"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border Width="316" Height="178" CornerRadius="10"
                    BorderBrush="{StaticResource BorderLineBrush}" BorderThickness="1">
                <Image Source="{Binding Thumbnail}" Stretch="UniformToFill"/>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**规则 2: 胶囊标签云识别**
```
SVG 特征:
- 多个圆角矩形，rx ≈ height/2 (rx=20, height=40)
- 内部包含文本
- 不同宽度适应文本长度

WPF 转换:
<ItemsControl ItemsSource="{Binding Tags}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border Background="{StaticResource BackgroundBlockBrush}" 
                    CornerRadius="20" Padding="12,8" Margin="4">
                <TextBlock Text="{Binding Name}" Foreground="{StaticResource TextPrimaryBrush}"/>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**规则 3: 输入框/筛选器识别**
```
SVG 特征:
- 圆角矩形 (rx=14.5) + 内部文本 + 右侧图标
- fill="#605F77" fill-opacity="0.25"
- stroke="#2E2C4A"

WPF 转换:
<Border Background="{StaticResource BackgroundBlockBrush}" 
        BorderBrush="{StaticResource BorderLineBrush}" BorderThickness="1"
        CornerRadius="15" Height="40">
    <Grid>
        <TextBox Background="Transparent" BorderThickness="0" 
                 Text="{Binding SearchText}" 
                 Foreground="{StaticResource TextPrimaryBrush}"/>
        <Path Data="..." Fill="{StaticResource IconBrush}" 
              HorizontalAlignment="Right" Margin="0,0,12,0"/>
    </Grid>
</Border>
```

---

## 4. 页面结构拆分与批量化处理

### 4.1 SVG 页面类型识别

**类型 A: 完整页面（含顶部栏）**
```
特征: SVG 包含 Y<98 的元素（Logo、导航）
处理: 
  1. 提取 Y<98 → MainWindow.xaml 顶部栏/侧边栏
  2. 提取 Y≥98 → {FeatureName}View.xaml 内容区
```

**类型 B: 纯内容页（无顶部栏）**
```
特征: SVG 最小 Y 值 ≥ 98 或 122
处理:
  1. 所有元素直接映射到 {FeatureName}View.xaml
  2. 坐标平移: Y' = Y - minY
```

**类型 C: 弹窗/浮层**
```
特征: SVG 包含遮罩层或中心定位的面板
处理:
  1. 提取为 {FeatureName}Dialog.xaml (Window)
  2. 或提取为 UserControl 用于 Popup
```

### 4.2 批量化处理工作流

```
输入: DesignAssets/{FeatureName}/
      ├── page_{FeatureName}.svg    (必需)
      └── README.md                 (可选，动态内容描述)

步骤 1: SVG 解析（自动/手动）
  ├─ 提取所有 <rect>, <text>, <path>, <image>
  ├─ 记录: x, y, width, height, fill, stroke, rx
  └─ 按 Y 坐标分组识别区域

步骤 2: 区域分类
  ├─ 顶部栏 (Y<98) → 跳过 或 更新 MainWindow
  ├─ 搜索/筛选区 (Y=122~170) → 提取为 Header 区域
  ├─ 主内容区 (Y≥186) → 识别网格/列表模式
  └─ 侧边/右侧面板 (X>1000) → 提取为 DetailPanel

步骤 3: 生成 WPF XAML
  ├─ 确定根布局: Grid/Canvas/ScrollViewer
  ├─ 按区域生成子元素
  │   ├─ 卡片网格 → ItemsControl + DataTemplate
  │   ├─ 标签云 → WrapPanel
  │   ├─ 按钮 → Style 引用
  │   └─ 文本 → TextBlock + 绑定
  └─ 应用颜色资源

步骤 4: ViewModel 生成
  ├─ 识别数据绑定点（文字描述辅助）
  ├─ 生成 ObservableProperty
  └─ 生成 ICommand

步骤 5: 集成到项目
  ├─ 复制到 Views/{FeatureName}View.xaml
  ├─ 复制到 ViewModels/{FeatureName}ViewModel.cs
  ├─ 更新 App.xaml DataTemplate
  └─ 更新 MainWindow.xaml 导航（如需要）
```

---

## 5. 与现有项目架构集成

### 5.1 当前项目结构（ProductivityWallpaper）

```
MainWindow.xaml
├── Sidebar (200px, StackPanel)
│   └── Menu Buttons → 导航到不同 ViewModel
└── ContentControl
    └── DataTemplate 映射 → UserControl
        ├── WallpaperView (壁纸库)
        ├── AiToolView (AI工具)
        ├── SettingsView (设置)
        └── {NewFeature}View (新页面)
```

### 5.2 新页面集成清单

**新增功能页面时，需要修改以下 5 个文件：**

| # | 文件 | 操作 | 内容 |
|---|------|------|------|
| 1 | `Views/{FeatureName}View.xaml` | 新建 | 从 SVG 转换的 UI |
| 2 | `Views/{FeatureName}View.xaml.cs` | 新建 | 代码隐藏 |
| 3 | `ViewModels/{FeatureName}ViewModel.cs` | 新建 | 属性 + 命令 |
| 4 | `App.xaml` | 修改 | 添加 DataTemplate 映射 |
| 5 | `MainWindow.xaml` | 修改 | 添加导航按钮 |

**文件 4 - App.xaml 修改：**
```xml
<DataTemplate DataType="{x:Type vm:{FeatureName}ViewModel}">
    <views:{FeatureName}View/>
</DataTemplate>
```

**文件 5 - MainWindow.xaml 修改：**
```xml
<!-- 在 Sidebar StackPanel 中添加 -->
<Button Content="{DynamicResource Menu_{FeatureName}}" 
        Command="{Binding NavigateTo{FeatureName}Command}" 
        Style="{StaticResource MenuButtonStyle}"/>
```

---

## 6. 动态内容绑定规范

### 6.1 文本占位符识别

SVG 中的静态文本在转换为 WPF 时，根据文字说明转换为绑定：

| SVG 文本 | 说明标注 | WPF 绑定 |
|----------|----------|----------|
| "产品名字" | {APP_NAME} | `Text="{DynamicResource AppName}"` |
| "创意工坊" | 页面标题 | `Text="{DynamicResource PageTitle}"` |
| "搜索主题名字..." | 占位符 | `Tag="{DynamicResource SearchPlaceholder}"` |
| "这里是主题名字" | {BIND: ThemeName} | `Text="{Binding SelectedTheme.Name}"` |
| "45.2 MB" | {BIND: FileSize} | `Text="{Binding SelectedTheme.FileSize, StringFormat='{}{0:F1} MB'}"` |
| "3840*2160" | {BIND: Resolution} | `Text="{Binding SelectedTheme.Resolution}"` |

### 6.2 列表数据绑定

**卡片网格绑定模板：**
```xml
<ItemsControl ItemsSource="{Binding ThemeCollection}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border CornerRadius="10" BorderThickness="1"
                    BorderBrush="{StaticResource BorderLineBrush}">
                <Grid>
                    <Image Source="{Binding Thumbnail}" Stretch="UniformToFill"/>
                    <Border Background="#AA000000" VerticalAlignment="Top" 
                            HorizontalAlignment="Right" CornerRadius="0,10,0,0">
                        <TextBlock Text="{Binding Type}" FontSize="10"/>
                    </Border>
                </Grid>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

---

## 7. 常见 SVG → WPF 映射速查表

### 7.1 形状映射

| SVG 元素 | SVG 属性 | WPF 元素 | WPF 属性 |
|----------|----------|----------|----------|
| `<rect>` | `x,y,width,height` | `<Border>` | 放入 Grid/Canvas，设 Margin |
| `<rect>` | `rx="10"` | `<Border>` | `CornerRadius="10"` |
| `<rect>` | `fill="url(#grad)"` | `<Border>` | `Background="{StaticResource PrimaryGradientBrush}"` |
| `<rect>` | `fill="#605F77" fill-opacity="0.25"` | `<Border>` | `Background="{StaticResource BackgroundBlockBrush}"` |
| `<rect>` | `stroke="#2E2C4A"` | `<Border>` | `BorderBrush="{StaticResource BorderLineBrush}" BorderThickness="1"` |
| `<line>` | `x1,y1,x2,y2` | `<Line>` 或 Border | `BorderThickness="0,0,0,1"` |
| `<path>` | `d="..."` | `<Path>` | `Data="..."` |
| `<path>` | `stroke="#A4A3B6"` | `<Path>` | `Stroke="{StaticResource IconBrush}"` |
| `<text>` | 内部文本 | `<TextBlock>` | `Text="..."` |
| `<image>` | `xlink:href` | `<Image>` | `Source="..."` |

### 7.2 尺寸映射

| SVG 用途 | SVG 尺寸 | WPF 建议 |
|----------|----------|----------|
| 标准画布 | 1440×810 | ScrollViewer 或自适应 Grid |
| 卡片 | 316×178, rx=9.5/14.5 | Border CornerRadius="10" |
| 按钮(大) | 316×44, rx=22 | CornerRadius="22" |
| 按钮(小/标签) | auto×40, rx=20 | CornerRadius="20", Padding="12,8" |
| 搜索框 | 640×40, rx=15 | CornerRadius="15" |
| 图标 | 24×24 | Width="24" Height="24" |
| 滚动条 | 8×566, rx=4 | ScrollViewer 样式或自定义 |

### 7.3 间距规律

```
从 SVG 分析的标准间距:
- 页面边距: 42px (左/上基准)
- 元素间隙: 16px (卡片之间: 374-42-316=16)
- 大区块间隙: 36px (搜索区186-122=64，减去40高度=24，实际约36)
- 文本内边距: 12px (按钮 Padding)
```

---

## 8. 示例：Workshop 页面完整转换

### 8.1 输入分析

**SVG 文件**: `page_workshop_default.svg` (1440×810)

**README.md 补充说明**:
```markdown
## 动态内容
- 主题网格: 从 API 获取，最多 9 个，3×3 排列
- 选中状态: 点击卡片后右侧显示详情
- 搜索: 实时过滤，防抖 300ms
- 标签: 可多选，选中时背景变为主渐变
- 按钮: "立即使用" → 应用主题；"去修改" → 跳转编辑器

## 交互
- 卡片悬停: 边框高亮（主渐变）
- 标签点击: 选中/取消，背景色切换
- 滚动: 右侧详情区独立滚动
```

### 8.2 生成的 WPF 结构

```xml
<UserControl x:Class="ProductivityWallpaper.Views.WorkshopView">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>      <!-- 左侧网格 -->
            <ColumnDefinition Width="360"/>    <!-- 右侧详情 -->
        </Grid.ColumnDefinitions>
        
        <!-- 左侧：搜索 + 网格 -->
        <Grid Grid.Column="0" Margin="42,24,24,24">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>   <!-- 搜索/筛选 -->
                <RowDefinition Height="*"/>      <!-- 卡片网格 -->
            </Grid.RowDefinitions>
            
            <!-- 搜索区 -->
            <Grid Grid.Row="0" Margin="0,0,0,24">
                <!-- 搜索框、下拉筛选器 -->
            </Grid>
            
            <!-- 卡片网格 -->
            <ItemsControl Grid.Row="1" ItemsSource="{Binding Themes}">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <UniformGrid Columns="3"/>
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <!-- ItemTemplate... -->
            </ItemsControl>
        </Grid>
        
        <!-- 右侧：详情面板 -->
        <ScrollViewer Grid.Column="1" Margin="0,24,42,24">
            <StackPanel>
                <!-- 大图、信息、标签云、按钮 -->
            </StackPanel>
        </ScrollViewer>
    </Grid>
</UserControl>
```

---

## 9. 批量化工具建议（未来扩展）

### 9.1 自动化脚本功能规划

```python
# 伪代码：SVG 解析器
class SvgToWpfConverter:
    def parse_svg(self, svg_path):
        # 1. 提取所有元素及其属性
        elements = self.extract_elements(svg_path)
        
        # 2. 识别布局模式
        if self.detect_grid_pattern(elements):
            layout_type = "Grid"
        elif self.detect_list_pattern(elements):
            layout_type = "List"
        else:
            layout_type = "Canvas"
        
        # 3. 生成 WPF XAML
        xaml = self.generate_xaml(elements, layout_type)
        
        # 4. 应用颜色映射
        xaml = self.apply_theme_resources(xaml)
        
        return xaml
```

### 9.2 手动处理检查清单

对于每个新页面，人工确认：

- [ ] SVG 画布尺寸确认（1440×810 或其他）
- [ ] 顶部栏元素识别（Y<98 是否跳过）
- [ ] 布局模式确认（网格/列表/自由）
- [ ] 动态内容标注（哪些文本需要绑定）
- [ ] 交互逻辑补充（点击、悬停、滚动）
- [ ] 颜色一致性检查（所有色值映射到资源）
- [ ] 间距统一（使用 8/16/24/42 标准）

---

## 10. 附录

### 10.1 颜色值校验表

| 颜色名 | 用户原始输入 | SVG 实际值 | 最终采用 | 备注 |
|--------|-------------|-----------|---------|------|
| 主渐变起点 | `#45885FF` | `#4586FF` | `#4586FF` | 用户输入可能有笔误，以 SVG 为准 |
| 主渐变终点 | `#FF90A9` | `#FF90A9` | `#FF90A9` | 一致 |
| 辅助色1 | `#6F7CFF` | `#6F7CFF` | `#6F7CFF` | 一致 |
| 辅助色2 | `#F98FAB` | - | `#F98FAB` | 设计系统中定义 |
| 图标色 | - | `#A4A3B6` | `#A4A3B6` | SVG 中 Path stroke |
| 背景主色 | `#0B1828` | `#0B1828` | `#0B1828` | 一致 |
| 背景块透明 | `#605F77@25%` | `#605F77@25%` | `#605F77@25%` | 一致 |
| 边框线 | `#2E2C4A` | `#2E2C4A` | `#2E2C4A` | 一致 |
| 主文本 | `#FEFFFF` | - | `#FEFFFF` | 设计系统定义 |

### 10.2 XAML 导出键名对照表

Uno 导出使用的 ThemeResource 键名 → WPF 标准键名：

| Uno 导出键名 | 含义 | WPF 标准键名 |
|-------------|------|-------------|
| `背景` | 主背景色 | `BackgroundMainBrush` |
| `主色` | 主渐变 | `PrimaryGradientBrush` |
| `辅助色` | 辅助色1 | `Accent1Brush` |
| `文本` | 主文本 | `TextPrimaryBrush` |
| `文本辅助色` | 次要文本 | `TextSecondaryBrush` |
| `背景_色块_透明` | 半透明块 | `BackgroundBlockBrush` |
| `正文` | 正文样式 | `BodyTextStyle` |
| `正文加粗` | 加粗正文 | `BodyBoldTextStyle` |
| `标签` | 小字样式 | `CaptionTextStyle` |
| `标题` | 标题样式 | `TitleTextStyle` |
| `Icon` | 图标颜色 | `IconBrush` |

### 10.3 标准尺寸速查

| 元素 | 宽×高 | 圆角 | 备注 |
|------|-------|------|------|
| 卡片 | 316×178 | 10 | 3列网格 |
| 主按钮 | 316×44 | 22 | 渐变背景 |
| 次按钮 | auto×44 | 22 | 透明+边框 |
| 标签 | auto×40 | 20 | Padding 12,8 |
| 搜索框 | 640×40 | 15 | 带图标 |
| 下拉框 | 128×40 | 15 | 带箭头 |
| 分隔线 | 全宽×1 | - | stroke #2E2C4A |
| 滚动指示 | 8×自动 | 4 | 右侧面板 |

---

*Last updated: 2026-03-07*  
*基于 ReferenceResources/exampleWindow.xaml 和 page_workshop_default.svg 分析更新*
