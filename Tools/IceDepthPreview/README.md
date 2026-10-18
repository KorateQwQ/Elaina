# IceShardLock 冰锥深度预览

## 冰锥发射瞬间拖尾

运行 `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/IceDepthPreview/run.ps1 -Launch`。
每次提取实际 `IceConeProj.AI/UpdateMovement/DrawBehind`、KL 距离采样与 `ValidTrailArray`，
直接编译实际 `TrailManager`，并使用当前拖尾贴图、模型、Shader 和 bloom 绘制。
检查第 24/25/26/27/30/35 帧，覆盖静止、移动、左右发射、蓄力转向，以及每三帧采样一次。
断言蓄力阶段不提交拖尾、历史点不混入飞行轨迹、正常采样的首个前冲段为 140 像素、总长不超过 500 像素。
`launch-*-before-*.png` 恢复从生成时采样及第 25 帧提前提交，用于复现旧问题；
`launch-*-after-*.png` 为当前源码结果。输出位于 `.vissandbox/ice-depth-preview/out`。
这是 FNA 离线预览：玩家位移和目标方向为固定输入，省略粒子、光照与 NPC 索敌，
不验证游戏碰撞、联机或真实更新调度。

追加`-Tracking`导出当前冰凌锁跟随预览：第12、28、29、65、96、109帧。运动、模型、绘制与`MoveWithTarget`来自实际源码；矩形为模拟目标，前29帧移动，首发抵达后固定并变为青色。该标记不属于正式技能特效，不证明游戏控制物理或联机正确。输出`tracking-*.png`；分段伤害/冰封行为另由`Tools/IceCycleChecks/run.ps1`检查。

运行 `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/IceDepthPreview/run.ps1`。
默认使用 `D:/Steam/steamapps/common/tModLoader` 的 FNA，可通过 `-TmlPath` 指定安装目录。
输出位于 `.vissandbox/ice-depth-preview/out`。

每次运行都重新提取实际 `IceCone3DUnit` 绘制、运动、溶解方法和
`IceShardLockProj` 的生成配置，并加载实际 OBJ、贴图和已编译的 IceCone3D Shader。
直接编译当前 `IceConeDepthRenderer`、KL 分层请求队列及投影代码。

检查第 25、65、109 帧；第 109 帧反转冰锥提交顺序，要求图像完全一致。
关闭深度测试作为反例，要求正反提交的图像不同。
同时检查无深度场景目标的 DiscardContents、PreserveContents 以及直接绘制到
backbuffer 的输出一致，并确认 backbuffer 的保存设置得到恢复。

这是 FNA 离线预览。使用固定相机、白色光照，不模拟 NPC、粒子和拖尾，
也不验证游戏生命周期、碰撞或联机。消融中的半透明边缘不属于第 109 帧的顺序无关断言。
工具使用 `.cs.txt`，生成源码在 `.vissandbox`，两目录均由现有 buildIgnore 排除。

## IceConeProj 单冰锥

同一命令追加 `-Single`，会重新提取 `IceConeProj.PreDraw` 请求和 `DrawVisual` 的实际模型绘制部分，
跳过拖尾与基类弹幕逻辑，使用真实模型、贴图和 Shader，在固定位置检查第 8、20、35 帧。
`single-020-outline.png` 仅将 pass 切回旧的 Outline，用于复现纯蓝色轮廓；
`single-020-base.png` 是修复后的贴图表面。
每个时间点分别在无深度目标和深度清零的 Depth24 目标上绘制，要求像素一致，
验证单冰锥明确忽略深度。此模式不调用冰锥群的深度渲染器。

## Bloom 接入

同一命令追加 `-Bloom`，会重新提取 KL `DrawSystem` 的完整 RequestBloom 合成流程、
`RenderHelper` 及相关绘制方法，加载实际 Bloom/ReColor Shader。
检查两类冰锥开关 bloom 的外观、轮廓外光晕、开启 bloom 后的冰锥群反序遮挡、
深度中间缓冲的 HDR 值保留，以及场景目标与 backbuffer 两条合成路径。
不传 `-Bloom` 时通过 KL 的关闭后处理分支执行请求，继续运行原有深度/单冰锥检查。

`bloom-group-on.png` 和 `bloom-single-on.png` 为带 bloom 的离线预览；
`bloom-*-appear.png` 检查消融显现阶段。当前截图只覆盖模型本体，未模拟拖尾、
IceShardLock 控制器中心星芒、粒子、游戏光照和真实角色。

Bloom 模式还会在场景 RT 和 backbuffer 上分别绘制 RGB 为 0、0.5、1、1.01、2 的 HDR 测试块，
检查降采样结果：小于等于 1 必须全黑，大于 1 必须产生高光。
阈值沿用 KL 现有 Overflow pass 的亮度定义 `dot(rgb, (0.3, 0.59, 0.11)) > 1`。
另外记录冰锥飞行帧的实际 HDR 峰值，并比较传入白色/暗色 lightColor 的输出，
以区分输入基础色、Shader 最终输出和环境光参数是否实际生效；这仍不是游戏昼夜模拟。

## 拖尾遮挡与 Alpha

同一命令追加 `-Trail`，会执行完整 `IceConeProj.DrawVisual` 和实际 KL `TrailEffect`，
使用原始拖尾贴图及 VertexDraw Shader；历史轨迹为固定直线样例，不模拟实际 AI。
对左右朝向分别比较“本体单独绘制”和“先拖尾、后本体”，要求不透明本体内的 HDR 像素完全一致。
反例仅恢复旧的本体 Alpha=1.1、消融边缘 Alpha=4.5，用来重现明亮拖尾造成的暗条；
不改变 RGB、混合状态或绘制顺序。`trail-*-before/after-bloom/plain.png` 是可视对照。
其他模式仍默认禁用拖尾。拖尾模式保留无深度绘制，未模拟多弹幕之间的层叠或真实游戏生命周期。
