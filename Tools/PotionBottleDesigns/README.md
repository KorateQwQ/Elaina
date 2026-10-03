# 魔药瓶型候选

集中药水与净土露滴的竖版像素图已获用户确认并接入正式物品，尺寸分别为 19×38、19×34。共鸣药水与轻羽药水也已确认并接入正式物品，尺寸分别为 31×39、19×38；详见 `../PotionPixel/ConcentrationPotion.md`、`PurificationDew.md` 与 `ResonanceAndFeatherlight.md`。星力三角瓶与月露合剂圆瓶的像素图也已补齐并接入，星力手记彩铅图同步切换为三角版，见 `../PotionPixel/StarAndMoonPotions.md`。手记彩铅图也已同步：星力三角瓶、集中与轻羽斜向试管、共鸣新圆瓶、净土虹彩斜滴管。彩铅保持原 SVG 构图，物品像素图独立采用竖直方向以保证像素规整；运行 `python Tools/PotionBottleDesigns/apply_notebook_potions.py` 重建（原始候选不覆盖，新稿保存在 `output/round1/approved`）。下方保留此前的候选设计过程。

这是一轮待选择的设计稿，只写入本目录，不修改现有物品、图鉴、像素贴图或剪影。

运行 `python Tools/PotionBottleDesigns/generate.py` 重建。依赖 Pillow、现有的 `Tools/AlchemyPlan/pencil_style.py` 和 svg-to-png 技能的 `convert_svg.py`；转换脚本可用 `--converter` 指定。

- `output/svg/`：9 个可独立编辑的原始 SVG（A—H 初稿与新增试管 I），透明背景，64×64 视图，100×100 固有尺寸。
- `output/pencil/`：复用目前 `hatched` 彩铅处理的派生 SVG。
- `output/png/`：256×256 原稿/彩铅预览，46×46 现有/候选/剪影对比，以及从实际 `CatalogEntry.PixelPath` 所指的 ExampleAssets 导出的原像素对照（整数倍最近邻缩放、居中于 64×64 画布）。使用标准 straight alpha，仅用于设计预览。
- `output/bottle-shortlist.png`：用户认可的 A/B/C 与新增试管 I 的对照表。`bottle-concepts.png` 保留第一轮八款历史总览。
- `output/index.html`：支持切换原稿、彩铅和剪影，放大对照并选择备选编号的本地页面。

A 三角烧瓶（星力）；B 圆肚月纹瓶（圆瓶比例参考）；C 菱形棱晶瓶（嗜血）；D 六角刻度瓶（集中）；E 扁圆共鸣瓶（共鸣）；F 细颈轻羽瓶（轻羽）；G 双泡葫芦瓶（净土）；H 心形药瓶（嗜血的备选方向）。用途均为建议，尚未接入游戏。

用户已确认月露合剂各版本的圆瓶很好，保留其现有资源；B 仅是圆瓶比例参考，不是月露替换提案。其他瓶型选定后，SVG、彩铅、原生像素版和未解锁剪影必须使用同一瓶身轮廓、瓶口、主色与标志。候选的新像素版尚未制作，不能把原像素对照列误认成候选像素版。

第二次反馈：用户认可 A、B、C，其他造型不够自然。默认页面仅显示这三款，D—H 暂存并折叠；`output/shortlist.json` 保存本次选择。B 可供其他药水采用，月露继续使用已有圆瓶。本轮未获具体资源替换指示。

第三次反馈：增加普通实验室试管方向，不要异形。候选 I 采用直筒圆底、普通塞口、小标签与刻度，取消飘带、瓶肩、收腰和大面积符号；与 A/B/C 一起展示，尚未选定。

第四次反馈：试管斜放更好。I 调整为向右倾斜 32°，瓶口朝右上、圆底朝左下，药液反向补偿旋转以保持液面水平；原稿、彩铅与剪影一同更新。`output/test-tube-detail.png` 为本次试管的放大与 46px 对照。

预览是 SVG 渲染与静态对照，不是 FNA 或游戏截图。选定后再做正式资源替换、预乘 Alpha 导出、未解锁剪影和物品像素版适配。

## 按药水适配的第一版

运行 `python Tools/PotionBottleDesigns/batch_v1.py`，输出至 `output/round1/`；页面入口为 `output/round1/index.html`，总览为 `potion-batch-v1.png`。复用 A/B/C/I 四种获认可的造型与目前的彩铅处理，药液颜色取自对应药水的现有 SVG。

当前分配：01 嗜血→C 菱形；02 星力→A 三角；03 集中→I 斜试管；04 共鸣→B 圆肚；05 轻羽→I 斜试管；06 净土→J 滴管新稿。每种单独保存 SVG、派生彩铅 SVG、100×100 彩铅候选及 46/32 像素显示尺寸预览。46px/32px 是彩铅图标的显示大小，不是新物品像素画。

用户明确：此阶段不制作像素图，只有月露合剂的圆瓶已确定；其余分配均待逐个挑选。因此脚本不生成像素画，也不替换生产资产。月露及止痛药只复制现有彩铅图到对照区。最终选定后，才统一原稿、彩铅、真正的像素版、未解锁剪影与游戏引用。

最新反馈：01—05 用户评价不错，保留本轮设计；06 净土露滴改试独立滴管。`dropper.py` 绘制 32° 斜放的橡胶吸头、金属套环、细玻璃管和脱离管尖的绿色露滴，保留原药液主色。`output/round1/purification-dropper-detail.png` 展示原稿、彩铅、46/32px 和轮廓。原三角方案另存 `output/round1/history/`。其余五款的源 SVG 和所有预览 PNG 经 SHA-256 对照保持不变；此轮仍未制作像素画或接入游戏。

## 净土露滴配色试稿

用户认为深绿色像毒液，希望尝试彩色、神圣的感觉。运行 `python Tools/PotionBottleDesigns/dropper_palettes.py` 生成 `output/round1/palettes/`：A 虹彩珍珠（珍珠白吸头、淡金套环、冰蓝/淡紫/粉金药液）；B 象牙白金（乳白、香槟金）。两版保持相同滴管轮廓，仅改颜色，单独输出 SVG、彩铅及 46/32px 小图。彩铅版对选中的多色渐变保留原色，并叠加透明排线，避免现有转换器把虹彩压成单一中间色。主页面提供试色入口，原绿色方案仍作历史对照；其他五款与生产资源保持不变，仍不制作像素画。
