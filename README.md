# DockCatWin

## 致谢

本项目是 macOS 桌面宠物应用 [DockCat](https://github.com/Auwuua/DockCat) 的原生 Windows 移植版本。所有的原创概念、逻辑设计以及默认的小猫 UI 资源均归原作者所有。非常感谢他们的出色工作！

DockCatWin 是 DockCat 的第一个 Windows 移植版。这个仓库起始于一个极简的 WPF 桌面宠物：

- 透明置顶窗口
- 任务栏边缘定位
- 从 DockCat 复制的默认图像资源
- 走路/休息/过渡动画
- 支持拖拽
- 右键菜单和托盘菜单
- 喝水和活动提醒
- 气泡对话操作
- 出门/专注模式（包含召回、返回事件和可收集奖励）
- 基础设置持久化
- 自定义素材包文件夹（支持回退到默认小猫）
- 自定义走路动画支持使用 PNG 序列帧或绿幕视频源
- 显示器选择和显示变更自动吸附
- 使用统计和本地数据备份

自定义素材包位于：

```text
UserData\AssetPacks
```

DockCatWin 在 `DockCatWin\Resources\MyCat` 下附带了一个名为 `my-cat` 的素材包。启动时，如果该文件夹尚不存在，它将被复制到 `UserData\AssetPacks\my-cat`，以便用户修改本地副本而不会被未来的运行覆盖。*(注：目前 `my-cat` 里的资源是根据我自家的小猫“灰灰”——一只虎斑加白的美短弟弟——采集制作的！ 🐾)*

设置、使用统计和备份位于：

```text
UserData
UserData\DataBackup\user-data-backup.json
```

出门收集品清单存储在：

```text
UserData\collectable-inventory.json
```

<details>
<summary>🎨 <strong>高级：创建自定义素材包</strong></summary>

## 自定义走路视频

自定义素材包可以在以下路径提供走路 PNG 序列帧：

```text
animations\walk\walk_01.png
animations\walk\walk_02.png
...
```

或者提供绿幕走路视频，让 DockCatWin 提取透明 PNG 帧到本地缓存：

```json
"animations": {
  "walk": {
    "fps": 3,
    "video": "animations/walk/walk.mp4",
    "video_frame_count": 4,
    "frames": []
  }
}
```

PNG 序列帧优先级更高。如果没有找到走路 PNG 序列帧，DockCatWin 会读取 `video`，提取 `video_frame_count` 个均匀分布的帧，去除纯绿幕背景，将小猫规范化为居中底部对齐的 `1100 x 650` 透明帧，并将生成的 PNG 文件缓存到 `UserData\VideoCache` 下。

绿幕抠图使用的是基础色度键：纯净且光照均匀的绿幕效果最好。它会使深绿色完全透明，平滑浅绿边缘，并减少半透明边缘的溢色。复杂的背景、厚重的阴影、毛发上的绿色反射或与背景色过近的细节可能仍需要手动清理。

## 自定义抓起尺寸

拖拽/抓起姿态可以使用其自己的显示画布尺寸。当 `poses\held\held.png` 比默认的窄型抓起素材更宽，否则会显得太小时，这很有用。

```json
"display_sizes": {
  "held": { "width": 650, "height": 1236 }
}
```

省略时，DockCatWin 将在抓起状态下使用素材包正常的 `canvas_width` 和 `canvas_height`格式。
</details>

<details>
<summary>💻 <strong>开发者指南（从源码构建）</strong></summary>

## 运行要求

- Windows 10 或 Windows 11
- .NET 8 SDK

## 运行

```powershell
dotnet run --project .\DockCatWin\DockCatWin.csproj
```

## 构建

```powershell
dotnet build .\DockCatWin\DockCatWin.csproj
```

## 发布

```powershell
.\scripts\publish-win.ps1
```

发布输出路径为：

```text
artifacts\DockCatWin
```

输出文件是完全便携且自包含的。你可以直接打包 `artifacts\DockCatWin` 文件夹并分享；用户不需要安装 .NET 运行时。

请把发行版解压到桌面、下载、文档等当前用户可写的位置。DockCatWin 会在程序同目录创建 `UserData` 保存设置、素材包和存档；如果放在 `Program Files` 等无写权限目录，设置保存可能失败。
</details>
