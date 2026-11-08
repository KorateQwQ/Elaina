# 动作拖尾共享弹幕 Bloom 检查

运行：`powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ActionTrailPreview/run.ps1`

复用 `BloomBackbufferPreview` 的实际 KL 后处理抽取器，每次从当前源码抽取动作的提交、采样与拖尾方法、挥杖节点几何、共享 RequestTrailBloom、KL TrailEffect 和队列实现。使用正式 LightTrail PNG、VertexDraw 和 Bloom Shader。玩家、动作帧和相机由独立 FNA 场景提供；不运行游戏逻辑。

检查无活动 SpriteBatch 时可以提前提交、自动拖尾数组快照、暂停时停止采样、挥杖节点的开始/结束边界、共享 Projectiles.After 且无额外 Bloom 层、队列只消费一次，以及后续弹幕直接 Draw 时不残留拖尾 Shader。生成进度 0.1、0.5、0.9 的 Bloom 开关对照，共六张 640×360 图片，输出至 `.vissandbox/action-trail-preview/out`。上方为自动拖尾，下方为固定中段进度的挥杖拖尾，左上白方块用于检查后续绘制状态。

这是 FNA 离线检查。玩家遮挡、实际 CacheProjDraws 钩子顺序及游戏内动作切换仍需游戏验证。Tools 与 .vissandbox 已从正式打包中排除，预览代码以 `.cs.txt` 保存。

星星回归同时抽取当前 `RequestBloomStar` / `QueueBloomStar` 和闪烁计算，加载正式十字星、光线贴图及 ReColor Shader，在当前 OverPlayers.After 队列执行。另输出六张 `star-*.png`：进度 0、0.5、1 的 Bloom 开关对照。左侧紫星只画十字贴图，读取浮点捕获目标验证无贴图叠加时 HDR 峰值为 1.5；两端为 0。中间水蓝与右侧橙色包含完整光线。检查后续白色方块未被染色，及星星区域存在 Bloom 差异。
