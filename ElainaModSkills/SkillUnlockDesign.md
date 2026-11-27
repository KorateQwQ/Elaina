# 伊蕾娜技能解锁重设计思路（参考草案）

> 本文档整理了“去纯 Boss 阶段化”的技能解锁改造思路，面向后续设计评审与迭代优化。文中具体数值、材料与触发条件均为可调草案。

---

## 一、现状诊断：为什么当前解锁体验偏平淡

目前技能系统已具备相当完整的星图与发现基础设施：
- `ElainaSkill` 已支持 `IsConstellationRevealed(Player)` 与 `ConstellationRevealHint`，可区分 **`hidden`（未显现）→ `locked`（问号待启封）→ `ready`（待研习）→ `learned`（已习得）** 四种状态（见 `ElainaSkillUI/ConstellationSkillPanel/ConstellationState.cs`）。
- `ConstellationDiscoveryPlayer` + `ConstellationDiscoveryTracker` 已能在技能从未显现变为待启封/待研习时弹出手札横幅提示。
- `SkillBalanceConfig` / `SkillBalanceSystem` 已将技能伤害与角色等级、`bossState` 和技能等级挂钩——**提前解锁技能本身并不会直接破坏输出上限**。

但当前大多数技能的 `UnlockCondition` 仍主要依赖 `RequireConfiguredCharacterLevelCap` / `RequireConfiguredCharacterLevel`（即由击败 Boss 推进 `bossState` 抬高角色等级上限），导致体验变成：
1. 击败任意 Boss 推高阶段 → 等级上限提升 → 星图自动亮出一批技能 → 凑研习点和材料点掉。
2. 探索、采药、炼金、遇险、战斗操作与技能获取之间缺乏因果联系，只有 `RecallSkill`（获得过回忆药水）和 `ShardbindSkill`（冬季夜间事件 + 霜月女王）真正用上了“探索发现”机制。
3. 生活/功能型技能（如 `FlightSkill`、`AlchemySkill`、`DollControlSkill`）也被捆在战斗阶段上，限制了非纯粹推 Boss 玩法的节奏。

---

## 二、核心改造原则

### 1. 把解锁拆成两层：「星辰显现」×「正式研习」
- **星辰显现（`IsConstellationRevealed` + `ConstellationRevealHint`）**：负责“惊喜感”与《魔女之旅》式的旅行见闻。
  - 尽量减少一刀切的 `hidden`，让多数非终极技能在星图上以 `locked`（`???` + 诗意模糊线索）存在。
  - 玩家在世界中的真实经历（天气、群系、遇险、炼金、用旧技能打出的操作或元素交互）永久点亮对应星辰，并触发 `ConstellationDiscoveryToastUI` 提示。
- **正式研习（`UnlockCondition`）**：负责“付出与仪式感”。
  - 用研习点 + 贴合主题的实践媒介（魔导书、炼金产物、群系/事件结晶等）完成学习，而不是单纯卡死在全局角色等级上限。

### 2. 把数值节奏交给「伤害曲线」与「进修节点」，而非「禁止拥有」
- 已有 `SkillBalanceConfig` 的 `GetSkillLevelNodes` 和伤害定价会按技能等级 / 角色等级平滑约束强度。
- 因此完全可以允许玩家**更早拿到功能与机制**（哪怕初始只有 1 级），再随着等级成长和研习点投入逐步升阶，让不同玩家在同一 Boss 进度下也能走出不同的技能优先级路线。

### 3. Boss 门槛只保留给两类情况
- **少数高阶/终极术式**：改为与**特定主题 Boss 或事件**产生剧情/元素共鸣（参考 `ShardbindSkill` 对应霜月女王），而非“任意 Boss 凑等级”。
- **战中顿悟（逆境反馈）**：部分保命、反制或机动技能可在与 Boss 交战受挫（如险些阵亡/承受高伤）时触发顿悟，让“没打过 Boss”也能带回新灵感。

---

## 三、五大解锁方向设计

### 方向 1：旅途见闻与自然奇遇（探索驱动）
利用泰拉瑞亚丰富的环境、天气与高度层，让“旅行”本身成为学习魔法的途径：
- **气候/环境共鸣**：在大风天、沙尘暴、暴风雪、暴雨、高空/太空、地狱等环境下停留或施法。
- **奇遇足迹**：发现漂浮岛、地牢、丛林神庙、微光湖（Shimmer）或首次被特定环境伤害/减益影响。

### 方向 2：危机直觉与生活交互（体验驱动）
- **绝境求生**：在生命垂危、身中持续掉血减益或经历坠落/溺水危机后领悟防护与回复魔法。
- **日常与伴随成长**：骑乘扫帚的累计飞行距离、使用大锅炼金的次数、与城镇 NPC 的共处，直接推进对应生活系技能的显现与成长。

### 方向 3：以旧悟新与元素交互（战斗驱动）
将“前置技能等级 + 操作型微挑战”结合，鼓励玩家真正去用基础技能：
- 用基础弹幕连续命中多目标、在元素领域内触发特定联动（例如火烧冻伤目标、风火交汇、水系持续引导等），从而在战斗中“顿悟”高阶术式。

### 方向 4：打通「炼金系统 × 魔导书 × 技能」（系统闭环）
当前 `ElainaModAlchemy`（大锅、配方手记、药水与奇物）和 `Items/魔法书` 已具备独立生态，可把它们与技能解锁深度耦合：
- **炼金媒介解锁**：部分进阶魔法不靠怪物掉落，而是要求玩家在炼金手记中研究并亲手用大锅熬制对应炼金产物（例如结合 `Items/消耗品/` 中的寒霜/焰火/潋滟/雷引药水，或 `ElainaModAlchemy` 下的主题药剂如 `BottledRain`、`MoonDew` 等）。
- **魔导书“阅览/共鸣”**：拿到 `水魔法书`、`火焰魔法书` 等不再只是背包里凑数消耗的门票，而是**获得书本先揭示残页线索**，随后携带该书在对应环境完成一次微小实践（如在水中施法、在地狱点燃敌人）才正式开放研习。

### 方向 5：分支路线与流派自由度
同一阶段不再所有技能一起开放、一起点完，而是根据玩家去过的群系和投入的资源产生分化：
- 喜欢下矿探图与基建的玩家更早解锁 `FlightSkill`、`DollControlSkill`、`AlchemySkill`。
- 提前闯地牢或雪原的玩家更早摸到水系/冰系衍生技能；深入丛林或地狱的玩家更早拿到治疗与火系分支。

---

## 四、全技能重设计参考方案（按系别梳理）

> 注：下表列出当前已注册的 19 个技能及 `WindTornadoSkill` 等雏形技能的现状与建议方向，作为后续调整基准。

### 1. 基础、生存与生活系
| 技能 | 当前解锁方式 | 建议「显现（旅途见闻）」 | 建议「研习 / 门槛」 | 设计意图 |
| :--- | :--- | :--- | :--- | :--- |
| `AshenWitchSkill`（灰之魔女） | 初始习得 | 默认已显现并习得 | 保持不变 | 角色核心身份特性 |
| `MagicMissileSkill`（魔法飞弹） | 初始习得 | 默认已显现并习得 | 保持不变 | 初始基础术式 |
| `MultiMissileSkill`（多重射击） | `MagicMissileSkill` Lv.5 + 默认研习点 | 使用 `MagicMissileSkill` 累计命中或短时间内命中多个不同目标 | `MagicMissileSkill` Lv.3~5 + 研习点（取消硬性阶段等待） | 鼓励早期多用基础飞弹打出操作 |
| `MagicBarrierSkill`（魔力护盾） | 角色 Lv.5 + 10 研习点 | 首次生命值跌破 35% 并存活脱险，或承受高额单次伤害 | 研习点（可配合少量魔力星/晶体材料） | “遇到危险后学会保护自己” |
| `HealSkill`（治疗魔法） | 角色等级上限 Lv.25 | 携带持续伤害减益（如中毒/流血/着火）状态下恢复满血，或身处受伤 NPC/队友旁 | 研习点 + `治疗魔法书`（或丛林草药媒介） | 让丛林探索与治疗魔法书真正串联 |
| `RecallSkill`（回城魔法） | 获得过 `RecallPotion` 后显现 + 20 点 + 1 回忆药水 | **保留并作为标杆**（已实现） | 保持现有形式，可微调研习点 | 已具备良好“旅途发现”质感 |
| `FlightSkill`（飞行魔法） | 角色等级 Lv.11 + 上限 Lv.15 + 15 点 | 骑乘扫帚累计飞行一定距离，或首次飞抵太空/空岛高度 | 取消 Boss 上限锁；改用研习点（或羽毛/云等轻盈材料） | 扫帚是伊蕾娜核心体验，尽早给机动乐趣 |
| `AlchemySkill`（炼金术） | 角色等级 Lv.6 + 上限 Lv.10 + 10 点 | 首次放置/右键 `炼金大锅` 或采集任意草药 | 取消 Boss 上限锁；极低研习点或直接免费开启手记 | 让玩家开局探索时即可自然进入炼金线 |
| `DollControlSkill`（玩偶操控） | 角色等级 Lv.6 + 上限 Lv.10 + 10 点 | 在地下宝箱/微光湖探索，或在特定夜晚遭遇后发现人偶线索 | 取消固定战斗阶段锁；研习点 + 丝绸/线团类生活材料 | 照顾基建、采集与休闲向玩家 |

### 2. 风系
| 技能 | 当前解锁方式 | 建议「显现（旅途见闻）」 | 建议「研习 / 门槛」 | 设计意图 |
| :--- | :--- | :--- | :--- | :--- |
| `WindSlashSkill`（风刃） | 角色等级上限 Lv.20 | 在大风天（Windy Day）或沙尘暴中停留，或高空迎风飞行一段时间 | 研习点 + `风魔法书`（或羽毛/风系素材） | 贴合“乘风旅行”的直观联想 |
| `WindTornadoSkill`（风之龙卷） | 暂未完整配置门槛（雏形） | 施放 `WindSlashSkill` 期间处于高风速/空中高速移动状态 | 前置 `WindSlashSkill` + 炼金/天空材料（如云/风元素媒介） | 定位为风系中期控场延伸 |

### 3. 水系
| 技能 | 当前解锁方式 | 建议「显现（旅途见闻）」 | 建议「研习 / 门槛」 | 设计意图 |
| :--- | :--- | :--- | :--- | :--- |
| `WaterLaserSKill`（水流术） | 角色等级上限 Lv.30 + `水魔法书` + 25 点 | 在雨天或水中施放基础魔法，或获得 `水魔法书` / 使用 `BottledRain` | `水魔法书` + 研习点（移除纯 Lv.30 阶段锁，改以地牢本身作为自然进度门槛） | 地牢本身已有骨架王前后的自然冒险门槛，不必双重卡死 |
| `WaterBallSkill`（水球术） | 等级上限 Lv.30 + 水流术 Lv.3 + 25 点 | 持有 `WaterLaserSKill` 在暴雨或水下连续引导一定时间 | 前置 `WaterLaserSKill` + 炼金水系药剂（如 `潋滟药水` / `BottledRain`） | 体现对水元素聚形压缩的进阶理解 |

### 4. 冰系
| 技能 | 当前解锁方式 | 建议「显现（旅途见闻）」 | 建议「研习 / 门槛」 | 设计意图 |
| :--- | :--- | :--- | :--- | :--- |
| `IceConeSkill`（冰锥术） | 等级上限 Lv.25 + `FrostCore` | 在暴风雪中探索雪原，或承受过冷冻/冰冻减益 | `FrostCore`（或 `寒霜魔法书` / `寒霜药水`）+ 研习点 | `FrostCore` 本身已是肉后暴风雪产物，无需再叠 Lv.25 上限锁 |
| `FrostVeilSkill`（霜幕） | 前置 `IceConeSkill` + 默认研习点 | 习得 `IceConeSkill` 后在低生命或雪原受击时领悟 | 前置 `IceConeSkill` + 研习点（可搭配少许冰霜素材） | 从“冰攻”延伸到“冰防”的直觉切换 |
| `ShardbindSkill`（冰凌锁） | 冰锥 Lv.13 + 霜月女王 + 冬季夜间事件显现 | **保留现有设计**（冬季夜间事件显现） | 保持现有“霜月女王 + 前置技能”形式 | 作为“特定主题 Boss/事件”终极技标杆 |

### 5. 火系
| 技能 | 当前解锁方式 | 建议「显现（旅途见闻）」 | 建议「研习 / 门槛」 | 设计意图 |
| :--- | :--- | :--- | :--- | :--- |
| `FireBurstSkill`（炎爆） | `火焰魔法书` + 上限 Lv.50 + 任意机械 Boss | 携带 `火焰魔法书` 进入地狱，或自身曾被点燃/经历岩浆危机 | `火焰魔法书` + 研习点（若需控制肉后强度，可用肉山/炼金材料而非机械 Boss 一刀切） | 强化地狱与烈焰魔法书的探索仪式感 |
| `FireLaserSkill`（火焰射线） | `火焰魔法书` + 上限 Lv.50 + 任意机械 Boss | 使用 `FireBurstSkill` 对冻结/霜裂目标打出元素增伤，或在火焰领域内持续作战 | 前置 `FireBurstSkill`（或机械 Boss 后置条件）+ `焰火药水`/魂魄 | 鼓励玩家真正去触发“火焰领域 + 元素反应”机制 |
| `FireTornadoSkill`（火焰龙卷） | 角色 Lv.86 + 上限 Lv.90 | 在火焰领域内同时施放风系法术，或于日食/高阶火焰危机中存活 | 保留少数高阶 Boss 共鸣门槛 + 前置风/火技能 | 顺理成章体现“风助火势”的融合奥义 |

---

## 五、技术落地要点（与现有框架对接）

如果后续开始编码实现，建议遵循以下最小改动路径：

### 1. 新增统一的「见闻记录器」`SkillDiscoveryPlayer`
参考 `Skills/Recall/RecallSkill.Research.cs` 与 `RecallDiscoveryPlayer`，避免为每个技能各建一个 `ModPlayer`：
- 新建 `ElainaModSkills/SkillDiscoveryPlayer.cs`（继承 `ModPlayer`）：
  - 用 `HashSet<string>` 或明确布尔字段记录永久性的“旅途见闻标志”（如 `SurvivedLowHealth`、`VisitedSkyHeight`、`ExperiencedSandstorm`、`CastInRain`、`TouchedCauldron`、`TriggeredFireIceReaction` 等）。
  - 通过 `SaveData` / `LoadData` 随角色持久化存档。
  - 若部分条件在服务器触发（如受击、击败特定 NPC、炼金完成），按需同步给客户端本地玩家，确保 `ConstellationDiscoveryPlayer` 能每 30 帧检测到状态变化并弹出提示横幅。

### 2. 解耦「解锁门槛」与「数值成长起点」
检查 `SkillBalanceConfig.cs` 与 `ConstellationState.cs`：
- 当技能取消 `RequireConfiguredCharacterLevelCap` 后，需确认 `SkillBalanceEntry` 的 `GrowthStartLevel` / `UnlockLevel` / `UnlockStageCap` 仅控制**升级节点分布**，而不会误阻断调试或面板状态判断。
- 特别是 `ConstellationState.cs` 中若仍有直接读取 `entry.UnlockStageCap` / `entry.UnlockLevel` 的逻辑，应统一归口到 `UnlockCondition` 与 `StageGateUnlockCondition.AllReachable` 判定，保持单一真相源。

### 3. 保留可热重载与清晰的状态区分
- 见闻数值阈值（如“生命跌破 35%”、“累计飞行距离”、“连续命中次数”）按照项目约定写成 `=>` 表达式体属性，方便后续调参和 C# 热重载。
- 避免将 `StageGateUnlockCondition` 的 `Reachable` 写得过严，否则星图上会长期显示为不可见的 `hidden`，玩家看不到 `locked（???）` 的探索线索。
  - **推荐做法**：普通探索技能的 `StageGateUnlockCondition.AllReachable` 保持为 `true`（即一进世界就能在星图看到模糊的问号星辰与线索）；只有真正的强阶段终极技（如 `ShardbindSkill`、`FireTornadoSkill`）才在达到对应大阶段前隐藏或锁定。

### 4. 玩家文案与本地化规范
修改 `ConstellationRevealHint` 及新增 `SkillInfo.UnlockConditions.*` 时，需先查阅 `Localization/README.md`：
- `ConstellationRevealHint` 写**方向性探索线索**，不直接剧透底层数值与实现细节（例如写“在狂风呼啸的高处乘风而行时，或许能捕捉到风的形迹”，而不是“在大风天 Y < 200 停留 600 帧”）。
- 正式解锁需求（`UnlockCondition.GetDescription`）保持简洁明确，直接告诉玩家缺什么媒介或前置即可。

---

## 六、建议的渐进实施顺序

1. **第一批（最见效、零数值风险）**：解放生活与移动类技能（`FlightSkill`、`AlchemySkill`、`DollControlSkill`、`MagicBarrierSkill`），用旅途见闻替代硬性等级上限。
2. **第二批（打通魔导书与环境体验）**：改造基础元素技能（`WindSlashSkill`、`WaterLaserSKill`、`IceConeSkill`、`HealSkill`、`MultiMissileSkill`），让环境探索、魔导书与老技能实践成为核心解锁驱动。
3. **第三批（炼金耦合与高阶奥义）**：让 `ElainaModAlchemy` 产物和元素反应接入进阶技能（`WaterBallSkill`、`FireBurstSkill`、`FireLaserSkill`、`FireTornadoSkill`、`WindTornadoSkill`），最后统一校准星图文案与 `ConstellationDiscoveryToastUI` 的发现节奏。
