# 扫帚穿平台回归检查

在仓库根目录执行 `./Tools/BroomCollisionChecks/run.ps1`。

脚本编译正式模组源码（不打包、不重新加载游戏），随后加载编译产物中的
`BroomPlatformCollision` 和 `扫帚ModPlayer`，直接注册正式碰撞钩子，使用本机
tModLoader 的真实 `Player`、`Tilemap` 和碰撞函数验证行为，不替换引擎实现。
临时工程、编译日志位于 `.vissandbox/broom-collision-checks`。

原版 `fallThrough` 在下降速度大于 1 时仍会阻挡平台，除非同时启用
`ignorePlats`；原版在玩家更新中只为指定坐骑类型设置后者。
修复仅对本模组扫帚有动力飞行且按住下键的玩家启用这两个参数。

2026-10-24：42 项检查通过，包括未修复时复现卡平台，空气、水、蜂蜜和微光
四条碰撞路径，下降速度 1 / 1.25 / 10 / 20 / 30，随后进行斜坡碰撞，松开下键、
无动力、下坐骑、其他坐骑、实体地面阻挡，以及卸载钩子恢复原行为。
尚未进行游戏内操作和多人联机验证。

## 2026-10-25：飞行魔法与扫帚重写

- 速度取 `ElainaSkillModPlayer` 中实际习得的 `FlightSkill.Level`，不再引用旧 `EXPmodplayer` 或熟练度。
- 正式参数为 `ElainaModSkills/Data/SkillBalance.json` 的 `configuration.skills[id=FlightSkill].flight`。普通速度 8；Lv1 +10%，每级再 +5%；技能节点为 11、21、31、41、51、61、71、81、91，最高 Lv9，月后正常速度 12。
- 默认 Space：按住 0.25 秒后加速，最高 1.5 倍，按有效加速帧支付 12 魔力/秒；短按松开冲刺，速度 2.5 倍，持续 0.2 秒，一次 25 魔力，冷却 1 秒（从启动开始）。方向优先取方向键，其次当前速度，最后面朝方向。
- 普通飞行和悬停免费。支付使用新版独特魔力，不足时停止加速，不进行生命转换。一次长按耗尽后须松开再按，不会每帧反复尝试。冲刺不提供无敌和穿墙。
- 方向键统一为二维加速度，斜向归一化；沿用原版实体碰撞与已有平台下穿钩子。个人速度加成不修改共享 MountData。
- 冷却使用游戏更新时钟，与绘制频率、角色等级、上下坐骑无关。取消输入保留冷却；暂停/失焦由 UpdateUI 清理待释放手势。
- 原正式配置已超过 32 KiB，游戏/网页/本机服务上限同步调整至 64 KiB；网页保存会保留 flight 对象，三处都验证参数范围。

验证命令：

```powershell
pwsh -File Tools/BroomCollisionChecks/run.ps1
pwsh -File Tools/AlchemyItemChecks/run-integration.ps1
node Tools/BroomCollisionChecks/check-editor.cjs
node Tools/MageBalance/test-runtime.cjs
python -m unittest discover -s Tools/MageBalance -p test_server.py
pwsh -File Tools/LocalizationChecks/parse.ps1
```

结果：51 项配置与手势状态检查、42 项真实碰撞检查、22 项真实玩家移动/按键/扣蓝检查通过；药水/食物/炼金既有集成回归通过，36 项 C# 配置运行时、17 项服务端配置、8 项网页 flight 保存校验及 12 个 HJSON 文件解析通过。

`Tools/MageBalance/test.cjs` 的全量检查在已有的“Published game loadouts must match browser defaults”断言失败：当前用户配装配置与网页默认配装不同。本次未覆盖用户配装。以上为离线代码与引擎验证，尚未进行游戏内手感及双客户端联机验证。

### 操控性改为百分比

正式 JSON 的 `flight.handlingPercent` 默认 **25**（范围 1–100），表示每帧消除当前速度与目标速度之间相应百分比的差值。起步、转向、松键刹车统一使用该值；技能等级、轻羽药水和快乐 Buff 只影响目标速度，不改变响应百分比。旧 `acceleration` / `braking` 参数已停用。越接近 100 越直接，100 为立即达到目标速度。剩余速度差小于当前速度上限的 1% 时归位，保证松键后精确悬停。

默认 25% 时，Lv1 / Lv9、有无轻羽药水均在 17 帧内完成起步或刹停，全速反向 3 帧内改变速度方向，19 帧内完成反向；直角转向 18 帧内消除侧向漂移。以上包含原版 HorizontalMovement 的真实玩家集成检查，未代替游戏内手感试用。

此次回归：50 项玩家移动/按键/魔力集成检查、53 项配置/输入状态检查、42 项碰撞检查、10 项网页配置检查、17 项服务配置测试通过，药水/食物/炼金集成回归通过。

### 飞行魔法解锁门槛修复

加速和冲刺现在必须同时满足：当前骑乘者在新版已习得技能集合中拥有 FlightSkill、状态为 UnLock、实际等级大于零。未学习者保留速度 8 的免费基础飞行；按键不产生加速/冲刺，也不扣魔力。技能被移除或重置时立即取消当前动作，保持冷却；重新学习不会接续旧的长按或松键手势。禁用期间已松键的情况下，学习后的首次新按键正常生效。

真实玩家移动集成检查扩展到 72 项，覆盖缺少技能、残留锁定条目、零等级、Lv1 解锁、动作途中重置以及按住期间重新学习。速度成长和固定操控百分比保持原规则。

### 伊蕾娜专属、延长冲刺与双气流环

- BroomAccessSystem 的 CanMount / SetMount 钩子、物品 CanUseItem、坐骑 Buff 与 UpdateEffects 共用当前玩家的伊蕾娜身份。普通玩家不能启动扫帚，角色改变时强制下坐骑；不依赖 Main.LocalPlayer 判断别人。
- `flight.dashSeconds` 默认从 0.2 改为 0.4（24 帧）；新增 `flight.dashRecoverySeconds: 0.15`（9 帧），冲刺自然结束保留短暂余速并逐渐收回。仍然只支付一次 25 魔力，冷却从启动计 1 秒。Lv9 在无碰撞情况下，匀速冲刺段长度由 360 像素变为 720 像素。失焦、未习得和角色切换等取消路径立即结束恢复状态。
- 成功冲刺产生一次双气流环，正式绘制与可复现预览见 `Tools/BroomDashPreview/README.md`。最终使用 NonPremultiplied 和离线转换后的直通 Alpha PNG，没有加色光环。

本轮验证：8 项真实原版坐骑权限检查、99 项移动/按键/扣蓝集成检查、67 项配置与状态检查、42 项原版碰撞检查、12 项网页配置检查和 17 项配置服务测试通过；药水/食物/炼金集成及 HJSON 解析通过。Debug / Release 编译通过，游戏内手感与双客户端联机待实测。
