# 炼金手记 FNA 离线预览

运行 `& Tools/AlchemyPreview/run.ps1`。可使用 `-Tml` 指定 tModLoader 安装目录，`-CompileOnly` 只编译绘制链。

预览直接链接正式 `AlchemyDrawing`、`AlchemyCatalog`、`AlchemyNotebookState`、`AlchemyNotebookSnapshot` 和 `AlchemyNotebookPresentation`，读取 KL 的实际 XNB 字体与模组 PNG。生产 UI 使用真实材料与角色进度；本工具通过 `NotebookFixture.cs.txt` 注入独立的离线快照。资源适配按照 tModLoader `ImageIO.ToRaw` 保留部分透明像素 RGB、清零全透明像素。窗口隐藏并自动退出，不读取真实角色、背包或存档。

`.vissandbox/alchemy-preview/output` 导出四分类、未研究问号、等级不足、可研究、未来配方不足/齐全、满级、材料不足、制作中与详情滚到底部等状态，覆盖 1600×1000、1280×720 和 125% UI 缩放。`-Configuration Release` 检查无 Debug 按钮的界面。目录与详情按正式共享尺寸裁剪；外壳导航位置由预览宿主重放。原生 SUI 输入树、文本输入、滚轮派发与游戏生命周期仍需游戏内验证。这些图片是 FNA 离线预览，不是游戏截图。

`python Tools/AlchemyPreview/bake_art.py` 从现有 HTML 提取炼金徽记和注脚的 `notebook-quill`，输出无文字、预乘 Alpha 的 UI 装饰。选中羽毛笔复用注脚原来的紫色 `#b49ac1` 和 0.8 透明度，PNG 为 160×280；游戏中显示在 22×42 的标记区域，注脚自身不再绘制羽毛笔。单独重建使用 `--only SelectionQuill`。需要 svg-to-png 技能的 `convert_svg.py`、resvg_py 与 Pillow；可通过 `--converter` 指定转换脚本。彩铅物品和材料继续来自 ExampleAssets。

工具源码使用 `.cs.txt`，生成项目在 `.vissandbox`；`build.txt` 已排除 Tools 与 .vissandbox，工具不进入正式模组打包。

`& Tools/AlchemyPreview/run.ps1 -BookOnly` 直接链接正式 `AlchemyBookArt`、`ConstellationBookMotion`、`ConstellationBookRenderer` 与 `ConstellationUIClock`。先捕获共享的完整炼金页面与无文字 PNG 加运行时字体封面，再以真实 32 条带书页网格重放展开、收回及两次快速反向。默认技能栏锚点按正式 `ConstellationBookLayout` 计算，收起坐标位于技能按钮左侧 104 UI 像素、尺寸 96×30；该坐标仅用于动画，不绘制按钮。

开合验证输出 `output/book-frames`：720p 连续 145 帧，1600×1000 与 125% UI 缩放代表帧；`alchemy-book-cover.png` 为实际标题绘制后的封面，`alchemy-book-timeline.json` 记录运动参数，`alchemy-book-checks.txt` 记录 30/60/144 FPS 时长、连续反向、不同帧间隔、失焦收敛、世界重置与结束状态检查。`python Tools/AlchemyPreview/book_contact_sheet.py` 生成帧条、缩放对照和 WebP 动画。

动画使用确定时间步长的 FNA 离线重放；并未模拟原生 SUI 输入树、游戏失焦回调或真实 RenderTargetPool 生命周期，相关部分需游戏内验证。只为编译共享 Renderer 中未使用的技能专属封面/按钮方法而提供 `ConstellationDrawing` 桩；若误调用会主动抛错，炼金封面由真实 `AlchemyBookArt` 绘制。

`python Tools/AlchemyPreview/prepare_pencil.py` 从 ExampleAssets 原始彩铅 PNG 生成 51 张预乘副本到 `UI/Assets/Pencil`。正式 UI 与预览都使用这些相同的副本，避免原始 straight-alpha 图在 tML AlphaBlend 下偏亮；HTML 图源与游戏像素贴图保持不变。
