# 炎爆领域施法演出预览（HTML 方案稿）

启动：`.claude/launch.json` 中的「炎爆领域施法预览」（`python -m http.server 5591 --directory Tools/FireBurstCastPreview`）。

用途：比较施法动作的节奏与分层（现状 / 方案 A 举杖聚火 → 点地爆燃 / 方案 B 抛种悬燃 → 坠地爆燃 / 方案 C 杖尖聚火 → 天空星环 → 坠地爆燃），不是正式绘制的像素级还原。

用户倾向方案 C：杖尖施法光效持续到下压，环形星光在天空成形后连同星环坠落；爆燃后不要上升光柱，即取消领域生成时在中心落下的第一道炎爆（仅“现状”保留该示意）。

- 贴图为正式资源的白色遮罩副本（`GlowDot`、`Star4`、`StarFlare`、KL `ShotLine`、`魔杖`），在页面内着色；Bloom 用 Canvas 模糊叠加近似。
- 火环沿用 `FireBurstDomainRingDrawing` 的展开曲线与火舌长度，着色为近似；“现状”中的中心第一道炎爆为简化示意（星芒 30 帧 → 火柱）。
- 世界坐标：领域中心为原点，地面在 y = 21，视野 1920×1080；「近景」为 2 倍。
- 分镜：`?sheet=A&zoom=4&frames=2,8,14` 把指定帧拼成一张图（`sheet` 取 `now` / `A` / `B` / `C`）。

方案 C 已接入正式代码（`FireBurstCastVisuals`、`Action_FireBurst`、`FireBurstImpactDust`），用 `Tools/FireBurstPreview/run.ps1 -Cast` 以正式绘制代码与 KL Bloom 核对；本页只作方案对比，不作为验收依据。
