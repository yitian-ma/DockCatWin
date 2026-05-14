# DockCatWin

DockCatWin 是 [DockCat](https://github.com/Auwuua/DockCat) 的 Windows 移植版本，让小猫也可以住在 Windows 桌面边缘。

它会贴着任务栏休息、伸懒腰、走来走去，也会提醒你喝水、起身活动。你可以拖动小猫，把它放到喜欢的位置；也可以让它出门玩一会儿，等它回来时听听见闻，或者收下它带回来的小礼物。

DockCatWin 的原创概念、行为设计以及默认栗子资源均来自 DockCat 项目。当前移植基于 DockCat `v0.4`，默认栗子的休息姿态资源和伸懒腰/打哈欠出现频率已同步到 DockCat `v0.4.2`。

当前版本面向 Windows 10 和 Windows 11。

## 与 DockCat 的关系

DockCatWin 已移植 DockCat 的核心体验：

- 小猫会休息、散步、伸懒腰、打哈欠、被抱起和面向你对话。
- 支持喝水提醒、起身活动提醒，以及出门/专注模式。
- 支持自定义小猫资源包；不完整的资源包会自动回退到默认栗子。
- 会记录本地使用统计和小猫出门带回的收藏品。

DockCatWin 也针对 Windows 做了一些补充：

- 小猫贴近 Windows 任务栏活动，并支持系统托盘菜单。
- 隐藏小猫后，可以从系统托盘菜单重新显示。
- 可从菜单中手动切换散步和休息状态。
- 设置、资源包和存档默认保存在程序同目录的 `UserData`，方便便携使用和备份。
- 资源包可为抱起姿态单独设置显示尺寸。
- 自定义资源包可选使用本机已安装的 `ffmpeg` 从绿幕视频生成走路序列帧。

当前暂未包含 DockCat `v0.4.2` 新增的英文界面切换；DockCatWin 的界面和文档仍以中文为主。

## 快速使用

如果你想直接使用，推荐下载 GitHub Releases 中的便携版压缩包。

1. 打开本仓库的 [Releases](https://github.com/yitian-ma/DockCatWin/releases) 页面。
2. 下载最新版本的 DockCatWin 压缩包。
3. 解压到桌面、下载、文档等当前用户可写的位置。
4. 运行 `DockCatWin.exe`。
5. 如果 Windows SmartScreen 提示未知发布者，请确认来源后选择继续运行。

请不要把 DockCatWin 解压到 `Program Files` 等通常需要管理员权限的位置。DockCatWin 会在程序同目录保存设置、资源包和存档，放在无写权限目录可能导致设置无法保存。

## 使用指引

- 启动后，小猫会出现在任务栏附近。
- 右键点击小猫，或使用系统托盘图标，可以打开菜单。
- 设置中可修改小猫名字、对你的称呼、显示器、显示缩放、提醒间隔、状态时长和默认出门时间。
- 隐藏小猫后，可以通过系统托盘菜单重新显示。
- 支持多显示器；更换显示器或显示器布局变化后，小猫会尽量重新吸附到可用区域。
- 支持自定义小猫资源包，让 DockCatWin 变成你自己的猫咪。

## 小猫状态

小猫会有以下状态：

- 休息：小猫保持一个姿势，如侧卧、揣手手、翻肚皮。
- 散步：小猫沿着任务栏附近走来走去。
- 过渡：小猫短暂地伸懒腰或打哈欠。
- 抱起：用鼠标左键拖动小猫可以把它抱起来移动。
- 对话：小猫面向你对话，用于提醒和出门确认。
- 出门：小猫按你设定的时长出门玩，并会带回来见闻或礼物。

## 我能自定义小猫形象吗？

当然可以。

DockCatWin 的用户资源包位于：

```text
UserData\AssetPacks
```

首次启动时，DockCatWin 会尽量准备这些入口：

- `default-lizz`：默认 DockCat 栗子资源包副本，便于参考。
- `huihui-cat`：可选示例资源包。
- `my-cat`：空模板资源包，供用户放入自己的猫猫图片。

PS: `huihui-cat` 是根据我自己的小猫“灰灰”采集制作的资源包。

程序默认使用 `default-lizz`。如果要使用自己的小猫，可以把 PNG 资源放入 `my-cat`，或在同级目录新建另一个资源包文件夹，然后在设置里选择它。

要让 DockCatWin 在所有场景下都使用你自己的小猫形象，需要休息、散步、过渡、抱起、对话这五类资源。DockCatWin 也允许加载不完整的资源包；缺失或加载失败的资源类型会自动回退到默认栗子，方便你边做边预览。

完整资源包格式见 [ASSET_PACK_GUIDE.md](ASSET_PACK_GUIDE.md)。

## 自定义走路素材

最稳定的方式是直接提供透明 PNG 走路序列帧。

如果你只有绿幕走路视频，也可以在资源包 manifest 中指向 `walk.mp4`。当用户电脑的 `PATH` 中能找到 `ffmpeg` 和 `ffprobe` 时，DockCatWin 会尝试从视频中抽取透明 PNG 走路帧，并缓存到：

```text
UserData\VideoCache
```

对外分享资源包时，仍建议直接提供透明 PNG 序列帧，这样不依赖外部工具，稳定性最好。

## 从源码构建

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

发布输出路径为：

```text
artifacts\DockCatWin
```

发布脚本会把 `README.md`、`LICENSE` 和 `ASSET_PACK_GUIDE.md` 一起复制到发行版文件夹。

## 隐私和数据记录

DockCatWin 是完全在本地运行的桌面应用，不需要联网、不传输数据、不含广告。

它只会在本地保存以下必要信息：

- 你自定义的设置项，如小猫名字、对你的称呼、提醒间隔、默认出门时间等。
- 使用统计，如陪伴时长、完成喝水/走动提醒次数、小猫出门得到的收藏品等。
- 你自定义的小猫资源包。

这些数据默认保存在程序同目录的 `UserData` 文件夹中。更新 DockCatWin 时，请保留这个文件夹，小猫就能继续读取原来的设置和存档。

## 许可证

DockCatWin 沿用上游 DockCat 的 PolyForm Noncommercial License 1.0.0。完整条款见 [LICENSE](LICENSE)。简单来说：

- 你可以自由阅读、复制、修改本项目源码，构建属于自己的 DockCatWin 版本。
- 你不可以把 DockCatWin 或其修改版本用于商业用途，包括销售、收费分发、商业产品捆绑等。
- 如果你公开分发修改版本，应保留原始许可证和版权声明、提供上游 DockCat 项目链接，并说明你的修改。

## 致谢

感谢 [DockCat](https://github.com/Auwuua/DockCat) 作者创造了这只温柔的小猫。DockCatWin 只是把这份陪伴带到 Windows 桌面上。
