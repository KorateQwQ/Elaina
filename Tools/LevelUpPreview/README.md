# 角色升级特效与提示

## 正式代码

- `System/Experience/LevelUpVisuals.cs`：特效时间轴、粒子生成与模拟、绘制（游戏与 FNA 预览共用，不依赖 Terraria）。
- `System/Experience/LevelUpEffect.cs`：升级者本端生成的弹幕，随原版同步给其他玩家；跟随主人脚底绘制星环与星芒，按 tick 放出粒子，提交光照。
- `System/Experience/LevelUpDust.cs`：粒子的 `KLBasicDust` 托管，寿命独立于弹幕。
- `ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/LevelUpToast.cs`、`LevelUpToastUI.cs`：头顶上方的升级提示。
- 触发点：`ElainaStatePlayer.Celebrate`，在经验升级、Boss 提高上限后消耗余经验、调试升级/升满时调用；仅本地玩家、游戏内。

## 表现（2026-10-28 定稿，HTML 预览 A 方案）

- 特效 90 tick：脚下 `ThinRing` 压扁成星环展开，8 个绕行光点（隔一个叠 `Star4` 闪烁）；一道光环自脚底升到头顶上方 62 像素；第 37 tick 起头顶亮起魔杖同款星芒（KL `T_StartFlare001` + 两条 `ShotLine` + 斜向 `Star4` + 亮芯）。
- 粒子：14 条自星环边缘上冲的 `ShotLine` 细光线，24 颗显迹尘同款 `GlowDot` 柔光点（每三颗一颗 `Star4` 闪粒）上浮飘散，星芒亮起时甩出 10 道短射线。暖金为主、少量淡紫。
- 绘制同玩偶星屑：`Projectiles.After` 同层 Bloom，预乘 AlphaBlend + `ReColor` HDR 2.2；`MarkGlow` 大柔光覆盖度为 0（纯叠加），避免白天亮背景出现灰色暗圈。贴图全部是预制 PNG，新增的 `ThinRing.png` 由 `Tools/FixedTextureBake` 烘焙并登记清单。
- 提示：与显现提示同款卡片，1.35 倍显示在屏幕 28% 高度处（人物在 50%），0.22 秒回弹弹入、停留 1.4 秒、0.45 秒原地上浮淡出，不接收鼠标，不进入技能栏旁的停靠位。内容为「旅程等级提升 · Lv.4 → Lv.5 · 获得研习点 +N」，未发放研习点时只显示等级变化。停留中再次升级合并为一条并重新计时；淡入淡出中按当前透明度续接。显现/启封提示保持原有行为，在技能栏右上方独立出现。

## 验证

```powershell
pwsh -File Tools/LevelUpPreview/run.ps1
pwsh -File Tools/ConstellationSkillPanelPreview/run.ps1 -Sources <tModLoader 源码目录> -DiscoveryOnly
pwsh -File Tools/ExperiencePreview/run.ps1 -Checks
```

- `run.ps1` 链接正式 `LevelUpVisuals` 与正式贴图，输出 `.vissandbox/level-up-preview/level-up-night.gif`、`level-up-day.gif`、`level-up-sequence.png`。预览按 tModLoader 方式预乘 PNG alpha，按 `场景×(1−a) + rgb×HDR` 合成，不模拟 KL Bloom 模糊；人物与地面为夹具，人物静止。
- `-DiscoveryOnly` 运行提示队列检查（弹入、停留、合并、淡出续接、约 2 秒后消失），并输出 `level-*.png`（100%/125% UI 缩放，弹入/停留/淡出三帧）。
- `-Checks` 中的升级反馈断言：无升级不提示；一次多级只放一次特效、一条提示且研习点与实际发放一致；满级存经验不提示；Boss 提高上限后消耗余经验时提示一次。
- `index.html` 是定稿前的 HTML 方案对比（`tex/` 为正式贴图副本，仅供该页面使用），不代表游戏内最终像素。

尚未在游戏内验证：Bloom 亮度、移动中跟随效果、多人同步观感、提示与其他 UI 的层级；本次未加音效。
