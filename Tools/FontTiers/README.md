# KL UI 字体的精确像素字号与裁剪

48px 母版字体缩小到 10–13px 绘制时，线性采样会丢笔画，所以 1080p 下的小字会糊。KL `PixelFont` 改为按屏幕上的实际字号（设计字号 × 面板缩放 × UI 缩放，四舍五入到整数像素）选用对应的烘焙字体，并按 1:1 对齐设备像素绘制。测量也用同一字体，因此换行宽度与绘制结果一致。

- 支持的显示配置：1920×1080 / 100%、2560×1440 / 150%、3840×2160 / 200%。这三种配置下面板都保持设计尺寸，屏幕字号就是设计字号的 1、1.5、2 倍。
- 字号表在 `build.ps1` 的 `$sizes` 中，KL `FontManager` 中的范围必须与之一致。超出字号表的字号（HarmonyOS 与 Noto 28px 以上、Gelasio 26px 以上）沿用 48px 母版缩放，缩放比不低于 0.54，不会丢笔画。
- 技能面板的中文小于 14px 时绘制时加大 1px（14–16px 间逐渐减到 0，见 `ConstellationDrawing.Legible`），布局代码仍写 HTML 参考字号；`-FontSizesOnly` 列出的是加大后的屏幕字号。
- 新增字号前，先运行 `Tools/ConstellationSkillPanelPreview/run.ps1 -FontSizesOnly -SkipChecks -Configuration Release`，它会列出三种配置下面板实际请求的全部字号。
- 只烘焙界面可能出现的字符：基本拉丁与中日文标点、本模组和 KL 的本地化、两个模组 C# 代码中的字符串字面量。HarmonyOS 另加各语言的原版物品名（需求列表会显示原版物品），Noto Serif 的精确字号只用于标题与技能名，不加物品名。精确字号缺字时，该段文字改用母版缩放，只是不够锐利。
- Noto Serif 的 48px 母版也按同样规则裁剪（含原版物品名，因为炼金界面用它显示物品名），母版缺字会显示 `?`。新增本地化或界面文字后，务必重新运行 `build.ps1`。完整字符范围仍保存在 `KL/Fonts/NotoSerifSC.dynamicfont` 中。

## 生成

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/FontTiers/build.ps1
```

所需输入：DynamicFontGenerator（`D:\泰拉原版贴图与代码与其他工具\FontBuilder`）、HarmonyOS Sans SC Regular TTF、`.vissandbox/font-integration/regular/NotoSerifSC-Regular.ttf`、`.vissandbox/gelasio-build/KLGelasio.ttf`（后两个是改名为 `KL …` 字族的 TTF，与母版描述中的 `FontName` 对应），以及原版简体中文本地化 `D:\泰拉原版贴图与代码与其他工具\tModLoader\Terraria\Localization\Content\zh-Hans`。脚本在 `.vissandbox/font-tiers` 中生成描述文件并烘焙，然后替换 `KL/Fonts/<字体>_<像素>.xnb`。

## 验证

`Tools/ConstellationSkillPanelPreview/run.ps1 -ResolutionOnly -SkipChecks -Configuration Release` 按三种配置的原生分辨率导出面板（`output/<culture>/resolution-comparison/`），请在图片查看器中以 100% 比例检查。
