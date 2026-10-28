# 炼金手记 FNA 离线预览

## 瓶中雨简化为雨云与玻璃瓶（2026-10-28）

瓶中雨每批仅需雨云 ×1、玻璃瓶 ×1，产出瓶中雨 ×1。材料映射使用本机 `ItemID.RainCloud`（765）与 `ItemID.Bottle`（31），中英文名称均引用原版本地化；正式描边为 `UI/Assets/Outlines/Item_765.png` 与 `Item_31.png`，已同步清单。依次运行 `pwsh -File Tools/AlchemyPreview/run.ps1 -ExportMaterials`、`python Tools/AlchemyPreview/bake_redesign.py`、`python Tools/AlchemyPreview/bake_redesign.py --check` 和 `pwsh -File Tools/AlchemyPreview/run.ps1 -Scene curio-rain` 可复现。66 张描边及正式目录资源检查通过，已打开本次 `output/zh-Hans/curio-rain-1600x1000-1.00.png` 和 `curio-rain-1600x1000-1.25.png`，确认两种材料的图标、数量、材料行与制作底栏正常。`dotnet msbuild 伊蕾娜.csproj /t:Compile /p:Configuration=Debug /p:BuildProjectReferences=false` 编译通过，仍有项目既有警告。预览链接正式目录和绘制源码；实际游戏内炼制与背包扣料尚未验证。

## 素材精简与月露文案（2026-10-23）

温香树脂已从正式物品、素材目录、配方、物品映射、中英文本地化及专属贴图移除；目录现有 22 项，其中素材 7 项，正式描边 64 张。月露描述改为“用瓶装水与月光草制成的药液基底，可用于各种药水。”，物品提示同步。`pwsh -File Tools/AlchemyPreview/run.ps1 -Scene material-initial` 已检查默认与 125% FNA 离线截图；旧存档中过期研究 ID 会先过滤再限制数量，避免条目减少后截断有效研究。完整编译、资源检查及制作集成检查通过；完整物品检查仍受 `RevealingDust` 贴图与目录尺寸不匹配阻断，尚未游戏内验证。

## 药剂持续时间（2026-10-23）

止痛药保留 60 秒，其余六种药剂统一为 4 分钟；七种药剂详情均显示持续时间。运行 `pwsh -File Tools/AlchemyPreview/run.ps1 -Scene <场景> -Configuration Release`，场景为 `potion`（月露合剂）、`recipe-painkiller-learned`、`bloodlust`、`starpower`、`focus`、`resonance`、`featherlight`；英文星力增加 `-Culture en-US`。已打开本次默认与 125% 缩放的 FNA 离线截图，确认时长栏、材料与制作底栏正常；英文长详情沿用滚动显示。65 张正式描边检查、完整模组编译、药剂行为与集成检查通过。本次未新增材料或图标，游戏内饮用与倒计时尚未人工验证。

## 炼金术技能上限与 Lv.8 配方（2026-10-22）

运行 `pwsh -File Tools/AlchemyPreview/run.ps1 -Scene recipe-scrolls -Configuration Release`，使用正式绘制、本地化和研究等级规则生成配方各状态。已打开检查嗜血可研究、星力等级不足、集中可研究及共鸣已研究页面，覆盖默认与 125% 缩放：上限显示 8，等级条件为 7/8 或 8/8，满级显示 MAX，材料行与底栏正常。正式目录的轮廓和清单检查通过；本次未新增材料或图标。角色技能升级、手记点击及多人游戏仍需游戏内验收。

## 实体配方检查（2026-10-21）

运行 `pwsh -File Tools/AlchemyPreview/run.ps1 -Scene recipe-scrolls`，一次生成四种战斗药水与轻羽药水的缺配方、等级不足但持有配方、可研究、已研究四种状态，覆盖默认和 125% 缩放。战斗药水门槛为 Lv.8，轻羽为 Lv.2；不足场景分别为 Lv.7 和 Lv.1。配方场景以已学 Lv.8 炼金术的手记上限展示，生产上限来自角色实际技能。英文使用 `-Culture en-US -Configuration Release`。单场景示例：`-Scene recipe-featherlight-ready`；文件名采用 `recipe-<bloodlust|starpower|focus|resonance|featherlight>-<missing|level-low|ready|learned>`。

配方名称从正式物品本地化读取，等级来自正式 `AlchemyProgressionRules`，图标与描边使用正式通用卷轴资源。预览启动校验包含卷轴描边，`bake_redesign.py --check` 同时校验资源清单。这些场景只模拟背包及研究状态；真实消费、存档、商店和掉落条件由 `Tools/AlchemyItemChecks/run.ps1 -PotionsOnly` 验证，游戏内 UI 输入和联机仍需验收。

## 新版炼金手记（2026-10-19）

当前渲染链接正式 `AlchemyNotebookPresentation`、`AlchemyNotebookRedesign`、`AlchemyNotebookSkin` 与 `AlchemyDrawing`，使用 KL 的游戏字体。页面、卡片、材料槽与动作区由正式方法绘制；宿主只重放控件位置与私有测试快照，不读取角色或存档。

```powershell
pwsh -File Tools/AlchemyPreview/run.ps1 -ExportMaterials
python Tools/AlchemyPreview/bake_redesign.py
pwsh -File Tools/AlchemyPreview/run.ps1 -Culture zh-Hans -Configuration Debug
pwsh -File Tools/AlchemyPreview/run.ps1 -Culture en-US -Configuration Release
```

输出在 `.vissandbox/alchemy-preview/output/<culture>/`。`redesign-reference` 对照参考图（Lv.3、止痛药、树脂不足）；其他场景覆盖未研究、配方条件、长效果、用途链接、制作状态、列表/详情底部与满级。每场景覆盖 1280×720、1600×1000，以及 125% UI 缩放。Debug / Release 分别验证有无调试按钮。

`bake_redesign.py --check` 逐像素验证当前预处理资源。新检出先用 `-ExportMaterials` 导出本机 tML / Terraria XNB；不需要旧彩铅图。原版动画贴图按帧生成边缘，运行时读取真实 `Main.GetItemDrawFrame` 并选择相同帧的遮罩，离线预览默认第零帧。新资产均使用预乘 alpha，详见 `ElainaModAlchemy/UI/README.md`。

新增配方材料还须遵守 `ElainaModAlchemy/AGENTS.md`。预览启动时会按正式目录检查所有材料与配方图标的轮廓 PNG，即使新材料尚未导出到 `material-sources` 也会报出缺失路径。四种职业药水分别用 `-Scene bloodlust`、`-Scene starpower`、`-Scene focus`、`-Scene resonance` 检查材料行和底栏。

三种料理分别使用 `pwsh -File Tools/AlchemyPreview/run.ps1 -Scene bread`、`-Scene stew`、`-Scene brulee`。2026-10-20 已检查三个中文页面的默认与 125% 缩放，以及英文面包页面的 Release 预览；使用正式目录和本地化，显示面包的角色差异、30 点生命恢复、炖菜的酒足饭饱、布蕾的吃得好及统一的 5 分钟时长。图片位于上述输出目录，文件名前缀分别为 `bread`、`stew`、`brulee`。

这些输出是 **FNA 离线预览**。正式 SUI 滚轮、拖动、点击、游戏内内容重载和联机生命周期仍需游戏内验收。下文彩铅与旧纸卡记录仅供旧资产维护，现版不再使用其资源校验。


当前图标使用像素版：23 个炼金条目按原图 1 倍绘制，28 种原版材料直接读取游戏 XNB，均使用 `PointClamp`。`run.ps1` 从正式 `AlchemyCatalogItemTypes` 映射和所选 `-Tml` 安装中的 `ItemID` 常量生成离线 ID 表；优先读取 tML 内容覆盖，再读取相邻 Terraria 安装的 `Content`。运行 `& Tools/AlchemyPreview/run.ps1 -Configuration Release` 后，`output/vanilla-materials-*.png` 展示净土露滴配方中的瓶装水、净化粉等原版素材，覆盖 720p、100% 和 125% 缩放。离线坠落之星固定在第 0 帧，正式游戏读取当前动画帧；以下彩铅生成说明保留用于历史资源维护。

灰赝尘与瓶中雨的已确认 SVG 使用 `python Tools/AlchemyPreview/prepare_curio_art.py` 单独重建彩铅资源（需要 Pillow、resvg_py；`--converter` 可指定 svg-to-png 的转换脚本）。输出 100×100 原始彩铅 PNG、预乘 UI 副本和对应剪影，保留 SVG 原稿与像素贴图。`curio-dust` / `curio-rain` 场景使用离线 Lv.20 已研究快照，分别选中两件奇物，覆盖卡片、详情图与 100% / 125% 缩放；运行下述预览命令即可重建。

瓶中雨的彩铅小图适配位于 `Tools/AlchemyPlan/rain_pencil.py`，由共享彩铅转换器识别源稿的 `data-pencil-art` 标记后调用，完整重建也会保留效果：三颗带浅色高光和蓝紫描边的水滴、较浅的积水、减弱的玻璃排线。源 SVG 只增加语义标记，原画外观不变。水滴仍参与同一彩铅颗粒滤镜，避免细雨丝被紫色排线淹没。

运行 `& Tools/AlchemyPreview/run.ps1`。可使用 `-Tml` 指定 tModLoader 安装目录，`-CompileOnly` 只编译绘制链。

`& Tools/AlchemyPreview/run.ps1 -QuillOnly -Configuration Release` 链接正式 `AlchemySelectionQuillMotion`，检查选中切换缓动、飞行中滚动、快速重新选择、落定后滚动、重排与缩放。15 项数值检查和 720p / 125% 缩放连续帧输出到 `output/quill-frames`。随后运行 `python Tools/AlchemyPreview/quill_contact_sheet.py` 生成帧条和 WebP 动画。卡片先绘制，羽毛笔最后在相同目录裁剪区叠加；帧图使用真实 FNA 绘制，原生 SUI 事件与裁剪树仍需游戏内检查。

「全部」显示完整目录，各分类场景只绘制对应条目。`all`、`catalog-middle`、`catalog-bottom` 检查完整列表首屏、中间裁剪和末项可见性；`curio`、`food`、`material` 检查分类过滤、首行定位和滚动范围。绘制使用同一套卡片坐标和滚动偏移，目录与详情均隐藏滚动条；与正式 UI 一样，选中卡跳过普通循环、最后绘制在相邻卡片之上。离线帧只绘制落定姿态（`AlchemyCardMotion.Settled`），拾起弹簧、悬停和呼吸动画的实时过程需游戏内检查；实际 SUI 滚轮与点击派发仍需游戏内验证。

`python Tools/AlchemyPreview/bake_surface.py` 生成炼金专属的无文字底图，使用 Pillow 计算连续紫色纸页、柔和光照与详情列过渡。1100×800 不透明 RGB，无半透明预乘歧义，不包含技能星图原来的顶部/底部裁剪边界；不修改技能表底图。运行时直接加载生成的 PNG，无逐帧纹理生成或额外 RenderTarget。

`python Tools/AlchemyPreview/bake_paper.py` 生成目录标本卡纸图集 `UI/Assets/CatalogPaper.png`（1704×936，2× 分辨率，固定随机种子可复现）。6 列为不同毛边与做旧纹理的纸样；3 行依次为近白纸面（运行时统一着为浅紫色）、选中描边用的外扩轮廓、阴影与光晕用的柔和轮廓。输出已为预乘 alpha，直接适配 AlphaBlend。

预览直接链接正式 `AlchemyDrawing`、`AlchemyCatalog`、`AlchemyNotebookState`、`AlchemyNotebookSnapshot` 和 `AlchemyNotebookPresentation`，读取 KL 的实际 XNB 字体与模组 PNG。生产 UI 使用真实材料与角色进度；本工具通过 `NotebookFixture.cs.txt` 注入独立的离线快照。资源适配按照 tModLoader `ImageIO.ToRaw` 保留部分透明像素 RGB、清零全透明像素。窗口隐藏并自动退出，不读取真实角色、背包或存档。

`.vissandbox/alchemy-preview/output` 导出四分类、未研究问号、等级不足、可研究、未来配方不足/齐全、满级、材料不足、制作中与详情滚到底部等状态，覆盖 1600×1000、1280×720 和 125% UI 缩放。`-Configuration Release` 检查无 Debug 按钮的界面。目录与详情按正式共享尺寸裁剪；外壳导航位置由预览宿主重放。原生 SUI 输入树、文本输入、滚轮派发与游戏生命周期仍需游戏内验证。这些图片是 FNA 离线预览，不是游戏截图。

`python Tools/AlchemyPreview/bake_art.py` 从现有 HTML 提取炼金徽记和注脚的 `notebook-quill`，输出无文字、预乘 Alpha 的 UI 装饰。选中羽毛笔沿轮廓增加不透明底色 `#302a3c`，遮住下方卡片边框；原来的紫色 `#b49ac1`、0.8 透明度与羽纹只叠在底色之上，外缘保留抗锯齿透明度。PNG 为 160×280；游戏中显示在 22×42 的标记区域，注脚自身不再绘制羽毛笔。单独重建使用 `--only SelectionQuill`。需要 svg-to-png 技能的 `convert_svg.py`、resvg_py 与 Pillow；可通过 `--converter` 指定转换脚本。彩铅物品和材料继续来自 ExampleAssets。

工具源码使用 `.cs.txt`，生成项目在 `.vissandbox`；`build.txt` 已排除 Tools 与 .vissandbox，工具不进入正式模组打包。

Debug 页脚包含「等级 +1」「升满级」「解锁所有配方」「补充素材」「重置手记」，正式 SUI 与预览共用按钮边界和绘制方法；`new` 检查可用状态，`level-max` 检查满级与全部解锁禁用状态，`brew-050` 检查制作中禁用。Release 不绘制或创建调试按钮。实际升级、当前技能等级上限、跳过研究条件解锁、存档往返、重置及 Release 拒绝调试操作由 `Tools/AlchemyGameplayChecks/run.ps1` 验证。

2026-10-22 调试按钮验证：`run.ps1 -Scene new -Configuration Debug`、`-Scene level-max`、`-Scene brew-050`，另以 `-Culture en-US -Scene new` 检查英文，以及 `-Scene debug-release -Configuration Release` 检查隐藏调试栏。正式资源完整性检查通过；已打开本次 FNA 离线截图，检查默认、125% 缩放及 720p 制作中状态。游戏逻辑检查 Release 73 项、Debug 99 项通过。正式源码使用 `dotnet msbuild 伊蕾娜.csproj /t:Compile /p:Configuration=Debug /p:BuildProjectReferences=false` 编译通过；打包被运行中的 tModLoader 阻止，未进行游戏内点击或多人验证。

`& Tools/AlchemyPreview/run.ps1 -BookOnly` 直接链接正式 `AlchemyBookArt`、`ConstellationBookMotion`、`ConstellationBookRenderer` 与 `ConstellationUIClock`。先捕获共享的完整炼金页面与无文字 PNG 加运行时字体封面，再以真实 32 条带书页网格重放展开、收回及两次快速反向。默认技能栏锚点按正式 `ConstellationBookLayout` 计算，收起坐标位于技能按钮左侧 104 UI 像素、尺寸 96×30；该坐标仅用于动画，不绘制按钮。

开合验证输出 `output/book-frames`：720p 连续 145 帧，1600×1000 与 125% UI 缩放代表帧；`alchemy-book-cover.png` 为实际标题绘制后的封面，`alchemy-book-timeline.json` 记录运动参数，`alchemy-book-checks.txt` 记录 30/60/144 FPS 时长、连续反向、不同帧间隔、失焦收敛、世界重置与结束状态检查。`python Tools/AlchemyPreview/book_contact_sheet.py` 生成帧条、缩放对照和 WebP 动画。

动画使用确定时间步长的 FNA 离线重放；并未模拟原生 SUI 输入树、游戏失焦回调或真实 RenderTargetPool 生命周期，相关部分需游戏内验证。只为编译共享 Renderer 中未使用的技能专属封面/按钮方法而提供 `ConstellationDrawing` 桩；若误调用会主动抛错，炼金封面由真实 `AlchemyBookArt` 绘制。

`python Tools/AlchemyPreview/prepare_pencil.py` 从 ExampleAssets 原始彩铅 PNG 生成 51 张预乘副本到 `UI/Assets/Pencil`。正式 UI 与预览都使用这些相同的副本，避免原始 straight-alpha 图在 tML AlphaBlend 下偏亮；HTML 图源与游戏像素贴图保持不变。

替换彩铅图时，应更新 `ExampleAssets` 原图后运行上述导出，不能直接把原图复制进 `UI/Assets/Pencil`。`--only BloodthirstPotion PurificationDew` 可只更新指定图；`--check` 逐像素验证 UI 副本是否等于原图 RGB×Alpha（保持 Alpha 不变），发现漏预乘、过期或重复预乘时返回非零。导出器跳过完全一致的副本。旧彩铅流程可单独使用此校验；当前 `run.ps1` 改为检查 `bake_redesign.py --check`，需要 Python + Pillow，可用 `-Python` 指定解释器；`-CompileOnly` 不做资源校验。

嗜血药水与净土露滴曾因 UI 文件直接复制了 straight-alpha PNG 而出现白紫色杂边，重新生成预乘副本即可修复。tML `ImageIO.ToRaw` 只清零全透明像素，不会预乘半透明像素；因此 PNG 能在普通图片查看器中正常显示，也可能在 `AlphaBlend` 中过亮。此次修复只更改两张 UI 副本的 RGB，原图、像素物品图、Alpha 和剪影均不变。

已确认药水的彩铅造型由 `python Tools/PotionBottleDesigns/apply_notebook_potions.py` 统一同步到 ExampleAssets、预乘 UI 副本与剪影；可用 `--only` 指定条目。当前星力是三角瓶，集中与轻羽保留原 SVG 的斜向试管，共鸣为侧丝带圆瓶，净土保留原虹彩斜滴管及装饰露滴。彩铅图直接使用原参考的彩铅 SVG，保留旋转和药液反向补偿；物品像素图是独立手绘的竖版，不能据此改变彩铅方向。原始候选留在 round1，新版 SVG/彩铅存于 `round1/approved`。月露圆瓶、止痛药片及已确认的嗜血彩铅图保持原样。

右上角进度采用炼金锅图标、`炼金术`、`Lv.` 等级、圆头经验条和下方经验文字。`CloseButtonBounds` 与 `CloseButton` 同时供正式 SUI 和预览使用。`header-reference` 仅在离线快照中使用参考图的 Lv.3、35/40，实际游戏仍读取角色进度；`level-max`、`new` 和 `close-hover` 分别检查满级、空经验条和关闭按钮悬停。`python Tools/AlchemyPreview/bake_art.py --only LevelCauldron` 从保留的 SVG 重建 96×112 透明预乘图标。

`python Tools/AlchemyPreview/prepare_mystery_pixels.py` 读取正式目录的物品像素图，沿用每项原尺寸的轮廓并画入问号，输出透明像素贴图到 `UI/Assets/MysteryPixel`。未研究和已研究图标均按原图 1 倍显示。当前目录有 23 项；新增条目后重新执行脚本即可扩展。旧 `prepare_silhouettes.py` 与彩铅剪影保留作历史资源，不再是运行时未研究图标。`curio-locked` 检查奇物轮廓，`recipe-missing` / `recipe-ready` 检查配方条件与可研究状态。

## 页面弹性滚动（2026-10-20）

`pwsh -File Tools/AlchemyPreview/run.ps1 -ScrollOnly -Configuration Release` 链接正式 `System/UI/ElasticScrollMotion`（与技能详情共用），检查 30/60/144 FPS 的拖动、物品按下禁止拖动、点击抖动、滚轮越界与回弹、连续滚动/反向、重新抓住越界页面、选中定位、内容缩短和取消拖动。输出 `output/zh-Hans/scroll-frames/checks.txt` 及顶部/底部、拖动/滚轮的多个时间点，覆盖 720p 默认缩放和 1600×1000 的 125% 缩放。`middle-<宽>.png` 为详情中段，检查共用 `System/UI/ScrollHint` 的上下箭头（下箭头叠在底部渐隐上）。

正式目录和详情使用 `System/UI/ElasticPageScrollView` 保留 SUI 容器布局与裁剪，以设计坐标计算弹性偏移，越界上限 32 像素；原生竖向滚动条不参与输入或绘制。目录卡片、详情材料行与用途链接排除拖动，详情文字与卡片间隙可拖动，松手或移出页面后释放均回弹，失焦/关闭/缩放时取消拖动。底部横向材料栏沿用原有滚动条。

离线帧使用正式绘制与运动代码，输入是确定性重放；并未模拟完整 SUI 事件树。物品点击、跨区域释放、失焦与游戏内 UI 缩放仍需在游戏中验收。


## 止痛药配方预览（2026-10-21）

`pwsh -File Tools/AlchemyPreview/run.ps1 -Scene recipe-painkiller-missing`、`recipe-painkiller-ready`、`recipe-painkiller-learned` 分别覆盖缺少配方、Lv.1 持有配方可研究、已研究材料详情。每个场景输出 1600×1000 / 1280×720 默认缩放与 1600×1000 的 125% 缩放；本轮已打开检查默认缺失状态及 125% 可研究/已研究状态，卷轴名称、数量、丛林宝箱来源、原有三项材料与底栏正常。止痛药最低研究等级为 1，没有可达的低等级状态，批量 `recipe-scrolls` 不生成该无效场景。沿用正式 `Recipe_Pixel.png` 与现有预制描边，无新增材料或独立图标；正式资源检查通过。

## 以太滴管与瓶中微光（2026-10-21）

正式以太滴管为 `Untitled 10152026 050309.png` 左上紫色变体，包含其分离的上部组件，透明裁边后为 27×28；瓶中微光使用已确认的 `BottledShimmer_Pixel_New.png`（18×22）。使用 `python Tools/PotionPixel/PrepareShimmerItems.py` 可无损重新提取/复制，随后运行 `prepare_mystery_pixels.py`、`run.ps1 -ExportMaterials`、`bake_redesign.py` 和 `bake_redesign.py --check`。正式 PNG、剪影与轮廓均在可打包目录。

`run.ps1 -Scene curio-dropper` 检查滴管条目；`-Scene bottled-shimmer` 检查瓶中微光素材条目；`-Scene curio-dust` 检查灰赝尘所需的瓶中微光材料行及底栏。三个场景均覆盖默认与 125% 缩放；英文增加 `-Culture en-US -Configuration Release`。保留内部 `shimmer` 目录 ID 兼容已有研究记录，实际物品映射为 `BottledShimmer`，旧物品类型由 `LegacyName("ShimmerDroplet")` 迁移。

取液已对齐本机原版水桶：鼠标目标有微光，且目标周围 3×3 同类液体总量大于 100 时可提取；先抽取目标格，再依序抽取相邻格，总共最多 255 液体单位。不再要求目标格装满。成功后消耗以太滴管并生成一个瓶中微光；瓶中微光只能向无液体的可用格释放完整一格微光并消耗，也可用于灰赝尘。失败目标不消耗；原版水桶的 101～254 单位取液阈值同样沿用。离线真实 Tilemap/WorldGen 行为检查见 `Tools/AlchemyItemChecks/ShimmerTransferChecks.cs.txt`；完整多人同步和游戏内操作仍须验收。

### 浅池取液修正（2026-10-21）

修复单格 `LiquidAmount == 255` 导致浅层微光无法提取的问题。生产 `AlchemyShimmerTransfer.TryCollect` 直接依据本机 `Player.ItemCheck_UseBuckets` 的 3×3 计数、`>100` 阈值、目标优先及最多 255 单位取液顺序实现，对每个修改格执行液体更新/同步。不更改瓶子的放液条件。中文 `curio-dropper` 与 `bottled-shimmer` 场景重新检查默认与 125% 缩放，说明不再要求单个目标格装满。

## 素材初始解锁（2026-10-21）

`pwsh -File Tools/AlchemyPreview/run.ps1 -Scene material-initial -Configuration Release` 使用 Lv.1、0 XP 快照，素材按正式 `InitiallyUnlockedIds` 全部解锁；其他非初始条目保持未研究。覆盖默认与 125% UI 缩放，检查全部 8 种素材显示真实图标和名称，无问号或可研究提示。已打开检查两种缩放；正式资源完整性检查通过，无新增图标或材料。真实新角色、旧存档补全、重置、重复研究拒绝及存档往返由 `Tools/AlchemyGameplayChecks/run.ps1` 检查（Release 65 项、Debug 84 项通过）。离线宿主不替代游戏内输入与多人验证。

## 料理初始解锁与面包开心（2026-10-22）

`pwsh -File Tools/AlchemyPreview/run.ps1 -Scene food-initial` 用 Lv.1、0 XP 的初始快照显示三个已解锁料理；蜂蜜面包效果为伊蕾娜「很满意 · 开心」、其他角色「吃得好」，持续 5 分钟。已打开检查本次 100% 和 125% FNA 离线截图，三个条目显示真实图标与名称，无需研究，面包材料行和底栏完整。正式资源检查通过（65 张描边），未新增贴图或材料。旧章节中的 30 点治疗记录已被本次行为取代。

净土露滴分类调整（2026-10-22）：正式目录改为 `curio`，详情类型同步为「奇物 · 功能」，现为魔药 7 项、奇物 5 项。`vanilla-materials` 和 `recipe-isolation-ready` 场景按正式目录选分类；已查看本次 100%／125% FNA 预览中的奇物列表、净土露滴材料与底栏，以及 125% 研究页面。65 张正式描边检查和模组编译通过，游戏内分类切换尚未人工验证。

2026-10-25 大锅交互重写：`pwsh -File Tools/AlchemyPreview/run.ps1 -Scene cauldron-animations` 导出 `cauldron-animation-frames.png`，直接链接正式 `AlchemyBrewAnimation` 的选帧、偏移与原角色贴图，包含翻书 11 帧、搅拌 13 帧；这是独立完整角色帧图预览，未模拟游戏内帽子、项链、染色及光照。`-Scene brew-050` 检查手记制作中显示，已查看默认与 125% 缩放。正式目录资源完整性检查通过；无新增材料、配方或图标。游戏内点击、角色与大锅的完整遮挡和真实联机仍需验证。

2026-10-25 大锅姿势位置修正：`-Scene cauldron-animations` 现在输出 `cauldron-position-2.00.png` 与 `cauldron-position-1.25.png`，使用正式 `AlchemyBrewAnimation` 坐标函数、翻书/搅拌/帽子 PNG，以及原版 Extra_102 凳子和 Tiles_96 大锅资源组合绘制。覆盖翻书 11 帧、搅拌 13 帧；验证凳子真实尺寸为 28×28，半高绘制为 14 像素，双脚与凳面、凳脚与地面严格接合。已打开检查两种缩放下所有动画帧的脚部支撑与帽子贴合。此前仅查看人物帧图漏掉了旧凳子偏移和帽子基准问题，本场景补上凳子、帽子、大锅和地面。地面和瓦片排布是场景夹具，尚不替代游戏内完整绘制链、光照与真实联机验证。正式游戏只加载既有贴图，未新增运行时纹理生成。

2026-10-26：修正上轮 14 像素垫高过大的问题。对照旧版 `portableStoolInfo.SetStats(7, 7, 7)` 和正式动作贴图，垫高恢复为 7 像素，28×28 凳子纵向压缩为 0.25 倍；宽度不变。新增每一搅拌帧的真实棍尖像素检查：帧内 y=35 的深色棍尖必须落在大锅距地面 28 像素的锅口实像素上，同时仍校验脚底/凳面/地面接合。已重新生成并打开检查全部 13 帧、2 倍和 1.25 倍 FNA 合成图；源码编译通过。游戏内完整绘制链仍待重载验证。

2026-10-26 止痛药研究等级由 Lv.1 调整为 Lv.5（炼金 Lv.5 需角色 Lv.25，蜂王后定位），上文“止痛药没有可达的低等级状态”已不适用：批量 `recipe-scrolls` 现在会生成 `recipe-painkiller-level-low`（Lv.4 持有配方）。本次未新增材料或图标，尚未重新运行该场景的离线预览。

2026-10-26 轻羽药水研究等级由 Lv.2 调整为 Lv.4（世吞／克脑后），上文“轻羽为 Lv.2、不足场景 Lv.1”已不适用：`recipe-featherlight-level-low` 现为 Lv.3 持有配方。尚未重新运行该场景的离线预览。
