# 炼金 UI 物品资产

> 此目录保留旧版 22 项样例的生成工具。最新策划与三个 HTML 的重建入口已迁移到 `../AlchemyPlan/README.md`；请使用新版流程更新当前页面。

`ElainaModAlchemy/item/ExampleAssets` 是 22 件炼金物品的交付目录，每件只有三种资产：

- `Name.svg`：从原炼金 HTML 提取的独立原始 SVG。
- `Name_Pencil.png`：统一彩铅画风，100×100 透明 PNG，沿用全物品彩铅预览中已确认的绘制规则。
- `Name_Pixel.png`：直接在原生像素网格绘制的 RGBA PNG，尺寸随物品变化，可见像素不透明。

名称、中文物品名、HTML id、尺寸、颜色数与路径对应关系见 `manifest.json`。这里的 `ManaElixir` 对应 HTML 中的「魔力合剂」；它是资产名称，不更改游戏中已有物品类和贴图引用。

## 重建

需要 Python + Pillow、Node.js + sharp。所有路径相对于项目，可从任意目录运行脚本；下面以项目根目录为例。

```powershell
node Tools/AlchemyPixel/ExportAlchemyArt.cjs --sharp-module "<安装的 sharp 模块目录>"
python Tools/AlchemyPixel/BuildAlchemyPixels.py
python Tools/AlchemyPixel/PreviewAlchemyAssets.py
```

如果 Node 可以直接解析 `sharp`，可省略 `--sharp-module`。上述重建流程会更新本目录 manifest 和对应的 66 个资产，不会重写 HTML 或物品类。

像素图层和调色板位于 `BuildAlchemyPixels.py`；药瓶与面包的已确认原稿分别复用 `../PotionPixel/GenerateManaElixirPixel.py` 和 `GenerateHoneyStarBreadPixel.py`。原始 SVG 与彩铅稿来自 `NewUIExample` 下的原始、全彩铅两份 HTML，导出器会检查它们的原画一致。

`preview/index.html` 可逐件查看三种资产并切换背景；PNG 总览是离线检查，不代表已测试游戏中的光照与绘制。这些工具和预览位于构建排除的 `Tools` 目录。
