# 经验光球

敌对 NPC 死亡时，单机/服务器向 1200 像素（75 格）内每名存活伊蕾娜各生成一颗独立光球，无需击杀归属、不平分奖励。第一版每颗 10 经验、每级 100 经验，集中在 `System/Experience/ExperienceRules.cs`。

排除城镇/友好 NPC、小动物、雕像怪、练习假人、弹幕型 NPC 和共享生命的从属节段。光球上浮 30 tick 后穿墙追踪固定接收者，超过最初分配范围仍继续追踪；死亡、离线、切换角色、过期只清理，不奖励、不转移。服务器 owner 为 255，ai[0] 为接收者、ai[1] 为经验、ai[2] 为年龄，避免服务器生成客户端 owner 弹幕时的 AI 初始化/同步问题。

服务器确认抵达且只结算一次，定向通知拥有者；拥有者通过现有 `ElainaStatePlayer` 存档保存经验与等级。该流程沿用角色本地存档模型，不是服务器托管角色系统。Boss 进度控制等级上限；到上限最多暂存一条经验，解锁上限后消耗。等级计算支持一次跨多级并保留余数。

运行：

```powershell
powershell -ExecutionPolicy Bypass -File Tools/ExperiencePreview/run.ps1 -Checks
powershell -ExecutionPolicy Bypass -File Tools/ExperiencePreview/run.ps1
dotnet msbuild 伊蕾娜.csproj -t:Compile -p:BuildProjectReferences=false -v:quiet -clp:ErrorsOnly
```

检查直接链接实际死亡钩子、光球 AI、网络处理、经验规则及 `ElainaStatePlayer`。引擎与传输使用确定性桩，覆盖多人各得一份、距离边界、接收者失效、槽位复用、吸收与过期、客户端拒绝结算、升级上限与存档。不能替代真实 tModLoader 双客户端联机测试。

FNA 离线预览直接链接实际运动、轨迹采样和绘制，模拟两名玩家与地形，输出 `.vissandbox/experience-preview/experience-sequence.png`（从左到右、从上到下为 8、29、45、70 tick）及 `experience-detail.png`（上暗底、下亮底，左 1x、右 3x）。细节图使用真实运动规则的第 45 tick 轨迹。玩家轮廓、场景和经验条是夹具；不模拟游戏音频、实时网络延迟或引擎生命周期。工具源码为 `.cs.txt`，生成物位于被排除的 `.vissandbox`，不进入模组编译与打包。

预览从实际 Terraria Content 加载 `Images/MagicPixel.xnb`（1×1000），不再用自建 1×1 贴图替代正式资源。默认从 tModLoader 同级的 `Terraria/Content` 查找，也可通过 `-TerrariaContent` 指定。每次预览运行 18 项真实资源尺寸回归：上浮/追踪阶段、1x/1.25x/3x、PointClamp/LinearClamp，逐像素对比单白像素参考，防止漏写取样区域后把整张长条纹理拉伸到屏幕上。

2026-10-23：34 项经验流程检查、25 项现有角色进度回归检查及正式源码编译通过。已打开本次 FNA 帧条和明暗背景细节图，确认浮出、分别追踪、吸收及原尺寸可读性。真实双客户端联机仍需游戏内验证。现有角色回归脚本含中文路径，使用 `pwsh -File Tools/CharacterProgressChecks/run.ps1` 执行，避免 Windows PowerShell 5.1 对无 BOM UTF-8 的误读。

同日绘制修正：用户游戏截图暴露出最初预览的 1×1 资源夹具掩盖了 MagicPixel 实际尺寸。改用真实 XNB 复现了长斜线，再为所有光球绘制指定 `(0,0,1,1)` 取样区域。此前的外观结论只覆盖了错误资源夹具，不能作为游戏中尺寸正确的证据。

2026-10-24：主体改为带轻微明暗呼吸的四尖十字星光。客户端记录最近 20 tick 的世界坐标，绘制最长 110 像素、沿真实轨迹弯曲并逐渐收细的紫色拖尾；碎星按出生 tick 保持方向并向尾迹两侧散开。超过 160 像素的位置跳变清空旧轨迹，专用服务器不分配视觉缓存。继续使用原有 SpriteBatch 和 MagicPixel，无新增贴图。18 项真实资源检查现同时覆盖星光、拖尾与碎星；本次 34 项经验流程检查、18 项资源检查和正式源码编译通过，已查看四阶段帧条及明暗底 1x/3x 细节图。游戏内观感和真实网络纠正仍需实机验证。
