# DockCatWin

DockCatWin 是 macOS 桌面宠物应用 [DockCat](https://github.com/Auwuua/DockCat) 的原生 Windows 移植版本。原创概念、行为设计以及默认栗子资源均来自 DockCat 项目。

## 版本对应

DockCatWin 基于 DockCat `0.4` 移植。本地用于移植参考的上游工程 `MARKETING_VERSION = 0.4`，参考发布标签是 `v0.4`，本地参考提交是 `29a39d6 Add troubleshooting guide`。

DockCatWin 目前不保证完全包含 DockCat `v0.4.2` 之后新增的内容，例如 `v0.4.2` 为栗子新增的几张休息姿态。

## 已移植的 DockCat 功能

- 透明置顶桌面小猫窗口。
- 贴近任务栏边缘活动。
- 默认栗子资源包与资源回退机制。
- 走路、休息、过渡、抱起、对话等状态。
- 鼠标拖拽、右键菜单和系统托盘菜单。
- 喝水提醒和活动提醒。
- 气泡对话与提醒操作。
- 出门/专注模式，包括召回、返回事件和可收集奖励。
- 自定义小猫资源包。
- 使用统计和本地数据备份。

## DockCatWin 新增或调整的功能

- Windows WPF 原生实现。
- 多显示器选择，以及显示器变化后的自动吸附。
- 便携式用户数据：设置、素材包和存档保存在程序同目录的 `UserData`。
- 隐藏小猫后，可通过系统托盘菜单重新显示。
- 显示缩放范围为 `4%` 到 `100%`。
- 抓起姿态可单独设置显示尺寸，适合更大的自定义抱起素材。
- 可选支持通过用户已安装的 `ffmpeg` 从绿幕视频抽取走路素材帧。
- 附带可选示例资源包 `huihui-cat`，但默认启动不使用它。

程序默认启动使用 `default-lizz`。首次启动时，DockCatWin 也会创建 `UserData\AssetPacks\my-cat` 作为空模板包，方便用户放入自己的猫猫素材。

## 资源包

用户可编辑资源包位于：

```text
UserData\AssetPacks
```

首次启动会尽量准备这些入口：

- `default-lizz`：默认 DockCat 栗子资源包副本，便于参考。
- `huihui-cat`：可选示例资源包。
- `my-cat`：空模板资源包，供用户放入自己的猫猫图片。

内置资源包只会在目标文件夹不存在时复制，因此用户修改本地副本后，不会被后续运行覆盖。

完整资源包格式见 [ASSET_PACK_GUIDE.md](ASSET_PACK_GUIDE.md)。

## 视频素材流程

DockCatWin 不再捆绑 OpenCV，这样发布包不会额外携带大型 OpenCV DLL。

作为相对原版 DockCat 的 Windows 侧补充能力，自定义资源包可以在 manifest 里指向一段绿幕 `walk.mp4`。如果用户电脑的 `PATH` 中能找到 `ffmpeg` 和 `ffprobe`，DockCatWin 会从视频中均匀抽取走路帧、去除绿色背景、归一化为 `1100 x 650`，并把透明 PNG 缓存到：

```text
UserData\VideoCache
```

对外分享资源包时，仍建议直接提供透明 PNG 走路序列帧；这样不依赖外部工具，稳定性最好。

## 用户数据

DockCatWin 是便携式应用。设置、素材包、统计、收藏品和备份保存在程序同目录：

```text
UserData
UserData\AssetPacks
UserData\DataBackup\user-data-backup.json
UserData\collectable-inventory.json
```

请把发行版解压到桌面、下载、文档等当前用户可写的位置。若放入 `Program Files` 等无写权限目录，设置保存可能失败。

## 许可证

DockCatWin 沿用上游 DockCat 的许可证，见 [LICENSE](LICENSE)。发布包中也会包含这份许可证。

## 开发

运行要求：

- Windows 10 或 Windows 11
- .NET 8 SDK

从源码运行：

```powershell
dotnet run --project .\DockCatWin\DockCatWin.csproj
```

构建：

```powershell
dotnet build .\DockCatWin\DockCatWin.csproj
```

发布便携式自包含 Windows 版本：

```powershell
.\scripts\publish-win.ps1
```

发布输出路径：

```text
artifacts\DockCatWin
```

发布脚本会把 `README.md`、`LICENSE` 和 `ASSET_PACK_GUIDE.md` 一起复制到发行版文件夹。
