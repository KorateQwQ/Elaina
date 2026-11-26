# 冰锥碎屑预览与正式接入检查

运行 `& ./Tools/IceShardPreview/run.ps1`，输出在 `.vissandbox/ice-shard-preview/out-stones-v2`。
需要本机 tModLoader/FNA、.NET 10、Python/Pillow；可通过参数替换 TmlPath 与 Dotnet。

`single.gif` 是正常速度单发；`continuous.gif` 是每秒 5 发的叠加检查；`dark.gif` 检查暗背景。
左侧世界像素 1:1，右侧相同种子和时间放大 2 倍。`detail.png` 和 `stages.png` 是静态检查图。

不带参数时保留第二版独立美术原型，仅作历史参考；当前正式版本已按用户要求改为白色贴图粒子。
来袭冰锥是简化的切面形状，用于说明断裂时机，不是正式 IceCone2 模型。
按用户参考图修订为小冰石块：5 块 7–11 像素的主块、4 块 3–5 像素的次级块、18 个细小光点。
不规则五边形截面有厚度，短断口、青蓝暗面及浅蓝亮面；取消碎片长尖与亮棱。
冰块保留惯性、轻微下落并翻滚，后半段淡出；冷雾和闪光分别在约 0.3s / 0.085s 内结束。
每次命中从同一点发散，随机种子固定，Draw 只采样时间不改变状态。

`IceShardScene.cs.txt` 是原型实际绘制代码；Pillow 只添加说明及合成动图，不重绘特效。
Tools 内源码使用 `.cs.txt`，临时项目在已忽略的 `.vissandbox` 中，均不参与模组打包。
原型中的白像素和柔雾贴图允许动态生成；当前正式实现加载已预制的 ShardAtlas.png。
这里没有模拟游戏碰撞、NPC 遮挡、完整 Bloom、环境光照和联机；未进行游戏内验证。

2026-11-10：独立预览工程编译通过，D3D11/FNA 导出 216 帧；已查看原尺寸/2 倍细节、
0.07s/0.20s/0.43s 阶段、连续命中和暗背景代表帧。GIF 采用固定调色板，避免背景逐帧量化闪动。
复现命令仅构建该独立工程；未运行完整模组构建。

同日第二版按用户截图改成小冰石块；独立工程重新编译并导出 216 帧，已查看新版
detail.png、暗背景 0.20s 与连续命中代表帧。第一版输出仍留在 out，第二版独立保存，便于对比。

## 正式冰锥与命中粒子的组合预览

运行 `& ./Tools/IceShardPreview/run.ps1 -Combined`，输出为 `.vissandbox/ice-shard-preview/out-sprites-white`。
接入前的第三版已确认结果保留在 `out-combined-v3`，当前命令不会覆盖它。
`single.gif` 为原尺寸正常速度，`continuous.gif` 为每秒 5 发，`dark.gif` 为暗底，
`slow.gif` 为约三倍时长的慢放；`stages.png` 是命中前后原生帧的 2 倍最近邻裁切。

每次执行用 Roslyn 从当前源码提取 IceConeProj 的 DrawVisual、DrawTrail、UpdateMovement、
DrawReleasedTrail、DrawBreakConnection 和 OnKill；复用 IceDepthPreview 的源码抽取方法，
另外提取当前 KL TrailEffect 的逐点宽度重载，以兼容正式拖尾代码。
使用正式 IceCone2.obj、IceConeProj2.png、IceCone3D.xnb、拖尾 PNG 和 VertexDraw.xnb。
Bloom 和分层请求使用当前 KL 正式实现，预览强度 1、迭代 5。

预览抽取 OnKill 时只移除连接管理器调用和基类回调，并将新版碎屑随机种子固定用于复现。
旧 IceStone 已在正式 IceConeProj 中替换成 IceShardDust，保留 BurstPoint、BurstDust、
SmokeDust2、GlowDust 的生成参数；四类粒子的 PreDraw/Update 及 SpawnDustsCircle 直接读取正式源码。
Dust 槽位、随机源、基础生命周期和资源服务是独立适配：模拟正常 60 tick/s，
包含生成时立即更新一次，贴图按原图集帧顺序采样、A=0 的像素清零。
新版小冰块直接编译正式 IceShardVisuals.cs 与 IceShardDust.cs，使用正式 Emit、Update 和绘制回调；
普通 Dust.Before 队列将它画在冰锥 Bloom 之后、原有命中粒子之前。第二版临时爆点与冷雾不参与正式组合。

场景固定向右发射，速度输入 35，运动使用实际 UpdateMovement；第 31 tick 模拟碰撞，
命中中心设在 (620,247)。拖尾路径是直线采样适配，死亡连接段也是直线；
不模拟 NPC、玩家持杖动作、索敌、真实碰撞、发射气流环、环境光或联机。
这次用户所说“武器”按当前冰锥攻击本体处理。

2026-11-10：组合宿主及提取的正式绘制方法编译通过，FNA/D3D11 完成 240 帧导出并正常退出。
已查看飞行、命中瞬间、闪光消退、连续命中和暗背景代表帧；原有爆点/光晕会在最初几帧遮住小冰块。
上述记录为接入前组合预览，接入后的验证见下节。

## 早期几何版接入记录（2026-11-10，已被贴图版替代）

- IceConeProj.OnKill 调用 IceShardDust.Emit；其余命中粒子参数保持不变。
- 一颗 Dust 承载 5 主块、4 小块和 18 亮点，使用游戏更新 tick，最长 51 tick；不在 Draw 推进状态。
- 冰块仍是动态翻滚和照明的几何体，复用顶点/面缓存；柔光只加载正式 96×96 PNG。
- ShardGlow 生成函数在 `Tools/FixedTextureBake/Bake.cs.txt`，资源与哈希登记于 `Effects/Tex/Baked/manifest.json`。
- `IntegrationChecks.cs.txt` 检查专用服务器不生成、一组一槽、起始年龄、正常到期、重复绘制不推进、
  相机平移像素一致、队列登记后槽位复用与失效不绘制。
- 正式碎屑与第二版原型在第 1/7/12/25/40/51 tick 逐像素对照：所有像素差异均为 0。
- 已查看正式组合的飞行/命中、明暗背景、连续命中，以及 125% 缩放和不同发射方向代表帧。
- 正式 C# 编译通过。直接构建到当前 Mods 被正在运行的游戏以 TML003 拦截；独立目录打包成功。
  新包位于 `.vissandbox/ice-shard-preview/package-save/Mods/伊蕾娜.tmod`，11 张清单纹理 rawimg、
  当前 DLL、TMOD 哈希与工具排除均通过 `Tools/FixedTextureBake/check_package.py` 检查。
- 当前游戏仍加载旧包。退出游戏/释放模组占用后，可重新构建加载，或将已验证的新包复制到 Mods。
  尚未进行真实游戏与多人验证。

## 当前正式贴图版（2026-11-10）

用户要求取消模拟几何光照并减少蓝色。正式 IceShardVisuals 已删除 IceShardRenderer、面排序、
BasicEffect 和冰块动态顶点提交，改为六种不规则偏白静态贴图，运行时只采样轨迹、二维旋转与淡出。
主块/小块/亮点的数量、尺寸和寿命沿用原方案。旧几何源码仅在 Tools/IceShardPreview/GeometryReference.cs.txt 留作参考。

正式图集为 `ElainaModSkills/Skills/Ice/Assets/ShardAtlas.png`，128×160 RGBA8，无 mipmap，80 KiB。
上部六格 32×32（3 列、2 行），轮廓约 24px、四周至少 4px 透明边；source 原点 (16,16)。
柔光 source=(0,64,96,96)，原点 (48,48)；短线 source=(104,72,16,4)，原点 (8,2)，只有中间两行有像素。
使用预乘 RGBA、AlphaBlend 和 LinearClamp。离线生成器为 `Tools/FixedTextureBake/IceShardAtlas.cs.txt`，
8×覆盖率采样及固定浅色平涂切面，无三维光照计算；运行 `Tools/FixedTextureBake/run.ps1` 可复现。
旧 ShardGlow 已移除。正式资源由模组资源系统管理，卸载仅清理引用。

同组所有有效命中共用一次延迟 SpriteBatch，全部取样自同一图集。每次命中最多 57 个 sprite 提交，
这不是 GPU Draw Call 数。绘制回调按命中缓存；每次命中仍会分配粒子状态数组，KL 请求队列也有自身分配。
不声称整个特效零分配；未测游戏 FPS 或 GPU 毫秒。

验证：正式 C# 编译通过；FNA/D3D11 导出 240 帧，已查看原尺寸、2 倍细节、125%/不同方向、明暗背景与连续命中。
专用服务器、单槽、51 tick 寿命、重复绘制、相机平移、槽位复用与到期回归通过。
旧原型像素一致断言已取消，因为用户明确更改了形状表现与配色；六时间点改为导出当前正式实现截图。
图集满足 RGB≤A、全透明像素 RGB=0，清单 11 张 PNG 与离线生成数据一致。
固定资源校验对生成 C# 使用文本比较，允许 Git 换行归一化；未改动雨云源文件。
独立打包与包内 RGBA/DLL 校验通过。游戏正在使用的包未替换，重新构建加载后才能看到本版；尚未进行真实游戏/多人验证。
