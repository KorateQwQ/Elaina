# 扫帚冲刺双气流环 · FNA 离线预览

从仓库根目录运行：

```powershell
pwsh -File Tools/BroomDashPreview/run.ps1
```

输出在 `.vissandbox/broom-dash-preview`：`broom-dash.gif`、`contact-sheet.png`、75 张连续帧、左右与斜向、亮背景及 125% 缩放截图。窗口隐藏并自动退出；不操作游戏或真实存档。

正式绘制直接链接 `Mounts/BroomDashWindRenderer.cs`。时间线读取正式 `SkillBalance.json` 中的速度、冲刺持续时间、恢复时间和操控百分比；运动轨迹是场景夹具，人物用中性方块代替，扫帚使用正式贴图。预览加载 PNG 解码后的 RGBA，模拟本机 tML ImageIO.ToRaw 对 A=0 像素清零、其他 RGB 原样保留的路径。方向图用于几何检查，人物位置是固定示意。

## 来源与透明度

用户指定的参考：同一作者的 CoVFX `Content/Projectiles/SakuraDashRingRenderer.cs` 及 `SakuraDashProj.png`。
原始纹理 SHA256：`a166c4429577f4fe742b5481db88664eb53d11a4b5041985def0c88e11847af3`。

正式 `Mounts/Assets/BroomDashWind.png` 为 256×128 RGBA **直通 Alpha**。原图基本是 RGB 已乘 Alpha 的白色气流遮罩，通过以下可复现命令离线反预乘，保留 Alpha 通道：

```powershell
python Tools/BroomDashPreview/import_texture.py
# 或提供其他位置的同一源图：
python Tools/BroomDashPreview/import_texture.py <SakuraDashProj.png路径>
```

游戏使用 `BlendState.NonPremultiplied`，顶点 RGB 保持原色，仅以顶点 Alpha 消隐；不使用加色混合、Bloom 或发光叠层。转换脚本与预览均位于不打包的 Tools；游戏只请求预制 PNG。旋转、扩张、透视椭圆和生命周期是动态几何，保留运行时绘制。两个环共享一个 BasicEffect，旋转由年龄直接求值，避免共享渲染器的多次绘制互相修改相位。

## 游戏接入

成功点按冲刺后，拥有端在 PostUpdate 生成一次零伤害视觉弹幕，由原版弹幕网络同步；弹幕固定在冲刺起点，两个环位于后方不同距离。专用服务器不分配图形资源。复用实际生成方向，因此支持左右、上下与斜向；全局渲染器在卸载时释放自有 Effect，模组资产不手动释放。绘制恢复设备状态及原版 SpriteBatch shader。

2026-10-25：已查看 NonPremultiplied 版本的方向合图和启动/末段帧，检查两环、透明边缘、明暗背景和 125% 缩放；编译通过。真实游戏光照、相机跟随及双客户端联机尚未实测，不能将本预览称为游戏截图。

## 气流拖尾

第一轮 `options.ps1`（A/B/C/D）选过 C：拖尾沿扫帚毛尾部采样。2026-10-26 游戏内实测：斜向冲刺时拖尾偏在身体一侧、宽度只有 26px；冲刺后转身会让采样点左右跳 74px，拖尾也跟着折弯。

第二轮 `body.ps1` 输出到 `.vissandbox/broom-dash-preview/body`（`closeup.png` 1.5× 近景、各场景 `compare-*.png` 与 GIF），场景包括水平、左下（截图角度）、右上冲刺后按下转向，以及同一转向但冲刺后冻结。定稿为 **E 整体气流带 + 冲刺后原地收拢**：
- 路径改为骑乘中心，不再受朝向影响。采样点做 Catmull-Rom 插值（每段 4 段细分）。
- 一条与身体同宽的气流带（44→62px），加两条从身体上下缘 ±20px 收拢到 ±9px、反相摆动的风线，复用 `BroomDashWind.png` 不同的 V 区间，不新增贴图。
- 冲刺期间速度锁定在冲刺方向，路径必为直线。冲刺结束、撞墙或远端延迟导致速度偏离冲刺方向时，弹幕停止追加采样点，拖尾留在直线上，在 14 tick 内向终点收拢并淡出，不随转向弯折。

正式实现为 `BroomDashWindRenderer.DrawTrail`，`body.ps1` 的 E 列直接调用它。与定稿原型逐帧对比，只有两点插值的第 1 帧有个别像素差异，其余一致。采样只在客户端本地进行，不同步；冲刺 tick 数经 `ai[2]` 传入。双环与拖尾画在玩家层之下（`behindProjectiles`），剔除边距 1600。尚未在真实游戏与联机中复测。
