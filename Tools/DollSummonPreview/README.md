# 玩偶召唤/收回特效预览

正式代码：`ElainaModSkills/Skills/DollControl/DollSummonVisuals.cs`（曲线、星屑模拟与绘制，游戏与预览共用）、`DollSummonDust.cs`（KL 粒子托管，仅游戏）、`DollPetProjectile.cs`（出现计时、收回状态、绘制缩放与泛白）。

## 表现（2026-10-25）

- 召唤：四只按编队编号每 6 tick 依次出现。轮到时先爆出粉紫四芒星（魔杖星光同款 KL `T_StartFlare001` + 两条 `ShotLine`），玩偶从星芯以过冲缓动弹出、纵向拉伸回弹，并叠加同帧泛白后恢复原色；同时迸出 `Star4` 星屑与 `GlowDot` 光粒，略微下落淡出。
- 收回：“收回玩偶”指令立即交还收集物、放下任务并进入收回状态（`ai[1] = 5`，随弹幕同步），后排先走、每只间隔 6 tick。玩偶先轻微鼓起，再收缩、泛白并上飘；剩 8 tick 时在玩偶位置爆出星芒与上飘星屑，主人端在 26 tick 后移除弹幕。
- 远距离瞬移回主人身边（任意端观察到两帧间位移 > 480 像素）：原地以星屑消散，新位置重新弹出。主人死亡、离线等直接移除时，`OnKill` 补一次消散星屑。晚加入的玩家看到现有玩偶时也会播放一次出现。
- 星屑为独立寿命的 `KLBasicDust`，玩偶移除后继续飘散；在 `Projectiles.After` 同层 Bloom 中以预乘 AlphaBlend + `ReColor` HDR 2.2 绘制（同魔力飞弹本体：写入覆盖度、先压暗背后画面再上饱和粉紫，Bloom 只提取溢出部分；不用 Additive，避免白天亮背景上爆白。贴图是预乘格式，所以不用 NonPremultiplied，否则柔边会在亮背景上发灰），星芒存续期间提交粉紫 `AddLight`。贴图均为已有资源（`GlowDot`、`Star4`、KL 星芒），无运行时生成。

## 验证

```powershell
pwsh -File Tools/DollSummonPreview/run.ps1
pwsh -File Tools/DollControlPreview/run.ps1 -Checks
pwsh -File Tools/DollControlPreview/run.ps1
```

输出在 `.vissandbox/doll-summon-preview/`：`doll-summon-night.gif`、`doll-summon-day.gif`（夜晚深色 / 白天青绿背景，召唤→停留→收回全程）与 `doll-summon-sequence.png`（上两行夜晚、下两行白天）。预览按 tModLoader 方式预乘 PNG alpha，星屑先画进带覆盖度的离屏目标，再按 `场景×(1−a) + rgb×HDR` 合成（截断到 8 位，同游戏场景目标），但不模拟 KL Bloom 模糊，游戏内光晕会更柔；玩家与地面为夹具。

`DollControlPreview` 的桩环境中 `DollSummonDust.Burst` 为空实现。`-Checks` 共 100 项通过，其中收回改为验证：立即交还物品且 `FindOwned` 不再返回收回中的玩偶、后排在 26 tick 先移除、整队在 26 + 3×6 tick 后全部移除、只影响主人自己的队伍。帧表预览通过 `Shown()` 夹具跳过出现动画，以展示稳定姿态。尚未在游戏内验证 Bloom 亮度、多人同步观感与音效（本次未加音效）。
