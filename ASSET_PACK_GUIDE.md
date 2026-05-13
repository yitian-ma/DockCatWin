# DockCatWin 资源包自定义指南

DockCatWin 的用户资源包位于：

```text
UserData\AssetPacks
```

首次启动时，DockCatWin 会尽量创建：

- `default-lizz`：默认 DockCat 栗子资源包副本，便于参考。
- `huihui-cat`：可选示例资源包。
- `my-cat`：空模板资源包，供用户放入自己的猫猫素材。

程序默认使用 `default-lizz`。如果要使用自己的小猫，把 PNG 资源放入 `my-cat`，或在同级目录新建另一个资源包文件夹，然后在 DockCatWin 设置里选择它。

## 推荐目录结构

```text
my-cat
├─ manifest.json
├─ animations
│  └─ walk
│     ├─ walk_01.png
│     ├─ walk_02.png
│     ├─ walk_03.png
│     └─ walk_04.png
└─ poses
   ├─ dialogue
   │  └─ stand.png
   ├─ held
   │  └─ held.png
   ├─ resting
   │  ├─ bread.png
   │  └─ loaf.png
   └─ transition
      ├─ stretch.png
      └─ yawn.png
```

PNG 文件需要有真正的透明 alpha 通道。图片里画出来的灰白棋盘格不是透明背景。

## manifest 示例

```json
{
  "id": "my-cat",
  "name": "My Cat",
  "author": "Your Name",
  "canvas_width": 1254,
  "canvas_height": 1254,
  "default_anchor": { "x": 0.5, "y": 0.88 },
  "poses": {
    "resting": "poses/resting",
    "held": "poses/held",
    "dialogue": "poses/dialogue",
    "transition": "poses/transition"
  },
  "display_sizes": {
    "held": { "width": 650, "height": 1236 }
  },
  "animations": {
    "walk": {
      "fps": 3,
      "video": "animations/walk/walk.mp4",
      "video_frame_count": 4,
      "frames": []
    }
  }
}
```

## 姿态要求

- `resting`：一个或多个休息姿态 PNG。
- `held`：一个或多个拖拽/抱起姿态 PNG。
- `dialogue`：一个或多个对话时使用的 PNG。
- `transition`：可选过渡姿态，例如伸懒腰或打哈欠。
- `animations\walk`：走路 PNG 序列帧。

如果自定义资源包缺少某一类资源，DockCatWin 会对这一类回退使用默认栗子资源。

## 走路帧

最稳定的方式是直接提供透明 PNG 序列帧：

```text
animations\walk\walk_01.png
animations\walk\walk_02.png
animations\walk\walk_03.png
animations\walk\walk_04.png
```

如果没有 PNG 走路帧，并且 manifest 指向一段绿幕 MP4，DockCatWin 可以可选地从视频中抽帧：

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

这个可选视频抽帧功能会调用用户电脑 `PATH` 中的 `ffmpeg` 和 `ffprobe`。DockCatWin 不会捆绑它们。工具可用时，程序会从视频中均匀抽帧、去除绿色背景、归一化为 `1100 x 650`，并缓存透明 PNG 到：

```text
UserData\VideoCache
```

对外发布资源包时，仍建议直接带 PNG 走路帧。视频输入主要适合从 AI 生成的绿幕视频快速制作素材。

## 抓起姿态尺寸

如果抱起姿态图片比普通站立/休息图更高或更宽，可以使用 `display_sizes.held`：

```json
"display_sizes": {
  "held": { "width": 650, "height": 1236 }
}
```

省略时，DockCatWin 会使用 `canvas_width` 和 `canvas_height` 显示抱起姿态。

## 绿幕视频建议

- 使用纯净、饱和、均匀的绿色背景。
- 避免阴影、地面反光，以及毛发上的绿色溢色。
- 每一帧都要露出完整猫身。
- 让猫尽量保持居中，大小变化不要太剧烈。
- 使用短而稳定的走路循环。

如果抠图后仍有明显绿边，建议先用专门的抠图工具清理成透明 PNG，再放入资源包。
