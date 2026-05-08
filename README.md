# CCWater Controller Player

手柄轨迹分析工具 —— 用于 FPS 游戏压枪训练与摇杆输入可视化分析。

## 功能特性

- **实时监控** - 左右摇杆 2D 轨迹图 + 时间序列图，实时显示手柄输入状态
- **自动录制** - 基于扳机/按键触发的环形缓冲录制，自动捕获开枪前后的摇杆数据
- **后坐力分析** - 三色分段轨迹（开枪前/开枪中/开枪后），直观展示压枪效果
- **多轨迹对比** - 叠加多条录制记录，回放动画对比，发现操作差异
- **悬浮窗** - 透明置顶悬浮窗，游戏内实时显示摇杆轨迹与历史参考
- **图片悬浮窗** - 独立图片悬浮窗，用于展示枪械后坐力图案作为参考
- **高频采样** - 支持最高 8000Hz 目标采样率，精确捕获每一帧输入
- **本地存储** - SQLite 本地数据库，数据完全离线，无需联网

## 支持的手柄

| 手柄类型 | 接口 | 说明 |
|---------|------|------|
| Xbox One / Series / 360 | XInput | 完整支持，最多 4 个手柄 |
| DualShock 4 / DualSense | DirectInput | 通过 DirectInput 支持 |

## 系统要求

- Windows 7 及以上（推荐 Windows 10/11）
- .NET 8.0 Runtime
- USB 或蓝牙连接的手柄

## 快速开始

### 从 Release 下载

前往 [Releases](../../releases) 页面下载最新的独立可执行文件，解压后直接运行。

### 从源码构建

```bash
git clone https://github.com/CCWater/CCWaterControllerPlayer.git
cd CCWaterControllerPlayer
dotnet build
dotnet run --project CCWaterControllerPlayer
```

### 发布独立可执行文件

```bash
dotnet publish CCWaterControllerPlayer/CCWaterControllerPlayer.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

输出位于 `CCWaterControllerPlayer/bin/Release/net8.0-windows7.0/win-x64/publish/`

## 使用说明

### 1. 连接手柄
- 通过 USB 或蓝牙连接手柄
- 点击侧边栏「连接」按钮
- 状态指示灯变绿表示连接成功

### 2. 实时监控
- 切换到「实时监控」页面
- 查看左右摇杆的 2D 轨迹和时间序列图

### 3. 录制轨迹
- 在设置中配置触发按键（默认：右扳机 RT）
- 游戏中按下扳机时自动录制前后数据
- 录制结果自动保存

### 4. 分析对比
- 在「录制记录」页面选择多条记录
- 查看三色分段后坐力轨迹
- 使用回放控制动画对比

### 5. 悬浮窗
- 在设置中启用悬浮窗
- 支持实时摇杆轨迹 + 后坐力分析面板
- 可固定（点击穿透）用于游戏内参考

## 项目结构

```
CCWaterControllerPlayer/
├── Models/          # 数据模型（设置、输入、录制记录）
├── ViewModels/      # MVVM ViewModel 层
├── Views/           # WPF 视图与自定义控件
├── Overlay/         # 悬浮窗（主悬浮窗 + 图片悬浮窗）
├── Services/        # 服务层（手柄、录制、数据库、设置）
├── Helpers/         # 工具类（环形缓冲区）
└── Resources/       # 资源（本地化、文档、主题）
```

## 技术栈

- **框架**: WPF (.NET 8)
- **架构**: MVVM (CommunityToolkit.Mvvm)
- **手柄输入**: SharpDX.XInput + SharpDX.DirectInput
- **数据存储**: Microsoft.Data.Sqlite
- **依赖注入**: Microsoft.Extensions.DependencyInjection

## 数据存储

应用数据存储在本地：
- 设置文件: `%LOCALAPPDATA%\CCWaterControllerPlayer\settings.json`
- 数据库: `%LOCALAPPDATA%\CCWaterControllerPlayer\data.db`

## 安全声明

本工具：
- **不会**注入任何游戏进程
- **不会**读取或修改游戏内存
- **不会**发送虚假输入
- **仅**通过标准系统 API 读取手柄输入
- 是纯粹的分析/训练辅助工具

## 许可证

[MIT License](LICENSE)

## 贡献

欢迎提交 Issue 和 Pull Request！

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/新功能`)
3. 提交更改 (`git commit -m '添加新功能'`)
4. 推送到分支 (`git push origin feature/新功能`)
5. 创建 Pull Request
