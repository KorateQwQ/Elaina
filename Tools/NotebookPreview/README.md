# 魔女手札 FNA 离线预览与状态检查

顶部专项：`& Tools/NotebookPreview/run.ps1 -Headers` 只重放当前顶部绘制，导出 100%、136%、200% 三种物理倍率。用于对齐不同倍率截图，检查标题笔画、Gelasio 数字、基线和柔光，不重复状态回归测试。

`python Tools/NotebookPreview/bake_header.py` 从参考 HTML 抽取书页背景 CSS，以完整 1100×800 渐变域生成后截取顶部 146 像素，输出 `Assets/HeaderSurface.png`。图中没有文字和图标；RGB 不透明，约 0.61 MiB RGBA 显存。运行时增加一张缓存纹理的绘制，无临时 RT；星空底图和书页其余区域不变。脚本需要本机 Chrome 和 Pillow，可用 `--chrome` 指定浏览器路径。

从模组仓库根目录运行：

```powershell
& Tools/NotebookPreview/run.ps1
# 安装路径不同时：
& Tools/NotebookPreview/run.ps1 -Tml 'D:/Steam/steamapps/common/tModLoader'
```

脚本从当前 `ConstellationPreviewUI.cs` 抽取窗口、节点、星空、连线及固定详情绘制方法，链接实际 `PreviewDrawing.cs`、`PreviewData.cs`、`PreviewDetail.cs`，加载 KL 的 HarmonyOS Sans SC、Noto Serif SC 和 Gelasio XNB，以及模组原始 PNG。只替换资源加载和游戏静态上下文。临时工程和 PNG 放在 `.vissandbox/notebook-preview`；所有工具均在 `buildIgnore` 范围内。

先执行 95 项演示状态回归检查，再以隐藏 FNA/D3D11 窗口导出 36 张图片：初始、普通术式、待研习、月华飞弹、关闭心得、失效飞弹及滚动到底部、隐星、待启封、满级、空分类、帮助。场景覆盖 1600×1000 / 100%、1280×720 / 100%、1600×1000 / 125%。进程自动退出，不操作正在运行的游戏。

`python Tools/NotebookPreview/reference.py` 可在本机 Chrome 中对原始 HTML 的副本打开初始和月华飞弹状态，输出浏览器参考图。只对副本附加场景初始化，不改用户 HTML。

这些是 **FNA 离线绘制预览，非游戏截图**。详情按同一份实测内容行排列并使用同一绘制委托，模拟滚动裁剪；未挂载完整 SUI 输入树。图片可验证字体、颜色、内容布局及裁剪，不能证明游戏中的鼠标派发、生命周期或联机行为。完整 SUI API 通过模组源码编译核对；游戏内仍需检查滚轮、关系跳转、滚动条拖动、帮助输入阻挡及退出世界。

旧的 `Tools/TypographyPreview` 和 `.vissandbox/starfield-preview` 针对旧版结构；本次手札验证使用此入口。
