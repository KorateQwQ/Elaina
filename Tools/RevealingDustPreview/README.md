# 显迹尘：行为与 FNA 离线预览

正式代码位于 `ElainaModAlchemy/Revealing/`，物品入口为 `item/Curios/RevealingDust.cs`。

## 水矢标记（2026-10-30）

客户端在每秒整区扫描危险物块的同一轮中，记录区域内的水矢书本（`TileID.Books` 且 `TileFrameX == 90`，破坏后掉落 `ItemID.WaterBolt`）。重叠区域合并去重；绘制与光照前再次检查物块，书被取走立即停止标记。标记对所有能看到该区域的客户端显示，与危险物块高亮一致，不需要服务器同步。

`PostDrawTiles` 用正式 `DrawWaterBolt` 绘制水蓝柔光与上浮星点（复用 `MarkGlow`、`GlowDot`、`Star4`），`PostUpdateEverything` 每帧提交 `(0.12, 0.42, 0.85)` 的蓝色光照。`run.ps1` 额外输出 `waterbolt-000.png` 至 `waterbolt-035.png` 与 `water-bolts.gif`：上排暗底、下排亮底，依次为原尺寸书架、2 倍书架、配方宝箱金光对照。书架为原版 `Tiles_50` 贴图拼出的夹具，不是完整地牢场景，游戏内光照传播仍需实机确认。`check-lighting.ps1` 新增 4 项水矢检查（只标记区域内样式 5、蓝光每帧一次、取走即停、末个区域消失即停），共 59 项。

## 配方宝箱金光（2026-10-30）

`RevealingRecipeChests` 在单机/服务器每秒扫描使用者自己的显迹区域，区域改变时提前扫描。已存有任意炼金配方卷轴的宝箱，以及按该角色当前保底进度开箱会产出配方的天然候选箱，发送给区域拥有者显示。候选箱的随机值在首次探测或开箱时确定并保存；探测不修改库存、不结算保底，开箱沿用该随机值。因此角色保底进度或获得记录变化、其他人先开箱后，下一次扫描会更新结果。

金光使用正式 `DrawRecipeChest` 与已有 `MarkGlow`、`GlowDot`、`Star4` 预制资源，在 `PostDrawTiles` 绘制金色柔光和交错上浮星芒；不生成运行时纹理。客户端只接收自己的坐标快照，并再次检查自己的存活区域；区域结束、角色死亡或箱子消失会停止绘制，断更快照在 180 tick 后失效。配方取走或新存入在下一次扫描更新。

运行 `pwsh -File Tools/RevealingDustPreview/run.ps1` 生成 `recipe-chests.gif` 与 `chests-000.png` 至 `chests-035.png`。预览上排暗底、下排亮底；四列依次为未标记金箱、标记金箱、标记常春藤箱、2 倍标记金箱。已检查多个阶段及中英文手记默认/125% 缩放。宝箱逻辑与私人同步检查运行 `pwsh -File Tools/AlchemyChestChecks/run.ps1`；原有 55 项危险物块、敌人及光照回归运行 `check-lighting.ps1`。离线夹具不替代实际游戏中的双客户端、延迟和完整地形遮挡验收。

## 目标实际光照（2026-10-25）

标记现在会在 `PostUpdateEverything` 每帧向 `Lighting.AddLight` 提交暖金色光照，RGB 强度为 `(0.65, 0.38, 0.10)`：危险物块按去重后的缓存提交，NPC 按当前中心提交。原有危险感高亮、金色粉尘与柔光继续保留。下文旧记录中“不向地形写入光源”仅描述此次修改前的行为。

物块被破坏或换成不同类型时立即停止提交，最后一个覆盖区域消失后清除光源；NPC 离区后仍随标记持续发光，标记到期停止。独立服务器和主菜单不提交光照。光源提交不依赖每秒扫描，避免扫描间隔中闪断。

独立检查命令：`pwsh -File Tools/RevealingDustPreview/check-lighting.ps1`。50 项引擎检查通过，其中 9 项新增检查截获实际 `Lighting.AddLight` 调用，覆盖位置、颜色、重复区域、逐帧提交、物块移除/替换、NPC 移动/到期及服务器/菜单。此入口只加载显迹尘代码，避免全量炼金工具受其他正在开发的 UI/玩家依赖影响。

源码 Compile 通过；重新生成并打开 `mark-comparison.png`，确认原有金色柔光保留。该 FNA 预览没有模拟世界光照传播，新光源的最终照明范围、不同光照模式和多人画面仍需重建模组后在游戏内验证。

出手即消耗一份，从人物朝投掷方向 24 像素的位置洒开粉尘；此位置留下半径 1000 像素、持续 18000 tick（5 分钟）的固定区域。没有额外的飞行或落地等待。首次更新立即扫描，之后每 60 tick 整区扫描一次；玩家走动不会移动区域。

客户端在每次整区扫描中调用真实 `TileDrawing.IsTileDangerous`（包含模组钩子与本地玩家装备条件），缓存危险物块；普通物块和动态绘制物块均应用原版危险感的红色亮度下限。扫描时按物块中心判断圆形范围，穿墙、不做视线遮挡检测。重复区域共用高亮，移除最后一个覆盖区域时立即取消。破坏物块立即失去标记，新增/变化的危险物块在下一秒扫描时更新。不检测飞行弹幕或真实液体，不向地形写入光源。

单机/服务器统一遍历活跃敌对 NPC，找到本轮任一区域覆盖其中心即刷新一次。排除友方、城镇 NPC、小动物和训练假人；敌人的暂时无敌阶段仍可沾染。标记独立于 Buff 槽位与免疫，从最后一次检测到沾染开始保留 18000 tick，刷新不累加，离区和区域到期均不清除。复用 NPC 槽位时重置。客户端使用该 NPC 的猎人显形判定与绘制颜色，不给玩家全局加 Buff；具有完全自定义绘制的其他模组 NPC 仍需游戏内兼容验证。

区域用 `netImportant` 弹幕及年龄同步，晚加入按剩余时间继续。NPC 标记在初次、离区后以及停留中每 15 秒校准；额外 AI 同步剩余 tick，不每秒广播完整 NPC 包。独立服务器只处理 NPC，不扫描地形绘制缓存。区域及标记不跨退出世界保存。

粉尘为 360 个确定性金色粒子，105 tick 内向外散开并淡出；正式绘制与预览链接同一 `RevealingDustDrawing.cs`，使用 `MagicPixel` 和标准 SpriteBatch，无运行时纹理生成、Shader 或真实照明。预览地形/人物是夹具，尖刺与史莱姆使用原版 XNB；高亮夹具不能代替实际世界绘制检查。

```powershell
pwsh -File Tools/AlchemyItemChecks/run.ps1 -RevealingOnly
pwsh -File Tools/AlchemyItemChecks/run.ps1
pwsh -File Tools/RevealingDustPreview/run.ps1
pwsh -File Tools/AlchemyPreview/run.ps1 -Scene curio-trace
pwsh -File Tools/AlchemyPreview/run.ps1 -Scene curio-trace -Culture en-US
dotnet msbuild 伊蕾娜.csproj -t:Compile -p:BuildProjectReferences=false -v:quiet -clp:ErrorsOnly
```

输出在 `.vissandbox/revealing-dust-preview/`：`revealing-dust.gif` 为 60 帧动画，`dust-sequence.png` 为 2/12/32/65/95/120 tick 帧条，另有 `dust-bright.png`、`dust-125.png` 和 `dust-range.png`。手记输出在 `.vissandbox/alchemy-preview/output/<语言>/curio-trace-*.png`。

2026-10-24：31 项真实引擎专项检查通过，包括立即/每秒扫描、边界、重叠去重、进出区域、过期、复用、网络剩余时间、服务器分工，以及实际猎人/普通和动画物块绘制钩子。全量炼金检查通过 482 项物品与本地化、67 项药剂、128 项配方、113 项微光、40 项瓶中雨检查。修正显迹尘目录尺寸为实际 PNG 的 32×32，原有全量图像检查阻塞已解除。正式 64 张轮廓资源检查通过；已打开金色粉尘多阶段、明暗底、缩放和完整范围截图，以及中英文手记默认/125% 的相关截图。

性能夹具用 400×400 实际 Tilemap，密铺石块，每七格一处尖刺，所有区域同帧刷新；1/5/10/20 个区域各记录 60 次扫描。不涉及完整帧、其他模组、网络或 GPU，不能等同于实际 FPS。线性坐标索引替代密集网格上易碰撞的 FNA Point 哈希后，两次测量 20 区域扫描中位分别约 2.63/3.27 ms、p95 约 4.75/4.08 ms；第二次同时运行了构建和预览，结果仅作量级参考。通常不同时间投掷自然错开刷新；同帧投掷仍严格立即检测。

源码编译通过。完整打包尝试被 TML003 阻止：运行中的 tModLoader 正占用模组，本轮没有生成新 `.tmod`。尚未验证真实游戏投掷手感、完整画面遮挡及多人端到端同步；需游戏释放模组后重建并验收。

## 暗色敌人与神庙喷火陷阱修复（2026-10-24）

用户实测发现洞穴蝙蝠只露眼睛、炸药和神庙喷火陷阱缺少高亮。两者原因不同：蝙蝠的蓝黑贴图乘红色最低亮度后仍很暗；普通物块的旧 `GlobalTile.DrawEffects` 介入时，原版已经计算了 `finalColor` 与全黑跳过标志，修改 `tileLight` 并不能可靠改变最终绘制。上一轮只检查被修改的字段，未检查实际引擎最终颜色，因此 31 项检查不足以证明普通物块显示正确。

已移除过晚的普通物块钩子，改在 `TileDrawing.DrawSingleTile` 首次采样光照后注入区域高亮，位置早于最终颜色和可见性计算。动态物块入口仍保留。现在 `-RevealingOnly` 通过 41 项检查，新增爆炸物、引爆器、神庙喷火陷阱（Traps 的 frameY=36）及洞穴蝙蝠。回归直接调用已安装引擎的 `DrawSingleTile`，用黑色环境采样、空纹理查询替代 GPU 依赖，在实际 `finalColor` 计算完成后截取并返回；验证三种物块在显迹尘下的最终颜色和开启危险感药水时完全相同。这个测试不模拟 GPU 绘制或其他模组的所有钩子。

新增 `Assets/MarkGlow.png`：64×64 RGBA8，基础纹理显存 16 KiB，由 `bake-glow.py` 生成；RGB 存径向衰减，非零像素 alpha=255，绘制 tint alpha=0，在现有 AlphaBlend 下叠加金色柔光。不是染色敌人贴图，也不添加环境光。每个已标记、在视野附近的敌人绘制柔光和小亮芯两次；统一在 `PostDrawTiles` 渲染，避免正常在地形后绘制的敌人把提示也遮住。重叠区域不增加光斑份数，离区后跟随标记保留，标记到期就消失。没有每帧纹理生成或 RT 切换。

FNA `mark-comparison-labeled.png` 左列为旧效果，右列为修复；上行为黑底，下行为亮底。实际使用原版 CaveBat 的 44×160 五帧贴图、Explosives 和 Traps 图集，以及共享的正式 `DrawMark`。蝙蝠同时检查原尺寸和 2 倍尺寸；已打开本次对比截图确认柔光与陷阱高亮。截图中的物块颜色是根据已验证颜色构造的展示，不是完整游戏场景。整区 `AddLight` 仍未接入。本轮源码编译与正式 64 张炼金轮廓资源检查通过，尚需重建并加载 `.tmod` 后游戏内复验喷火陷阱、黑暗蝙蝠与联机。

## 圆心星光、粉尘重做与粉尘光照（2026-10-25）

圆心在 `RevealingDustArea.AI` 每帧 `Lighting.AddLight`，颜色 `CenterLightColor = (1.05, 0.78, 0.36)` 乘 `CenterStrength`：出手 8 tick 内亮起并带一次约 28 tick 衰减的闪亮，稳定期 ±7% 呼吸，到期前 180 tick 线性淡出。飞散期间（`BurstTicks` = 180）每 `LightStride` = 6 颗粉尘取一颗，按 `Mote` 算出的实际位置提交 `MoteLightColor = (0.55, 0.34, 0.09)` × 透明度 × 尺寸，照明随粉尘向外扫开后消失。独立服务器不提交。

绘制全部移到 `RevealingDustSystem.PostDrawTiles`，与敌人标记共用一个 LinearClamp 批次，弹幕 `PreDraw` 不再绘制。圆心星光仿照魔杖星光（`ElainaTrailVisualStyle.DrawStar`）：KL `T_StartFlare001` 四芒星 + 两条正交 `ShotLine`，加 45° 的 `Star4` 副星、`MarkGlow` 柔光底和 `GlowDot` 亮芯；出手 40 tick 内有一次放大后收束的大星芒，之后呼吸闪烁，周围 12 颗 `GlowDot` 光粒循环上浮，每 4 颗一颗 `Star4` 闪粒。粉尘为 300 颗 `GlowDot` 柔光粒（光晕 + 亮芯），每 4 颗一颗旋转、尖锐闪烁的 `Star4`；飞散减速后缓慢下沉，在 105–180 tick 内逐颗淡出。tint alpha=0 在 AlphaBlend 中叠加发光，不生成纹理、不切换混合状态。

预览按 tModLoader 的方式对 PNG 预乘 alpha 后加载。`check-lighting.ps1` 共 55 项通过，新增圆心位置与暖色、粉尘沿飞散路径照明、飞散结束后只剩圆心光、结束前淡出、服务器不提光。FNA 输出 `dust-*.png`、`center-00300.png`（稳定）、`center-17900.png`（淡出）、`center-bright.png`、`center-wick.gif` 与 `revealing-dust.gif`；预览不模拟世界光照传播，实际照明范围需游戏内验证。
