# 固定纹理离线预制

将伊蕾娜模组正式代码中的固定纹理生成移到构建工具，游戏中只调用 `ModContent.Request<Texture2D>`。原先的九次纹理创建合并为八张正式 PNG，两个白像素使用同一资源。

| 用途 | 正式资源 | 尺寸 |
| --- | --- | --- |
| 共用白像素 | `Effects/Tex/Baked/WhitePixel.png` | 1×1 |
| 瓶中雨噪声，R 随机/G 消融场 | `ElainaModAlchemy/Rain/Assets/CloudNoise.png` | 256×256 |
| UI 柔光点 | `ElainaModSkills/UICore/Assets/GlowDot.png` | 128×128 |
| UI 柔边圆环 | `ElainaModSkills/UICore/Assets/SoftRing.png` | 128×128 |
| 升级星环细圆环（非原生成器移出，新增） | `ElainaModSkills/UICore/Assets/ThinRing.png` | 256×256 |
| 升级头顶星芒（仿 KL T_StartFlare001 的纯白版，新增） | `ElainaModSkills/UICore/Assets/StarFlare.png` | 128×128 |
| UI 四芒星 | `ElainaModSkills/UICore/Assets/Star4.png` | 128×128 |
| UI 菱形 | `ElainaModSkills/UICore/Assets/DiamondFill.png` | 64×64 |
| 传送门线条横截面 | `ElainaModSkills/Skills/Recall/Assets/PortalLine.png` | 1×32 |
| 冰霜准星 fbm | `ElainaModSkills/Skills/Ice/Assets/ReticleNoise.png` | 512×512 |

资源清单位于 `Effects/Tex/Baked/manifest.json`，记录尺寸、RGBA SHA-256、透明度编码。RGBA8 总像素量为 1,523,844 字节，未增加图集/mipmap；不据此宣称帧率提升，改动主要移除运行时固定像素计算和上传路径。

`ThinRing` 是 2026-10-28 为升级特效新增的固定纹理：半径 120/128、截面 σ=2 像素的高斯细环，生成函数在 `Bake.cs.txt` 末尾，同样以 `(a,a,a,a)` 存储并写入清单。

`StarFlare` 是 2026-10-29 为升级头顶星芒新增的纯白四芒星：形状仿 KL `T_StartFlare001`（向核心加粗的光臂、尖角亮芯、淡柔光），但去掉了原图横臂的彩虹色散——缩到几十像素绘制时会采到偏红的行，染成淡紫后显出一条红线。柔光与尖角亮芯乘径向窗口在半径 60 内归零，贴图边框 alpha 为 0；否则提亮到 HDR 后整张贴图的矩形边界会显出来。

## 生成与编码

`OriginalFixedTextures.cs.txt` 保存从原正式代码移出的生成算法：使用相同 .NET 8 / FNA `Color`、`MathF`、随机种子 19781024 和计算顺序。控制台导出原始 RGBA，`write_png.py` 原样保存 PNG；没有重新近似绘制。雨云的 129 项消融阈值同步导出到正式 `RainCloudBakedData.cs`，避免在游戏内读回噪声并排序。

**不要再次预乘或反预乘。** 本机 tML `ImageIO.ToRaw` 保留非零 alpha 像素的 RGB，仅清零 alpha=0；因此 UI/线条 PNG 存 `(a,a,a,a)`，噪声存不透明数据。校验覆盖 PNG、FNA 加载后的 GPU 数据以及打包后的 rawimg，三者均与原生成像素逐字节一致。

```powershell
pwsh -File Tools/FixedTextureBake/run.ps1
pwsh -File Tools/FixedTextureBake/run.ps1 -Check
pwsh -File Tools/FixedTextureBake/preview.ps1
pwsh -File Tools/FixedTextureBake/package.ps1
```

正式代码只加载贴图，不包含生成器。卸载时清理引用，由资源系统释放共享纹理；不会自行 Dispose 借用资源。雨云绘制也已去除白纹理创建、Effect 克隆、反射状态恢复，结束调用 KL `EndBeginDraw()`。

## 范围与验证

- 范围为当前伊蕾娜模组的正式 C#。已检查显式/目标类型 `Texture2D` 创建、`SetData`、流式图片加载；剩余 `new Texture2D[...]` 是引用数组，不是 GPU 纹理。KL/SUI 等依赖的内部实现不在本次修改范围。
- 屏幕合成、3D 深度和含动态/本地化文字的 UI 捕获仍使用 RenderTarget；它们不是固定图案。独立工具为导出、对照和截图创建纹理也不属于游戏运行时。
- 48 项 FNA 检查覆盖八张 GPU 纹理原始字节、深浅背景与 100%/125% 缩放，以及正式 IceReticle XNB + 当前正式参数方法的四个阶段。输出 `texture-review.png` 和 `reticle-*.png`，已打开检查。
- 瓶中雨更改前后的本次新生成截图逐像素一致，包括出现、完整、消散、明亮背景和 125% 缩放。技能轮盘改为加载正式 PNG，已查看 150% 图。传送门加载预制线条后通过正式 KL Bloom 预览并生成 439 帧，已查看帧条；239 项既有行为检查通过。
- `package.ps1` 在独立目录打包，不覆盖正在运行的游戏；确认八张 rawimg 的尺寸/RGBA 哈希、当前 DLL、TMOD 哈希和工具排除项。输出位于 `.vissandbox/fixed-texture-bake/package-save/Mods/`。
- 通过 C# 编译与炼金正式资源完整性检查。尚未在真实游戏及多人环境验证资源替换，需要重新构建并加载模组后使用。

`reference/RainCloudShaderRenderer.cs.txt` 是改造前实现的工具侧留档，不参与正式模组编译或打包。后续调噪声/固定图案请修改离线生成器并重新运行，不恢复运行时生成。
