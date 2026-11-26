# 动作拖尾共享弹幕 Bloom 检查

运行：`powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ActionTrailPreview/run.ps1`

复用 `BloomBackbufferPreview` 的实际 KL 后处理抽取器，每次从当前源码抽取动作的提交、采样与拖尾方法、挥杖节点几何、共享 RequestTrailBloom、KL TrailEffect 和队列实现。使用正式 LightTrail PNG、VertexDraw 和 Bloom Shader。玩家、动作帧和相机由独立 FNA 场景提供；不运行游戏逻辑。

检查无活动 SpriteBatch 时可以提前提交、自动拖尾数组快照、暂停时停止采样、挥杖节点的开始/结束边界、共享 Projectiles.After 且无额外 Bloom 层、队列只消费一次，以及后续弹幕直接 Draw 时不残留拖尾 Shader。生成进度 0.1、0.5、0.9 的 Bloom 开关对照，共六张 640×360 图片，输出至 `.vissandbox/action-trail-preview/out`。上方为自动拖尾，下方为固定中段进度的挥杖拖尾，左上白方块用于检查后续绘制状态。

这是 FNA 离线检查。玩家遮挡、实际 CacheProjDraws 钩子顺序及游戏内动作切换仍需游戏验证。Tools 与 .vissandbox 已从正式打包中排除，预览代码以 `.cs.txt` 保存。

杖尖星芒与光晕走 `OverPlayers.After` 普通队列。运行 `Tools/ActionTrailPreview/run.ps1 -BakeHalo` 从正式亮芯和 KL 的五级 Bloom 结果提取灰度光晕，写入 `ElainaActions/Assets/WandStarHalo.png`。128×128，中心原点，预乘灰度 RGBA，运行时按技能染色和闪烁。重跑默认检查可查看八支魔杖重叠、分散与非整数缩放的对照图，并检查星芒的 Bloom 请求数为零。`QueueReference` 仅供烘焙和 KL 局部 Bloom 对照使用。

## 2026-11-18：透明度、重合点与中断收尾

当前入口改用 `Game.RunOneFrame()`，在初始化、开始绘制和批量导出过程中检查 SDL 隐藏状态。新增 `ElainaTrailSnapshot` / `ElainaTrailAfterimage` 与正式 `PostUpdatePlayers` 的源码抽取，保留真实 KL `TrailEffect`、`VertexDraw.xnb` 和 Bloom 绘制。

修复内容：

- 魔杖统一使用 `AlphaBlend`。整体透明度一次作用于 RGB/Alpha，空间尾部透明度由 `TrailEffect` 的可选 `premultiplyOpacity` 同步作用于 RGB，Shader 再作用于 Alpha。未修改共用 FX/XNB，也未改变其他调用者的颜色约定。
- `GetActionAlpha` 改为 SmoothStep 包络。LightTrail 使用现有正式 PNG，不重新预乘贴图。预览逐像素核对 `Texture2D.FromStream` 与 tML `ImageIO.ToRaw` 的读取结果，确保并未重复预乘。
- `TrailEffect` 保留重合点的年龄位置，使用相邻有效切线避免零向量归一化；全静止条带不生成顶点。插值使用 `i / (count - 1)`，尾端宽度、透明度确实到达设定值。此修复覆盖 `TrailEffect`，不代表其余 `VertexDrawEffect` 等绘制入口已做全量退化几何审计。
- 历史采样移至 `PostUpdatePlayers`，暂停不采样，引导保持帧仍采样；绘制不改写历史。动作中断保留上一绘制帧的不可变世界位置/姿势/颜色快照，8 个游戏帧 SmoothStep 淡出。自然结束不复活拖尾，死亡和退出世界清理残留。
- 挥斩节点的 `EndRecoverDuration = 0` 与其原注释保持一致，立即回收。

验证：`run.ps1` 通过，输出 19 张隐藏 FNA 图片，包括 `geometry-*.png`、`afterimage-{0,2,4,6,8}.png`。实测 RGB 与 Alpha 在透明度 1 / 0.5 / 0.25 / 0 时同比例衰减；每游戏 tick 绘制 0 / 1 / 4 次所得历史完全相同。检查保持帧、暂停、快照修改隔离、中断寿命、正常结束、死亡与退出世界。当前单十字星峰值为 2.0（由正式 `DefaultStarIntensity` 决定），上面的 1.5 为历史记录。

故障注入：`powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ActionTrailPreview/check-faults.ps1`。它只修改独立预览程序集中的抽取源码，分别注入重复 Alpha、零向量归一化、绘制采样、缺失中断快照，要求四次运行按对应断言失败，再运行正常版本；不会改写正式代码。四项旧故障均已被回归拒绝。

爆燃专项：`powershell -NoProfile -ExecutionPolicy Bypass -File Tools/FireBurstPreview/run.ps1 -SkipShader -WandTrail`，42 帧、昼夜与 1/1.25/2 倍缩放通过，已查看原尺寸局部图。完整 `-Cast` 仍在既有曝光检查失败（第 30 帧夜景、1 倍缩放，571 个近白像素超过 120），与原工具文档记录一致，未放宽该检查。

KL 与伊蕾娜正式 C# 使用 `dotnet msbuild <项目>.csproj '-t:Compile;CopyFilesToOutputDirectory' -p:BuildProjectReferences=false` 分别编译。完整模组打包被运行中游戏阻止（TML003），未重载游戏；真实多人中断/遮挡仍需游戏内验证。
