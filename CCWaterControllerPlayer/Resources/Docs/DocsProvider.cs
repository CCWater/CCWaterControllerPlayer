namespace CCWaterControllerPlayer.Resources.Docs;

public static class DocsProvider
{
    public static string GetDoc(string key, string lang)
    {
        return (key, lang) switch
        {
            ("quickstart", "zh") => QuickStart_ZH,
            ("quickstart", _) => QuickStart_EN,
            ("features", "zh") => Features_ZH,
            ("features", _) => Features_EN,
            ("recording", "zh") => Recording_ZH,
            ("recording", _) => Recording_EN,
            ("visualization", "zh") => Visualization_ZH,
            ("visualization", _) => Visualization_EN,
            ("overlay", "zh") => Overlay_ZH,
            ("overlay", _) => Overlay_EN,
            ("faq", "zh") => FAQ_ZH,
            ("faq", _) => FAQ_EN,
            _ => lang == "zh" ? "文档未找到" : "Document not found"
        };
    }

    #region Quick Start

    private const string QuickStart_EN = @"
## Quick Start Guide

### 1. Connect Your Controller
- Plug in your Xbox or PlayStation controller via USB or Bluetooth
- Click the ""Connect"" button in the sidebar
- The status indicator will turn green when connected
- The detected polling rate will be displayed

### 2. Start Monitoring
- Navigate to the ""Monitor"" tab
- You'll see real-time 2D trajectory and time series graphs for both sticks
- The left panel shows the left stick, the right panel shows the right stick

### 3. Record a Track
- Set your game and weapon in Settings before recording
- Configure the trigger button (default: Right Trigger / R2)
- When you press the trigger in-game, the app automatically records:
  - 500ms BEFORE the trigger press (pre-buffer)
  - The entire duration while trigger is held
  - 500ms AFTER trigger release (post-buffer)
- Recordings are automatically saved to the local database

### 4. Compare Tracks
- Go to the ""Records"" tab
- Select multiple recordings to compare
- Click ""Compare"" to overlay them on the same graph
- Use playback controls to animate the comparison

### 5. Use the Overlay
- Enable the overlay in Settings
- A transparent window will appear on top of your game
- It shows your real-time stick movement with optional history overlay
";

    private const string QuickStart_ZH = @"
## 快速开始指南

### 1. 连接手柄
- 通过USB或蓝牙连接你的Xbox或PlayStation手柄
- 点击侧边栏的""连接""按钮
- 连接成功后状态指示灯变绿
- 检测到的轮询率会显示在侧边栏

### 2. 开始监控
- 切换到""实时监控""标签页
- 你会看到左右摇杆的实时2D轨迹图和时间序列图
- 左侧面板显示左摇杆，右侧面板显示右摇杆

### 3. 录制轨迹
- 录制前在设置中配置游戏名称和武器名称
- 配置触发按键（默认：右扳机 RT / R2）
- 在游戏中按下扳机时，程序自动录制：
  - 按下前500ms的数据（预缓冲）
  - 按住期间的全部数据
  - 松开后500ms的数据（后缓冲）
- 录制结果自动保存到本地数据库

### 4. 对比轨迹
- 进入""录制记录""标签页
- 选择多条录制记录进行对比
- 点击""对比""将它们叠加在同一图表上
- 使用回放控制来动画展示对比过程

### 5. 使用悬浮窗
- 在设置中启用悬浮窗
- 一个透明窗口会显示在游戏画面上方
- 它会显示实时摇杆轨迹，可选叠加历史轨迹作为参考
";

    #endregion

    #region Features

    private const string Features_EN = @"
## Feature Overview

### Controller Support
- **Xbox Controllers**: Full support via XInput API (Xbox One, Xbox Series, Xbox 360)
- **PlayStation Controllers**: Support via DirectInput (DualShock 4, DualSense)
- **Auto-detection**: Automatically detects controller type and polling rate
- **Multi-controller**: Supports up to 4 Xbox controllers simultaneously

### Input Monitoring
- **Dual stick tracking**: Both left and right sticks monitored simultaneously
- **Full state capture**: Sticks, triggers, and all buttons recorded
- **Three performance levels**: Low (50Hz), Medium (1000Hz), High (8000Hz) - balance precision vs CPU usage
- **Auto-detect rate**: Measures actual polling rate of your controller

### Recording System
- **Ring buffer**: Continuously stores the last N seconds of input
- **Configurable trigger**: Single button, combo, or threshold-based activation
- **Pre/Post buffer**: Captures data before and after the trigger event
- **Merge mode**: Consecutive triggers within a time window merge into one recording
- **Game/Weapon tagging**: Organize recordings by game and weapon

### Visualization
- **2D Trajectory**: XY plot showing stick movement path with time-based color gradient
- **Time Series**: Separate X and Y axis plots over time (oscilloscope-style)
- **Multi-track overlay**: Compare unlimited tracks simultaneously
- **Playback**: Animate recordings with adjustable speed (0.25x - 4x)

### Overlay
- **Transparent mode**: Click-through overlay on top of games
- **Independent mode**: Separate always-on-top window
- **Real-time display**: Shows current stick movement
- **History overlay**: Overlays selected historical tracks for reference
- **Draggable & resizable**: Position and size the overlay as needed

### Image Overlay
- **Built-in recoil images**: Default ammo-type recoil pattern reference images
- **Left/Right navigation**: Browse through images with arrow buttons
- **Custom images**: Drag-and-drop or select your own reference images
- **Pin mode**: Pin to make click-through, navigation buttons hidden when pinned
- **State persistence**: Remembers selected image and visibility across sessions

### Data Management
- **Local SQLite storage**: All data stored locally, no server needed
- **Game/Weapon categorization**: Filter and organize by game and weapon
- **Binary serialization**: Efficient storage of high-frequency data
- **Export-ready**: Data structure supports future CSV/JSON export
";

    private const string Features_ZH = @"
## 功能概览

### 手柄支持
- **Xbox手柄**：通过XInput API完整支持（Xbox One、Xbox Series、Xbox 360）
- **PlayStation手柄**：通过DirectInput支持（DualShock 4、DualSense）
- **自动检测**：自动识别手柄类型和轮询率
- **多手柄**：最多同时支持4个Xbox手柄

### 输入监控
- **双摇杆追踪**：左右摇杆同时监控
- **完整状态捕获**：摇杆、扳机和所有按键均被记录
- **三档采样性能**：低(50Hz)/中(1000Hz)/高(8000Hz)，按需平衡精度与CPU占用
- **自动检测采样率**：测量手柄的实际轮询频率

### 录制系统
- **环形缓冲区**：持续存储最近N秒的输入数据
- **可配置触发**：支持单键、组合键或阈值触发
- **前后缓冲**：捕获触发事件前后的数据
- **合并模式**：时间窗口内的连续触发合并为一条记录
- **游戏/武器标签**：按游戏和武器组织录制记录

### 可视化
- **2D轨迹图**：XY坐标图显示摇杆移动路径，颜色渐变表示时间
- **时间序列图**：X和Y轴分别随时间变化的曲线（示波器风格）
- **多轨迹叠加**：同时对比无限数量的轨迹
- **回放功能**：可调速度动画回放（0.25x - 4x）

### 悬浮窗
- **透明模式**：点击穿透的游戏内覆盖层
- **独立模式**：独立的置顶窗口
- **实时显示**：显示当前摇杆移动
- **历史叠加**：叠加选定的历史轨迹作为参考
- **可拖拽调整**：自由调整位置和大小

### 图片悬浮窗
- **内置后坐力图**：默认包含弹药类型后坐力参考图
- **左右切换**：通过箭头按钮浏览图片
- **自选图片**：拖拽或点击选择自定义参考图片
- **固定模式**：固定后点击穿透，导航按钮隐藏
- **状态记忆**：记住选择的图片和显示状态，下次启动自动恢复

### 数据管理
- **本地SQLite存储**：所有数据本地存储，无需服务器
- **游戏/武器分类**：按游戏和武器筛选和组织
- **二进制序列化**：高效存储高频数据
- **可导出**：数据结构支持未来的CSV/JSON导出
";

    #endregion

    #region Recording

    private const string Recording_EN = @"
## Recording System Details

### How Recording Works

The recording system uses a **ring buffer** that continuously stores controller input. 
When a trigger event is detected, the system:

1. **Pre-trigger capture**: Extracts the last N milliseconds from the ring buffer
2. **Active recording**: Records all input while the trigger condition is active
3. **Post-trigger capture**: Continues recording for N milliseconds after trigger release

### Trigger Configuration

#### Single Button Mode (Default)
- Default trigger: Right Trigger (RT / R2)
- Activates when the trigger value exceeds the threshold (default: 10%)
- Best for FPS games where shooting = pulling the trigger

#### Combo Button Mode
- Requires multiple buttons pressed simultaneously
- Example: LB + RB to manually mark recording start/end
- Useful when you want manual control over recording

#### Threshold Mode
- Triggers when stick magnitude exceeds a threshold
- Useful for detecting any significant stick movement
- Checks both left and right sticks

### Merge Behavior

When ""Merge consecutive triggers"" is enabled:
- If a new trigger occurs within the merge window (default: 500ms) after the previous trigger ends
- The two recordings are merged into a single continuous recording
- This is useful for burst-fire weapons or tap-firing patterns

When disabled:
- Each trigger press creates a separate recording
- Post-trigger of the first and pre-trigger of the second may overlap in time

### Timing Parameters
- **Pre-trigger (ms)**: How much data before the trigger to include (default: 500ms)
- **Post-trigger (ms)**: How long to continue recording after release (default: 500ms)
- **Merge window (ms)**: Time window for merging consecutive triggers (default: 500ms)
- **Ring buffer (seconds)**: Continuous input buffer size (default: 5s)

### Data Stored Per Recording
- Timestamp (high-resolution ticks)
- Left stick X/Y position (-1.0 to 1.0)
- Right stick X/Y position (-1.0 to 1.0)
- Left/Right trigger values (0.0 to 1.0)
- Button states (bitmask)
- Sampling rate at time of recording
- Game name and weapon name tags
";

    private const string Recording_ZH = @"
## 录制系统详解

### 录制工作原理

录制系统使用**环形缓冲区**持续存储手柄输入。
当检测到触发事件时，系统会：

1. **触发前捕获**：从环形缓冲区中提取最近N毫秒的数据
2. **主动录制**：在触发条件激活期间录制所有输入
3. **触发后捕获**：触发释放后继续录制N毫秒

### 触发配置

#### 单键模式（默认）
- 默认触发键：右扳机（RT / R2）
- 当扳机值超过阈值时激活（默认：10%）
- 最适合FPS游戏中射击=扣扳机的场景

#### 组合键模式
- 需要同时按下多个按键
- 示例：LB + RB 手动标记录制起止点
- 适合需要手动控制录制的场景

#### 阈值模式
- 当摇杆幅度超过阈值时触发
- 适合检测任何显著的摇杆移动
- 同时检查左右摇杆

### 合并行为

启用""合并连续触发""时：
- 如果新的触发在上一次触发结束后的合并窗口内（默认：500ms）发生
- 两次录制会合并为一条连续的录制
- 这对连发武器或点射模式非常有用

禁用时：
- 每次扳机按下都会创建独立的录制
- 第一次的后缓冲和第二次的前缓冲在时间上可能重叠

### 时间参数
- **触发前时长(ms)**：触发前包含多少数据（默认：500ms）
- **触发后时长(ms)**：释放后继续录制多长时间（默认：500ms）
- **合并窗口 (ms)**：合并连续触发的时间窗口（默认：500ms）
- **环形缓冲区(秒)**：持续输入缓冲区大小（默认：5秒）

### 每条录制存储的数据
- 时间戳（高精度Ticks）
- 左摇杆 X/Y 位置（-1.0 到 1.0）
- 右摇杆 X/Y 位置（-1.0 到 1.0）
- 左/右扳机值（0.0 到 1.0）
- 按键状态（位掩码）
- 录制时的采样率
- 游戏名称和武器名称标签
";

    #endregion

    #region Visualization

    private const string Visualization_EN = @"
## Visualization Guide

### 2D Trajectory View

The 2D trajectory view shows stick movement as a path on an XY coordinate plane:

- **X axis**: Horizontal stick deflection (-1 = full left, +1 = full right)
- **Y axis**: Vertical stick deflection (-1 = full down, +1 = full up)
- **Center point**: Stick at rest position (0, 0)
- **Circle boundary**: Maximum stick deflection radius
- **Grid lines**: Quarter divisions for reference
- **Crosshair**: Center reference lines

The drawing area maintains a 1:1 aspect ratio to ensure circular inputs appear as circles.

#### Color Coding
- Each track in comparison mode gets a unique color
- The current position is shown as a larger dot at the end of the path
- History tracks are drawn with reduced opacity

### Time Series View

The time series view shows stick values over time (like an oscilloscope):

- **Horizontal axis**: Time (scrolling window)
- **Vertical axis**: Stick value (-1.0 to +1.0)
- **Zero line**: Center reference (stick at rest)
- **X and Y shown separately**: Each axis gets its own graph

This view is useful for:
- Seeing the timing of your inputs
- Identifying patterns in your recoil control
- Comparing the smoothness of different attempts

### Multi-Track Comparison

When comparing multiple tracks:
- Each track is assigned a distinct color
- All tracks are time-aligned to their trigger point
- You can overlay them on the same graph or view side-by-side
- Playback animates all tracks simultaneously
- Speed control: 0.25x (slow-mo) to 4x (fast-forward)

### Reading the Graphs for Recoil Control

For FPS recoil control analysis:
- **Ideal pattern**: Smooth, consistent downward pull on the right stick Y axis
- **Good control**: Tracks that closely follow the same path each time
- **Inconsistency**: Large variation between tracks indicates inconsistent muscle memory
- **Over-correction**: Spikes or sudden direction changes
- **Starting point variation**: Different starting positions show the ""no dead zone"" challenge
";

    private const string Visualization_ZH = @"
## 可视化指南

### 2D轨迹视图

2D轨迹视图在XY坐标平面上显示摇杆移动路径：

- **X轴**：水平摇杆偏移（-1 = 最左，+1 = 最右）
- **Y轴**：垂直摇杆偏移（-1 = 最下，+1 = 最上）
- **中心点**：摇杆静止位置 (0, 0)
- **圆形边界**：最大摇杆偏移半径
- **网格线**：四分之一刻度参考线
- **十字线**：中心参考线

绘制区域保持1:1宽高比，确保圆形输入显示为圆形。

#### 颜色编码
- 对比模式中每条轨迹使用不同颜色
- 当前位置在路径末端显示为较大的圆点
- 历史轨迹以降低的透明度绘制

### 时间序列视图

时间序列视图显示摇杆值随时间的变化（类似示波器）：

- **水平轴**：时间（滚动窗口）
- **垂直轴**：摇杆值（-1.0 到 +1.0）
- **零线**：中心参考（摇杆静止）
- **X和Y分别显示**：每个轴有独立的图表

此视图适用于：
- 查看输入的时序
- 识别压枪控制中的模式
- 比较不同尝试的平滑度

### 多轨迹对比

对比多条轨迹时：
- 每条轨迹分配不同的颜色
- 所有轨迹按触发点对齐时间
- 可以叠加在同一图表上或并排查看
- 回放时所有轨迹同步动画
- 速度控制：0.25x（慢动作）到 4x（快进）

### 用图表分析压枪控制

FPS压枪控制分析：
- **理想模式**：右摇杆Y轴平滑、一致的向下拉动
- **良好控制**：每次轨迹都紧密跟随相同路径
- **不一致性**：轨迹间差异大说明肌肉记忆不稳定
- **过度修正**：突然的尖峰或方向变化
- **起始点差异**：不同的起始位置体现了""无死区""的挑战
";

    #endregion

    #region Overlay

    private const string Overlay_EN = @"
## Overlay Guide

### Overlay Modes

#### Transparent Mode
- The overlay renders as a transparent, always-on-top window
- It sits directly over your game
- Click-through enabled: mouse clicks pass through to the game
- Best for: Fullscreen windowed or borderless windowed games

#### Independent Mode
- A separate always-on-top window
- Not click-through (you can interact with it)
- Best for: When you have a second monitor, or during practice sessions
- Can be moved to a second display

### Overlay Content

The overlay displays:
1. **Real-time track**: Your current stick movement drawn in real-time
2. **History overlay**: Previously selected tracks shown as reference
3. **Grid and crosshair**: For spatial reference

### Configuration
- **Position**: Drag the title bar to reposition
- **Size**: Use the resize grip at the bottom-right corner
- **Opacity**: Adjust in Settings (10% - 100%)
- **Show/Hide**: Toggle via Settings or the close button (hides, doesn't exit)

### Anti-Cheat Considerations

Warning: The transparent overlay mode uses standard Windows APIs (WS_EX_TRANSPARENT, WS_EX_TOPMOST). While this is a legitimate technique used by many applications:

- Some anti-cheat systems may flag overlay windows
- This tool does NOT inject into game processes
- This tool does NOT read game memory
- This tool only reads controller input via standard APIs
- Use at your own discretion in competitive environments
- For ranked/competitive play, consider using Independent mode on a second monitor

### Tips for Effective Use
- Position the overlay where it won't obstruct critical game UI
- Use low opacity (30-50%) to keep it subtle
- Load a ""good"" recording as history overlay to use as a reference target
- The real-time track helps you see if you're matching the reference
";

    private const string Overlay_ZH = @"
## 悬浮窗指南

### 悬浮窗模式

#### 透明模式
- 悬浮窗渲染为透明的置顶窗口
- 直接覆盖在游戏画面上
- 启用点击穿透：鼠标点击会传递到游戏
- 最适合：全屏窗口化或无边框窗口化游戏

#### 独立模式
- 独立的置顶窗口
- 不穿透点击（可以与之交互）
- 最适合：有第二显示器时，或练习时使用
- 可以移动到第二个显示器

### 悬浮窗内容

悬浮窗显示：
1. **实时轨迹**：当前摇杆移动的实时绘制
2. **历史叠加**：之前选择的轨迹作为参考显示
3. **网格和十字线**：空间参考

### 配置
- **位置**：拖拽标题栏重新定位
- **大小**：使用右下角的调整手柄
- **透明度**：在设置中调整（10% - 100%）
- **显示/隐藏**：通过设置或关闭按钮切换（隐藏，不退出）

### 反作弊注意事项

注意：透明悬浮窗模式使用标准Windows API（WS_EX_TRANSPARENT, WS_EX_TOPMOST）。虽然这是许多应用程序使用的合法技术：

- 某些反作弊系统可能会标记悬浮窗
- 本工具不会注入游戏进程
- 本工具不会读取游戏内存
- 本工具仅通过标准API读取手柄输入
- 在竞技环境中请自行判断是否使用
- 对于排位/竞技游戏，建议使用独立模式配合第二显示器

### 有效使用技巧
- 将悬浮窗放在不会遮挡关键游戏UI的位置
- 使用低透明度（30-50%）保持低调
- 加载一条""好的""录制作为历史叠加，用作参考目标
- 实时轨迹帮助你看到是否在匹配参考轨迹
";

    #endregion

    #region FAQ

    private const string FAQ_EN = @"
## Frequently Asked Questions

### Q: My controller is not detected?
**A:** Try the following:
1. Ensure the controller is properly connected (USB or paired via Bluetooth)
2. Check if Windows recognizes the controller in ""Devices and Printers""
3. For PlayStation controllers, you may need DS4Windows or similar driver
4. Try disconnecting and reconnecting the controller
5. Restart the application

### Q: The sampling rate seems low?
**A:** The app offers three sampling performance levels in Settings:
- **Low (50Hz)**: Uses Task.Delay, minimal CPU usage, suitable for basic track drawing
- **Medium (1000Hz)**: Uses hybrid sleep + spin-wait, moderate CPU usage
- **High (8000Hz)**: Uses pure spin-wait, saturates one CPU core for maximum precision

The actual achieved rate also depends on:
- Your controller's hardware polling rate
- USB connection type (USB is generally faster than Bluetooth)
- System load and other running applications

For best results, use a wired USB connection and Medium or High performance level.

### Q: Can I use this with any game?
**A:** Yes! This tool reads controller input at the system level, independent of any game. It works with any game that accepts controller input. The tool does not interact with game processes in any way.

### Q: Will this get me banned?
**A:** This tool:
- Does NOT inject code into games
- Does NOT modify game memory
- Does NOT send fake inputs
- Only READS your actual controller input
- Is purely an analysis/training tool

However, some anti-cheat systems may flag overlay windows. Use the independent window mode if concerned.

### Q: How much disk space do recordings use?
**A:** Each sample is approximately 28 bytes. At 1000Hz (Medium) sampling:
- 1 second = 28 KB
- A typical 4-second recording = 112 KB
- 1000 recordings ≈ 112 MB

At 50Hz (Low) sampling:
- 1 second = 1.4 KB
- A typical 4-second recording = 5.6 KB
- 1000 recordings ≈ 5.6 MB

Storage is very efficient thanks to binary serialization.

### Q: Can I export my data?
**A:** Currently data is stored in SQLite format. Future versions will support CSV and JSON export. You can also access the database directly at:
`%LOCALAPPDATA%\CCWaterControllerPlayer\data.db`

### Q: Why do my tracks look different each time?
**A:** This is exactly the problem this tool helps you understand! With analog sticks:
- There's no fixed starting point (especially with no dead zone)
- Muscle memory varies between attempts
- Stick tension and wear affect consistency
- Your grip position may shift slightly

The goal is to use this tool to identify and reduce these variations through practice.
";

    private const string FAQ_ZH = @"
## 常见问题

### Q: 手柄未被检测到？
**A:** 请尝试以下步骤：
1. 确保手柄正确连接（USB或蓝牙配对）
2. 在Windows""设备和打印机""中检查是否识别到手柄
3. PlayStation手柄可能需要DS4Windows或类似驱动
4. 尝试断开并重新连接手柄
5. 重启应用程序

### Q: 采样率似乎很低？
**A:** 应用在设置中提供三档采样性能：
- **低 (50Hz)**：使用Task.Delay，CPU占用极低，适合基本轨迹绘制
- **中 (1000Hz)**：使用混合休眠+自旋等待，CPU占用适中
- **高 (8000Hz)**：使用纯自旋等待，满载单核以获得极致精度

实际达到的采样率还取决于：
- 手柄硬件的轮询率
- USB连接类型（USB通常比蓝牙快）
- 系统负载和其他运行的应用程序

为获得最佳效果，请使用有线USB连接并选择中或高性能档位。

### Q: 可以配合任何游戏使用吗？
**A:** 是的！本工具在系统层面读取手柄输入，独立于任何游戏。它适用于任何接受手柄输入的游戏。本工具不会以任何方式与游戏进程交互。

### Q: 会导致封号吗？
**A:** 本工具：
- 不会注入代码到游戏中
- 不会修改游戏内存
- 不会发送虚假输入
- 仅读取你的实际手柄输入
- 纯粹是分析/训练工具

但是，某些反作弊系统可能会标记悬浮窗。如有顾虑，请使用独立窗口模式。

### Q: 录制占用多少磁盘空间？
**A:** 每个采样点约28字节。以1000Hz（中档）采样率计算：
- 1秒 = 28 KB
- 典型的4秒录制 = 112 KB
- 1000条录制 ≈ 112 MB

以50Hz（低档）采样率计算：
- 1秒 = 1.4 KB
- 典型的4秒录制 = 5.6 KB
- 1000条录制 ≈ 5.6 MB

得益于二进制序列化，存储非常高效。

### Q: 可以导出数据吗？
**A:** 目前数据以SQLite格式存储。未来版本将支持CSV和JSON导出。你也可以直接访问数据库：
`%LOCALAPPDATA%\CCWaterControllerPlayer\data.db`

### Q: 为什么每次轨迹看起来都不一样？
**A:** 这正是本工具帮助你理解的问题！使用模拟摇杆时：
- 没有固定的起始点（特别是无死区设置下）
- 肌肉记忆在每次尝试间有差异
- 摇杆张力和磨损影响一致性
- 握持位置可能会轻微偏移

目标是使用本工具识别并通过练习减少这些差异。
";

    #endregion
}
