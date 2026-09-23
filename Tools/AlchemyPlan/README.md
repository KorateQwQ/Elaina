# 新版炼金手记

此目录对应最新策划，维护 **15 件造物与 8 种模组素材**。三个 HTML 使用同一份数据与界面逻辑，区别仅为默认图标画风：

- `elaina-battle-eight-skills-alchemy.html`：默认彩铅。
- `elaina-battle-eight-skills-alchemy-all-pencil.html`：默认彩铅。
- `elaina-battle-eight-skills-alchemy-all-pixel.html`：默认像素。

都可以在「彩铅 / 像素 / 原画」之间切换。页面展开全部条目用于展示；效果、获取阶段、配方和产量写在 `plan.json`。配方中尚未确定的值集中列于 `assumptions`，便于后续调整。蜂蜜面包沿用此前已确认的圆面包图标。回声花粉已删除，「隔离药水」统一改为「净土露滴」。

物品框和详情栏使用统一的深紫色样式，条目展示图标、名称与产量。选中标记来自 `ui/selection-quill.svg`，构建时嵌入 HTML；22×42 像素的羽毛笔平滑移动到当前物品框的右下边缘，停靠位置按标记实际尺寸计算。像素图标保持整数倍显示。

所有彩铅物品与素材图标统一使用月露合剂试改中已确认的加强排线。`plan.json` 的 `pencilProfile: "hatched"` 是当前默认值；`pencil_style.py` 仍保留 `soft` 用于旧效果对照。

## 资产

`ElainaModAlchemy/item/ExampleAssets` 中，每个当前模组物品都有 `.svg`、`_Pencil.png` 和 `_Pixel.png`；彩铅 PNG 为 100×100，像素 PNG 保持原生尺寸。新增 13 种造型，复用 10 种已确认造型，星力药水继续采用一大一小两颗十字星。

原版材料位于 `ExampleAssets/Materials`，只生成 28 个简化彩铅 PNG，没有新增原版材料的像素资产。瓶装水使用水滴。旧版中不再展示的样例资产继续保留，但不在新手记的条目中使用。

## 重建

需要 Python + Pillow、Node.js + sharp：

```powershell
python Tools/AlchemyPlan/build_art.py
node Tools/AlchemyPlan/render_art.cjs --sharp-module "<已安装的 sharp 模块目录>"
python Tools/AlchemyPlan/build_notebook.py
```

只刷新 `ExampleAssets` 中现有的全部彩铅 PNG（包含保留的旧样例，不改写 SVG 和像素 PNG）时运行：

```powershell
python Tools/AlchemyPlan/build_art.py --pencils-only
node Tools/AlchemyPlan/render_art.cjs --pencils-only --sharp-module "<已安装的 sharp 模块目录>"
```

`artwork.py` 保存新 SVG 与原版素材符号；`pixels.py` 保存整数网格像素绘制；`pencil_style.py` 延续已确认的彩铅处理。`notebook.css` 与 `notebook.js` 分别保存视觉样式和展示交互，`notebook-shell.html` 保留现有书本动画、装饰和战斗 HUD。

原版造型的复用来源保留在旧样例资产中；不运行旧版 `Tools/AlchemyPixel/ExportAlchemyArt.cjs` 更新这份策划。当前任务没有修改游戏内物品效果或物品类引用。

## 展示行为

- 分类为魔药、奇物、料理、素材；购买或取得型素材展示来源。
- 月露与温香树脂可以演示炼制，产物增加到素材行囊中。
- 椎骨 / 腐肉、铁锭 / 铅锭按合计数量显示并消耗。
- 制作会改变本地演示数量；补充与重置只影响该页面的展示数据。
- 灰赝尘的替代机制、滴管和雨云效果等仅做文字展示，没有实现对应游戏机制。

`generated/plan-art-overview.png` 是离线资产对照，不代表浏览器整页或游戏运行截图。
