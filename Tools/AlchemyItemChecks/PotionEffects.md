# 魔药效果与原版接入记录

2026-10-15，以当前中文炼金手册为准。净土露滴按要求暂缓。

| 魔药 | 实际规则 | 持续时间 |
| --- | --- | --- |
| 月露合剂 | 伊蕾娜脱战时存入最多 5 格储备；每格恢复当前独特魔力上限的 50%；收藏后自动补槽 | 储备存档 |
| 止痛药 | 通过原版 lifeRegen 每秒恢复最大生命 1%，保留小数恢复量 | 60 秒 |
| 嗜血药水 | 近战伤害 +10%、近战伤害的 5% 吸血、最终承伤 +20%；与吸血飞刀共用 lifeSteal 额度，服从月噬和不可吸血目标限制 | 5 分钟 |
| 星力药水 | 最大魔力 +40；当前原版魔力高于当前生命的差值每点提供 0.1% 魔法伤害，300 点达到 +30% | 5 分钟 |
| 集中药水 | 初始发射方向与命中时目标中心方向夹角：0 度 +30%、15 度 +17.5%、30 度 +5%，超过 30 度不触发 | 5 分钟 |
| 共鸣药水 | 鞭击标记目标 3 秒，重复命中刷新；自己的召唤物、哨兵及其攻击弹丸对该目标伤害 +20%，鞭子本身不享受 | 5 分钟 |
| 轻羽药水 | 地面移动、跑鞋、翅膀水平/垂直飞行与模组扫帚速度 +20% | 5 分钟 |

除止痛药外，增益药剂保留既有 `AlchemyPotion.DurationTicks` 的 5 分钟默认值。月露沿用独特魔力储备系统。其他六种使用 `Item.buffType` / `Item.buffTime`，原版负责饮用、扣物品、快捷增益、单槽续时及 Buff 存取。游戏自动生成持续时间提示，因此止痛药文本不重复写时长。

## 查阅的本机原版/tModLoader 源码

源目录：`D:/泰拉原版贴图与代码与其他工具/tModLoader/Terraria`。实际编译与离线执行使用 `D:/Steam/steamapps/common/tModLoader` 中当前安装的程序集。

- `Item.cs` 的 `case 2349`：愤怒药水使用 17 帧饮用、Item3 音效和 buffType/buffTime；回忆药水 `case 2350` 是有独立传送使用逻辑的瞬时消耗品，不能仅复制 ID 就获得自定义效果。
- `Player.cs` 的 `QuickBuff`、`AddBuff`、`UpdateBuffs`：消费、跳过已有 Buff、续时与逐帧调用 ModBuff。
- `Player.cs` 的 `WingAirLogicTweaks` 和移动阶段：翅膀/悬停、地形、坐骑计算结束后，`PlayerLoader.PostUpdateRunSpeeds` 紧接 `HorizontalMovement` 之前执行。
- `ModLoader/ModPlayer.cs` 的 `PostUpdateRunSpeeds`：用于修改 maxRunSpeed、accRunSpeed、runAcceleration 等移动参数。轻羽同时缩放步行和跑鞋限速以及加/减速度，不逐帧乘 velocity；水平飞行共享此路径，不在另一钩子重复增加。
- `Player.cs` 的 `WingMovement`、`ModLoader/GlobalItem.cs` 的 `VerticalWingSpeeds`：上升限速和加速度通过参数接入，保留原版重力方向和飞行时间消耗。
- `WingMovement` 喷气背包 `wingsLogic == 4 && TryingToHoverUp` 分支绕过普通翅膀参数。仅在这次原版方法调用期间缩放 `Player.jumpSpeed` 限速基准，用 `finally` 恢复；不额外修改地面跳跃，不修改已有 velocity。其他翅膀仍走专用参数钩子。
- `ModLoader/ModMount.cs`：ModMount 只有一个共享实例。扫帚的飞行速度、加速度和冲刺解限计时现在存于骑乘者的 ModPlayer，通过移动钩子应用，不能写回共享 MountData。
- `Projectile.cs` 的 `vampireHeal`：吸血消耗玩家 lifeSteal 预算。嗜血按自身 5% 计算治疗，用 Player.Heal 处理本地生命/治疗提示。
- `ModLoader/ModPlayer.cs` 的 `ModifyHitNPCWithProj` / `OnHitNPCWithProj`：玩家弹丸命中在攻击者客户端处理。共鸣的归属计时在同一端结算；不共享给其他玩家、不广播每帧计时。

## 判定细节和边界

集中保存弹丸发射位置和方向，通过 ExtraAI 同步；子弹丸继承父弹丸的初始射线。追踪、反弹、命中时零速度或命中后移动鼠标不会重置射线。没有有效初始方向的静止伤害区域不凭空获得满额奖励。

共鸣按 NPC 和玩家分别保存到期时间，多个敌人可同时有标记；NPC 重新初始化后清空。只有当前仍拥有共鸣 Buff 的标记者可以受益。沿用原版召唤物/哨兵/MinionShot/SentryShot 分类，不把鞭子当作召唤物攻击。

轻羽不提高矿车或其他坐骑的速度，不增加翅膀飞行时间。自行实现移动并绕过原版翅膀钩子的其他模组装备需要另行适配。

## 验证与游戏内复核

命令与离线覆盖见本目录 README。完整模组 DLL 编译成功；直接打包时运行中的 tModLoader 返回 TML003，故未替换运行中的 `.tmod`。需要通过游戏的构建/重载流程后才会在当前游戏生效。

游戏内建议复核：手动喝药和快捷增益是否各扣一瓶；重复饮用只续时；取消 Buff/死亡/重进后状态；跑鞋、普通翅膀、喷气背包、悬停翅膀和扫帚的实际碰撞与转向；两名玩家各自鞭击、召唤物命中和治疗同步。离线实体检查不等同于这些端到端结果。
