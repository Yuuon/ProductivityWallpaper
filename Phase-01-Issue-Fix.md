# Phase 01 问题修复需求文档 -- 五次修复

所有问题集中在CreatorView页面下

运行测试Log:
[Navigation] Selecting feature: ThemePreview
[Navigation] Loading content for: ThemePreview
[NavigationMonitor] FAILED: ThemePreview
[NavigationMonitor] ERROR: ViewModel is null
[Navigation] WARNING: ThemePreview loaded but ConfigurationContent is null
[NavigationMonitor] SUCCESS: DesktopBackground -> DesktopBackgroundViewModel
“ProductivityWallpaper.exe”(CoreCLR: clrhost): 已加载“C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\8.0.24\zh-Hans\System.Xaml.resources.dll”。模块已生成，不包含符号。
[NavigationMonitor] SUCCESS: MouseClick -> MouseClickViewModel
[NavigationMonitor] SUCCESS: Shutdown -> ShutdownViewModel
[NavigationMonitor] SUCCESS: BootRestart -> BootRestartViewModel
[NavigationMonitor] SUCCESS: ScreenWake -> ScreenWakeViewModel
[Navigation] Selecting feature: OpenApp
[Navigation] Loading content for: OpenApp
[NavigationMonitor] FAILED: OpenApp
[NavigationMonitor] ERROR: ViewModel is null
[Navigation] WARNING: OpenApp loaded but ConfigurationContent is null
[Navigation] Selecting feature: DesktopClock
[Navigation] Loading content for: DesktopClock
DesktopClockViewModel created: True
[NavigationMonitor] SUCCESS: DesktopClock -> DesktopClockViewModel
[Navigation] SUCCESS: DesktopClock loaded, Content type: DesktopClockViewModel
System.Windows.Data Error: 23 : Cannot convert '' from type 'String' to type 'System.Windows.Media.ImageSource' for 'en-US' culture with default conversions; consider using Converter property of Binding. NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at System.ComponentModel.TypeConverter.GetConvertFromException(Object value)
   at System.ComponentModel.TypeConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at System.Windows.Media.ImageSourceConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)'
System.Windows.Data Error: 6 : 'TargetDefaultValueConverter' converter failed to convert value '' (type 'String'); fallback value will be used, if available. BindingExpression:Path=PreviewImagePath; DataItem='ClockStyleModel' (HashCode=28583536); target element is 'Image' (Name=''); target property is 'Source' (type 'ImageSource') NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)
   at MS.Internal.Data.TargetDefaultValueConverter.Convert(Object o, Type type, Object parameter, CultureInfo culture)
   at System.Windows.Data.BindingExpression.ConvertHelper(IValueConverter converter, Object value, Type targetType, Object parameter, CultureInfo culture)'
System.Windows.Data Error: 23 : Cannot convert '' from type 'String' to type 'System.Windows.Media.ImageSource' for 'en-US' culture with default conversions; consider using Converter property of Binding. NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at System.ComponentModel.TypeConverter.GetConvertFromException(Object value)
   at System.ComponentModel.TypeConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at System.Windows.Media.ImageSourceConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)'
System.Windows.Data Error: 6 : 'TargetDefaultValueConverter' converter failed to convert value '' (type 'String'); fallback value will be used, if available. BindingExpression:Path=PreviewImagePath; DataItem='ClockStyleModel' (HashCode=46663997); target element is 'Image' (Name=''); target property is 'Source' (type 'ImageSource') NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)
   at MS.Internal.Data.TargetDefaultValueConverter.Convert(Object o, Type type, Object parameter, CultureInfo culture)
   at System.Windows.Data.BindingExpression.ConvertHelper(IValueConverter converter, Object value, Type targetType, Object parameter, CultureInfo culture)'
System.Windows.Data Error: 23 : Cannot convert '' from type 'String' to type 'System.Windows.Media.ImageSource' for 'en-US' culture with default conversions; consider using Converter property of Binding. NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at System.ComponentModel.TypeConverter.GetConvertFromException(Object value)
   at System.ComponentModel.TypeConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at System.Windows.Media.ImageSourceConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)'
System.Windows.Data Error: 6 : 'TargetDefaultValueConverter' converter failed to convert value '' (type 'String'); fallback value will be used, if available. BindingExpression:Path=PreviewImagePath; DataItem='ClockStyleModel' (HashCode=43648078); target element is 'Image' (Name=''); target property is 'Source' (type 'ImageSource') NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)
   at MS.Internal.Data.TargetDefaultValueConverter.Convert(Object o, Type type, Object parameter, CultureInfo culture)
   at System.Windows.Data.BindingExpression.ConvertHelper(IValueConverter converter, Object value, Type targetType, Object parameter, CultureInfo culture)'
System.Windows.Data Error: 23 : Cannot convert '' from type 'String' to type 'System.Windows.Media.ImageSource' for 'en-US' culture with default conversions; consider using Converter property of Binding. NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at System.ComponentModel.TypeConverter.GetConvertFromException(Object value)
   at System.ComponentModel.TypeConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at System.Windows.Media.ImageSourceConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)'
System.Windows.Data Error: 6 : 'TargetDefaultValueConverter' converter failed to convert value '' (type 'String'); fallback value will be used, if available. BindingExpression:Path=PreviewImagePath; DataItem='ClockStyleModel' (HashCode=21536161); target element is 'Image' (Name=''); target property is 'Source' (type 'ImageSource') NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)
   at MS.Internal.Data.TargetDefaultValueConverter.Convert(Object o, Type type, Object parameter, CultureInfo culture)
   at System.Windows.Data.BindingExpression.ConvertHelper(IValueConverter converter, Object value, Type targetType, Object parameter, CultureInfo culture)'
[Navigation] Selecting feature: Pomodoro
[Navigation] Loading content for: Pomodoro
PomodoroViewModel created: True
[NavigationMonitor] SUCCESS: Pomodoro -> PomodoroViewModel
[Navigation] SUCCESS: Pomodoro loaded, Content type: PomodoroViewModel
System.Windows.Data Error: 23 : Cannot convert '' from type 'String' to type 'System.Windows.Media.ImageSource' for 'en-US' culture with default conversions; consider using Converter property of Binding. NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at System.ComponentModel.TypeConverter.GetConvertFromException(Object value)
   at System.ComponentModel.TypeConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at System.Windows.Media.ImageSourceConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)'
System.Windows.Data Error: 6 : 'TargetDefaultValueConverter' converter failed to convert value '' (type 'String'); fallback value will be used, if available. BindingExpression:Path=PreviewImagePath; DataItem='PomodoroStyleModel' (HashCode=21064410); target element is 'Image' (Name=''); target property is 'Source' (type 'ImageSource') NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)
   at MS.Internal.Data.TargetDefaultValueConverter.Convert(Object o, Type type, Object parameter, CultureInfo culture)
   at System.Windows.Data.BindingExpression.ConvertHelper(IValueConverter converter, Object value, Type targetType, Object parameter, CultureInfo culture)'
System.Windows.Data Error: 23 : Cannot convert '' from type 'String' to type 'System.Windows.Media.ImageSource' for 'en-US' culture with default conversions; consider using Converter property of Binding. NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at System.ComponentModel.TypeConverter.GetConvertFromException(Object value)
   at System.ComponentModel.TypeConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at System.Windows.Media.ImageSourceConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)'
System.Windows.Data Error: 6 : 'TargetDefaultValueConverter' converter failed to convert value '' (type 'String'); fallback value will be used, if available. BindingExpression:Path=PreviewImagePath; DataItem='PomodoroStyleModel' (HashCode=39418019); target element is 'Image' (Name=''); target property is 'Source' (type 'ImageSource') NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)
   at MS.Internal.Data.TargetDefaultValueConverter.Convert(Object o, Type type, Object parameter, CultureInfo culture)
   at System.Windows.Data.BindingExpression.ConvertHelper(IValueConverter converter, Object value, Type targetType, Object parameter, CultureInfo culture)'
System.Windows.Data Error: 23 : Cannot convert '' from type 'String' to type 'System.Windows.Media.ImageSource' for 'en-US' culture with default conversions; consider using Converter property of Binding. NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at System.ComponentModel.TypeConverter.GetConvertFromException(Object value)
   at System.ComponentModel.TypeConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at System.Windows.Media.ImageSourceConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)'
System.Windows.Data Error: 6 : 'TargetDefaultValueConverter' converter failed to convert value '' (type 'String'); fallback value will be used, if available. BindingExpression:Path=PreviewImagePath; DataItem='PomodoroStyleModel' (HashCode=48288078); target element is 'Image' (Name=''); target property is 'Source' (type 'ImageSource') NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)
   at MS.Internal.Data.TargetDefaultValueConverter.Convert(Object o, Type type, Object parameter, CultureInfo culture)
   at System.Windows.Data.BindingExpression.ConvertHelper(IValueConverter converter, Object value, Type targetType, Object parameter, CultureInfo culture)'
System.Windows.Data Error: 23 : Cannot convert '' from type 'String' to type 'System.Windows.Media.ImageSource' for 'en-US' culture with default conversions; consider using Converter property of Binding. NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at System.ComponentModel.TypeConverter.GetConvertFromException(Object value)
   at System.ComponentModel.TypeConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at System.Windows.Media.ImageSourceConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)'
System.Windows.Data Error: 6 : 'TargetDefaultValueConverter' converter failed to convert value '' (type 'String'); fallback value will be used, if available. BindingExpression:Path=PreviewImagePath; DataItem='PomodoroStyleModel' (HashCode=11297860); target element is 'Image' (Name=''); target property is 'Source' (type 'ImageSource') NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)
   at MS.Internal.Data.TargetDefaultValueConverter.Convert(Object o, Type type, Object parameter, CultureInfo culture)
   at System.Windows.Data.BindingExpression.ConvertHelper(IValueConverter converter, Object value, Type targetType, Object parameter, CultureInfo culture)'
“ProductivityWallpaper.exe”(CoreCLR: clrhost): 已加载“C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.24\System.Runtime.Serialization.Formatters.dll”。已跳过加载符号。模块进行了优化，并且调试器选项“仅我的代码”已启用。
[Navigation] Selecting feature: Anniversary
[Navigation] Loading content for: Anniversary
AnniversaryViewModel created: True
[NavigationMonitor] SUCCESS: Anniversary -> AnniversaryViewModel
[Navigation] SUCCESS: Anniversary loaded, Content type: AnniversaryViewModel
System.Windows.Data Error: 23 : Cannot convert '' from type 'String' to type 'System.Windows.Media.ImageSource' for 'en-US' culture with default conversions; consider using Converter property of Binding. NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at System.ComponentModel.TypeConverter.GetConvertFromException(Object value)
   at System.ComponentModel.TypeConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at System.Windows.Media.ImageSourceConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)'
System.Windows.Data Error: 6 : 'TargetDefaultValueConverter' converter failed to convert value '' (type 'String'); fallback value will be used, if available. BindingExpression:Path=PreviewImagePath; DataItem='AnniversaryStyleModel' (HashCode=24224733); target element is 'Image' (Name=''); target property is 'Source' (type 'ImageSource') NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)
   at MS.Internal.Data.TargetDefaultValueConverter.Convert(Object o, Type type, Object parameter, CultureInfo culture)
   at System.Windows.Data.BindingExpression.ConvertHelper(IValueConverter converter, Object value, Type targetType, Object parameter, CultureInfo culture)'
System.Windows.Data Error: 23 : Cannot convert '' from type 'String' to type 'System.Windows.Media.ImageSource' for 'en-US' culture with default conversions; consider using Converter property of Binding. NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at System.ComponentModel.TypeConverter.GetConvertFromException(Object value)
   at System.ComponentModel.TypeConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at System.Windows.Media.ImageSourceConverter.ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)'
System.Windows.Data Error: 6 : 'TargetDefaultValueConverter' converter failed to convert value '' (type 'String'); fallback value will be used, if available. BindingExpression:Path=PreviewImagePath; DataItem='AnniversaryStyleModel' (HashCode=37653388); target element is 'Image' (Name=''); target property is 'Source' (type 'ImageSource') NotSupportedException:'System.NotSupportedException: ImageSourceConverter cannot convert from System.String.
   at MS.Internal.Data.DefaultValueConverter.ConvertHelper(Object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, Boolean isForward)
   at MS.Internal.Data.TargetDefaultValueConverter.Convert(Object o, Type type, Object parameter, CultureInfo culture)
   at System.Windows.Data.BindingExpression.ConvertHelper(IValueConverter converter, Object value, Type targetType, Object parameter, CultureInfo culture)'
程序“[25968] ProductivityWallpaper.exe”已退出，返回值为 0 (0x0)。


* 左侧导航
    * 二级菜单勾号是去掉了，但是选中条目需要的高亮色没有设置；
    * 显示滚动条时，避免压缩左侧区域的位置，事先留出空间，但是只有需要时再显示，显示时不要压缩导航按钮宽度；
    * 左侧导航按钮高亮逻辑仍然有问题，请按以下逻辑重新处理：所有导航按钮（包括可展开，不可展开）都在一个逻辑组中，该逻辑组同一时间只能有一个项目处于启用状态，处于启用状态时显示高亮，非启用状态时不显示高亮
    * 二级菜单展开后，导航按钮右侧箭头帽位置偏低，低于按钮文字最底部；

* 页面显示错误
右侧页面显示仍然是错误的！！！目前除了主题预览其他所有页面都无法正常显示！！！仍然未能修复！！！