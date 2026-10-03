# 炼金手记 FNA 离线预览

当前图标使用像素版：23 个炼金条目按原图 1 倍绘制，28 种原版材料直接读取游戏 XNB，均使用 `PointClamp`。`run.ps1` 从正式 `AlchemyCatalogItemTypes` 映射和所选 `-Tml` 安装中的 `ItemID` 常量生成离线 ID 表；优先读取 tML 内容覆盖，再读取相邻 Terraria 安装的 `Content`。运行 `& Tools/AlchemyPreview/run.ps1 -Configuration Release` 后，`output/vanilla-materials-*.png` 展示净土露滴配方中的瓶装水、净化粉等原版素材，覆盖 720p、100% 和 125% 缩放。离线坠落之星固定在第 0 帧，正式游戏读取当前动画帧；以下彩铅生成说明保留用于历史资源维护。

灰赝尘与瓶中雨的已确认 SVG 使用 `python Tools/AlchemyPreview/prepare_curio_art.py` 单独重建彩铅资源（需要 Pillow、resvg_py；`--converter` 可指定 svg-to-png 的转换脚本）。输出 100×100 原始彩铅 PNG、预乘 UI 副本和对应剪影，保留 SVG 原稿与像素贴图。`curio-dust` / `curio-rain` 场景使用离线 Lv.20 已研究快照，分别选中两件奇物，覆盖卡片、详情图与 100% / 125% 缩放；运行下述预览命令即可重建。

瓶中雨的彩铅小图适配位于 `Tools/AlchemyPlan/rain_pencil.py`，由共享彩铅转换器识别源稿的 `data-pencil-art` 标记后调用，完整重建也会保留效果：三颗带浅色高光和蓝紫描边的水滴、较浅的积水、减弱的玻璃排线。源 SVG 只增加语义标记，原画外观不变。水滴仍参与同一彩铅颗粒滤镜，避免细雨丝被紫色排线淹没。

运行 `& Tools/AlchemyPreview/run.ps1`。可使用 `-Tml` 指定 tModLoader 安装目录，`-CompileOnly` 只编译绘制链。

`& Tools/AlchemyPreview/run.ps1 -QuillOnly -Configuration Release` 链接正式 `AlchemySelectionQuillMotion`，检查选中切换缓动、飞行中滚动、快速重新选择、落定后滚动、重排与缩放。15 项数值检查和 720p / 125% 缩放连续帧输出到 `output/quill-frames`。随后运行 `python Tools/AlchemyPreview/quill_contact_sheet.py` 生成帧条和 WebP 动画。卡片先绘制，羽毛笔最后在相同目录裁剪区叠加；帧图使用真实 FNA 绘制，原生 SUI 事件与裁剪树仍需游戏内检查。

「全部」显示完整目录，各分类场景只绘制对应条目。`all`、`catalog-middle`、`catalog-bottom` 检查完整列表首屏、中间裁剪和末项可见性；`curio`、`food`、`material` 检查分类过滤、首行定位和滚动范围。绘制使用同一套卡片坐标和滚动偏移，包含目录滚动条；实际 SUI 滚轮与点击派发仍需游戏内验证。

`python Tools/AlchemyPreview/bake_surface.py` 生成炼金专属的无文字底图，使用 Pillow 计算连续紫色纸页、柔和光照与详情列过渡。1100×800 不透明 RGB，无半透明预乘歧义，不包含技能星图原来的顶部/底部裁剪边界；不修改技能表底图。运行时直接加载生成的 PNG，无逐帧纹理生成或额外 RenderTarget。

预览直接链接正式 `AlchemyDrawing`、`AlchemyCatalog`、`AlchemyNotebookState`、`AlchemyNotebookSnapshot` 和 `AlchemyNotebookPresentation`，读取 KL 的实际 XNB 字体与模组 PNG。生产 UI 使用真实材料与角色进度；本工具通过 `NotebookFixture.cs.txt` 注入独立的离线快照。资源适配按照 tModLoader `ImageIO.ToRaw` 保留部分透明像素 RGB、清零全透明像素。窗口隐藏并自动退出，不读取真实角色、背包或存档。

`.vissandbox/alchemy-preview/output` 导出四分类、未研究问号、等级不足、可研究、未来配方不足/齐全、满级、材料不足、制作中与详情滚到底部等状态，覆盖 1600×1000、1280×720 和 125% UI 缩放。`-Configuration Release` 检查无 Debug 按钮的界面。目录与详情按正式共享尺寸裁剪；外壳导航位置由预览宿主重放。原生 SUI 输入树、文本输入、滚轮派发与游戏生命周期仍需游戏内验证。这些图片是 FNA 离线预览，不是游戏截图。

`python Tools/AlchemyPreview/bake_art.py` 从现有 HTML 提取炼金徽记和注脚的 `notebook-quill`，输出无文字、预乘 Alpha 的 UI 装饰。选中羽毛笔沿轮廓增加不透明底色 `#302a3c`，遮住下方卡片边框；原来的紫色 `#b49ac1`、0.8 透明度与羽纹只叠在底色之上，外缘保留抗锯齿透明度。PNG 为 160×280；游戏中显示在 22×42 的标记区域，注脚自身不再绘制羽毛笔。单独重建使用 `--only SelectionQuill`。需要 svg-to-png 技能的 `convert_svg.py`、resvg_py 与 Pillow；可通过 `--converter` 指定转换脚本。彩铅物品和材料继续来自 ExampleAssets。

工具源码使用 `.cs.txt`，生成项目在 `.vissandbox`；`build.txt` 已排除 Tools 与 .vissandbox，工具不进入正式模组打包。

Debug 页脚新增「等级 +1」，正式 SUI 与预览共用按钮边界和绘制方法；`new` 检查可用状态，`level-max` 检查「已满级」禁用状态，`brew-050` 检查制作中禁用。Release 不绘制或创建此按钮。实际升级、经验保留、20 级上限和 Release 拒绝升级由 `Tools/AlchemyGameplayChecks/run.ps1` 验证。

`& Tools/AlchemyPreview/run.ps1 -BookOnly` 直接链接正式 `AlchemyBookArt`、`ConstellationBookMotion`、`ConstellationBookRenderer` 与 `ConstellationUIClock`。先捕获共享的完整炼金页面与无文字 PNG 加运行时字体封面，再以真实 32 条带书页网格重放展开、收回及两次快速反向。默认技能栏锚点按正式 `ConstellationBookLayout` 计算，收起坐标位于技能按钮左侧 104 UI 像素、尺寸 96×30；该坐标仅用于动画，不绘制按钮。

开合验证输出 `output/book-frames`：720p 连续 145 帧，1600×1000 与 125% UI 缩放代表帧；`alchemy-book-cover.png` 为实际标题绘制后的封面，`alchemy-book-timeline.json` 记录运动参数，`alchemy-book-checks.txt` 记录 30/60/144 FPS 时长、连续反向、不同帧间隔、失焦收敛、世界重置与结束状态检查。`python Tools/AlchemyPreview/book_contact_sheet.py` 生成帧条、缩放对照和 WebP 动画。

动画使用确定时间步长的 FNA 离线重放；并未模拟原生 SUI 输入树、游戏失焦回调或真实 RenderTargetPool 生命周期，相关部分需游戏内验证。只为编译共享 Renderer 中未使用的技能专属封面/按钮方法而提供 `ConstellationDrawing` 桩；若误调用会主动抛错，炼金封面由真实 `AlchemyBookArt` 绘制。

`python Tools/AlchemyPreview/prepare_pencil.py` 从 ExampleAssets 原始彩铅 PNG 生成 51 张预乘副本到 `UI/Assets/Pencil`。正式 UI 与预览都使用这些相同的副本，避免原始 straight-alpha 图在 tML AlphaBlend 下偏亮；HTML 图源与游戏像素贴图保持不变。

替换彩铅图时，应更新 `ExampleAssets` 原图后运行上述导出，不能直接把原图复制进 `UI/Assets/Pencil`。`--only BloodthirstPotion PurificationDew` 可只更新指定图；`--check` 逐像素验证 UI 副本是否等于原图 RGB×Alpha（保持 Alpha 不变），发现漏预乘、过期或重复预乘时返回非零。导出器跳过完全一致的副本。`run.ps1` 在渲染前自动执行此校验，需要 Python + Pillow，可用 `-Python` 指定解释器；`-CompileOnly` 不做资源校验。

嗜血药水与净土露滴曾因 UI 文件直接复制了 straight-alpha PNG 而出现白紫色杂边，重新生成预乘副本即可修复。tML `ImageIO.ToRaw` 只清零全透明像素，不会预乘半透明像素；因此 PNG 能在普通图片查看器中正常显示，也可能在 `AlphaBlend` 中过亮。此次修复只更改两张 UI 副本的 RGB，原图、像素物品图、Alpha 和剪影均不变。

已确认药水的彩铅造型由 `python Tools/PotionBottleDesigns/apply_notebook_potions.py` 统一同步到 ExampleAssets、预乘 UI 副本与剪影；可用 `--only` 指定条目。当前星力是三角瓶，集中与轻羽保留原 SVG 的斜向试管，共鸣为侧丝带圆瓶，净土保留原虹彩斜滴管及装饰露滴。彩铅图直接使用原参考的彩铅 SVG，保留旋转和药液反向补偿；物品像素图是独立手绘的竖版，不能据此改变彩铅方向。原始候选留在 round1，新版 SVG/彩铅存于 `round1/approved`。月露圆瓶、止痛药片及已确认的嗜血彩铅图保持原样。

右上角进度采用炼金锅图标、`炼金术`、`Lv.` 等级、圆头经验条和下方经验文字。`CloseButtonBounds` 与 `CloseButton` 同时供正式 SUI 和预览使用。`header-reference` 仅在离线快照中使用参考图的 Lv.3、35/40，实际游戏仍读取角色进度；`level-max`、`new` 和 `close-hover` 分别检查满级、空经验条和关闭按钮悬停。`python Tools/AlchemyPreview/bake_art.py --only LevelCauldron` 从保留的 SVG 重建 96×112 透明预乘图标。

`python Tools/AlchemyPreview/prepare_mystery_pixels.py` 读取正式目录的物品像素图，沿用每项原尺寸的轮廓并画入问号，输出透明像素贴图到 `UI/Assets/MysteryPixel`。未研究和已研究图标均按原图 1 倍显示。当前目录有 23 项；新增条目后重新执行脚本即可扩展。旧 `prepare_silhouettes.py` 与彩铅剪影保留作历史资源，不再是运行时未研究图标。`curio-locked` 检查奇物轮廓，`recipe-missing` / `recipe-ready` 检查配方条件与可研究状态。
