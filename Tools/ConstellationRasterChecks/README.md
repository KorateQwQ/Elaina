# 技能面板的小尺寸栅格验证

此入口直接抽取正式 `ConstellationDrawing` 的帽子、星形、点、圆环和文字方法，链接正式 `ConstellationFont`，用本机 FNA 绘制。只需要已安装的 tModLoader、.NET 8 SDK 和项目资源，不依赖旧小字号 XNB、旧电脑的字体生成器或 Terraria / SUI 源码。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ConstellationRasterChecks/run.ps1
```

安装目录不同可传 `-Tml`。输出在 `.vissandbox/constellation-raster/after`。86%、100%、112.5%、125%、150%、200% 各输出 PointClamp 与 LinearClamp 两张原生尺寸图；使用真实非整数 UI 矩阵和小数原点。检查 79 个字体图集、104485 个字形记录的完整性，以及 100% / 112.5% 标题的采样器无关性（RGB 误差最多 2）。旧标题在这些尺寸从 48px 母版直接缩小时不能通过后一项检查。

这次修正保留 1100×800 布局和命中坐标：

- 帽子离线面积滤波到接近实际尺寸的层级，保留原图的灰色填充和预乘 Alpha。
- 研习点书本也按实际显示尺寸选层级；24px 以下保持原书页与书脊轮廓，减少页内细节并调整笔画宽度，分别覆盖 13px 按钮、15px 需求行和 27px 页头。同一图集增加炼金标题徽记，各自保留原素材。
- 技能面板使用独立的密集星形图集 `StarsRaster`，4–24px 每个整数尺寸都有一层；原 `Stars` 图集继续供其他消费者使用。
- 点使用有透明边距的小尺寸遮罩；虚点圆环按实际屏幕周长选择点数，8px 圆环不会再挤入 40 段亚像素短线。细线用预制透明边缘遮罩抗锯齿。
- 字体从现有 48px 母版离线积分覆盖率，保存为无损 8-bit Alpha PNG 与字形度量。正文 9–28px、衬线 9–48px、拉丁 8–26px，补齐原字号表空档；字符集由独立的 `charsets.json` 维护，分别为 2408、1363、95 个 Unicode BMP 码点。运行时仅加载纹理与字形数据，不生成贴图；超出范围或字符集直接回退原始母版。正文测量、换行和绘制共用新度量。
- 正式技能面板显式使用 LinearClamp，结束后恢复调用方绘制状态。其他 UI 的字体及全局采样器不变。

重建固定遮罩与字体：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ConstellationRasterChecks/bake.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ConstellationRasterChecks/run.ps1 -Label bake-fonts
```

所有正式资源位于 `ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets`，`FontRaster` 的 `.png` 和 `.bin` 都需要打包。新增字体约 25 MiB（打包压缩前），按实际用到的字号加载；字体来源仍是 KL 中现有的三套母版。

旧字体清理后，重建只读取三套母版和 `charsets.json`，不从旧小字号文件或现有输出反推字符集。新增文字时在对应清单追加码点并确认母版覆盖，再运行 `-Label bake-fonts`。检查包含各字号字符集与清单一致、母版覆盖、缺字及超出字号边界的直接回退；原始母版未覆盖的字符需先维护母版。

清理验证：删除 54 份旧 XNB 后重建的 158 个字体 PNG / BIN 与清理前逐字节一致，12 张原生细节图也一致；KL 编译通过。后续书本与炼金徽记更新只影响共用遮罩图集。炼金界面现在复用同一批字体资源；其嵌套卡片变换、缩放和中英文长文案由 `Tools/AlchemyPreview` 验证。

2026-11-04 验证：12 张细节图及 24 张整页图导出成功；整页覆盖 1366×768 / 100%、1864×977 / 125%、1920×1080 / 100%、112.5%、125%、150%、2560×1440 / 150%、3840×2160 / 200%。整页入口为 `Tools/ConstellationSkillPanelPreview/run.ps1 -ResolutionOnly -SkipChecks -Configuration Release`，新电脑可用 `-Sources` 与 `-SilkySources` 指定从已安装程序集取得的匹配依赖类型。隔离预览不代表真实角色状态，也不覆盖游戏内输入树和重载。

本机完整编译的现存阻塞：.NET SDK 8.0.427 在 `ElainaModAlchemy/Revealing/RevealingRecipeChests.cs:102` 报 CS8344（迭代器内使用 ref struct 枚举器）；修改前后相同。本次正式绘制、字体与富文本代码通过离线工程编译与渲染，但未关闭正在运行的游戏或替换已加载模组。

## 实线圆环（2026-11-05）

升级卡片的 1.2px 描边乘自身 1.35 倍后为 1.62px，原来会越过 `Line` 的 1.5px 抗锯齿分支，回到 160 段旋转硬边矩形。正式 `Ring` 的正圆路径现在使用 `Effects/Content/UICircle.fx/.xnb` 按屏幕像素覆盖率绘制，保留描边中心半径、颜色和连续的小数坐标；同时作用于显现/启封提示、技能节点和选中扩散圈。虚点环不变。非正圆、`Arc` 和其他消费者的硬边 `Line` 仍是另行排查的范围，不能据此认为所有 UI 已修复。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ConstellationRasterChecks/compile-circle.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ConstellationRasterChecks/run.ps1 -Label circle-aa
```

编译脚本输出正式 XNB，预览直接加载该字节码，并每次抽取正式 `Ring` 方法。没有运行时生成固定纹理；Shader 根据连续变化的半径和描边求覆盖率，复用引擎白像素作为绘制载体。

新增 54 组检查：半径 4/20/29/32/40/72、86%–200% UI 缩放，升级圈另含 4 个弹出缩放姿态。用独立的 16×16 面积积分作参考，保留修改前的 160 段画法作反例；在升级停留的 100% UI 下，边缘区域平均 Alpha 误差由 0.1047 降至 0.0323，旧实现无法通过 0.065 的上限。另检查 Point/Linear 采样结果一致，以及带平移的非整数矩阵、裁剪区域、混合/采样/深度/栅格状态和圆环前后绘制顺序。输出 `ring-*-before/after.png` 与 `ring-checks.txt`；这些局部图均未缩放。

2026-11-05：54 组圆环检查及原有字体/图集检查通过；完整升级提示 28 个原生画面、24 张技能面板整页和显现/启封提示成功导出，并查看低分辨率、非整数缩放和 4K/200% 代表图。完整提示入口见 `../ConstellationSkillPanelPreview/README.md`。正式 C# 使用 `dotnet msbuild 伊蕾娜.csproj -t:Compile -p:Configuration=Release -p:BuildProjectReferences=false` 编译通过（KL 依赖已先编译）。完整模组打包被运行中的 tModLoader 以 TML003 阻止；游戏内加载/视觉验证尚未执行。
