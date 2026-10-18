# 水球蓄力水流：FNA 离线预览

在项目根目录运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/WaterChargePreview/run.ps1
```

`-TmlPath` 可指定 tModLoader 安装目录；脚本自动用 Python/Pillow 合成 GIF。
输出：`.vissandbox/water-charge-preview/out/water-charge.gif`、`contact-sheet.png` 和逐帧 PNG。

每次运行刷新实际 `WaterChargeFlowRenderer.cs`，通过 Roslyn 提取当前 `WaterBall.DrawBall`，
使用项目 WaterBallEffect.xnb 和本模组/KL 原贴图，按水流后层 → 水球 → 水流前层顺序绘制。
仅移除水球方法的飞行拖尾分支；绘制助手适配 SpriteBatch 和本地资源加载。
水流直接加载本模组和 KL 材质，使用 WaterChargeFlow.xnb 的顶点/像素 Shader。
主条带与白沫使用一致的曲线和 UV 滚动，噪声轻微扰动横向采样。
Shader 区分黑底灰度图（亮度×Alpha 遮罩）和彩色透明底图（Alpha 遮罩+原材质颜色），
以预乘 Alpha 输出；白沫正常覆盖青蓝水体，只有底层柔光采用零 Alpha 加色。窄条带采样对像素覆盖范围内四点取平均，减轻高分辨率水纹的颗粒。
没有 CoVFX 运行时依赖，曲线上移动脉冲的思路参考其 SakuraKiriChargeFlow。

覆盖开始到三秒蓄力的 90 帧、日间背景/1.35 倍缩放、不同弹幕种子/0.8 倍缩放和无条带对照。
本场景模拟蓄力时间、球体成长和固定球心，不模拟玩家、实际相机、游戏光照、碰撞或联机。
游戏里还需检查移动瞄准时的衔接及松手发射；正式入口仅在 Hold 状态绘制水流。

源码编译（不会尝试覆盖运行中的模组包）：

```powershell
dotnet msbuild '伊蕾娜.csproj' -t:Compile -p:BuildProjectReferences=false -v:quiet
```

工具源以 `.cs.txt` 保存，生成内容在 `.vissandbox`；现有 buildIgnore 排除两个工具目录，
SDK 默认排除点号目录的编译输入。独立进程批量绘制使用隐藏窗口并自动退出。

## C# 直接调参

直接编辑 `WaterChargeFlowRenderer.Draw` 开头的局部变量调参区。
`trailPath`、`foamPath`、`splashPath`、`noisePath`、`glowPath` 分别控制主条带、白沫、破浪图集、噪声和柔光。
路径使用 `/`，不带扩展名。每次 Draw 都通过资源缓存获取当前路径对应的贴图，
没有构造时固定的材质或参数快照；应用方法体热重载后，下一次绘制读取新值。
`widthMultiplier`、`brightness`、`foamStrength`、`noiseStrength`、`textureRepeat`、
`textureScrollSpeed`、`noiseScrollSpeed` 均在旁边附有中文注释。

日常在已连接的 C# 热重载环境中直接改变量即可，不需要 JSON 或预览命令。
修改方法体局部变量可沿用已有 C# 热重载环境；更换 Shader 字节码后需要重新加载对应资源。
上面的独立预览仅作可选离线验证工具，不能替代调试器热重载，也不会自动更新正在运行的游戏。

若修改 `Effects/Content/WaterChargeFlow.fx`，先执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/WaterChargePreview/compile-shader.ps1
```

脚本在隔离目录编译并核对时间戳，更新本模组的 WaterChargeFlow.xnb，不修改 KL。


## 当前水流材质与形状

- 主体：本模组 `Eff_Splash_322_288`，青蓝色水体，不再只有细亮丝。
- 白沫：同一张材质的白色区域，按 `min(R,G,B)` 提取，避免把高亮青色当成白沫。
  同路径、同 UV、同宽度覆盖水面；使用正常透明混合，白色不受 `mainColor` 染色。
- 边缘破浪：本模组 `Eff_Splash_4001` 的 2列×6行图集。近景每股两处、远景每股一处白沫。
  减少碎滴密度，以便看清汇聚方向。按脉冲位置确定性移动和淡入淡出，不增加游戏实体。
- `foamStrength` 控制白沫覆盖强度，`foamThreshold` 越低白色区域越多，
  `foamSpread` 控制横向展开，`foamSoftness` 控制边缘，`foamColor` 默认近白色。
- `splashStrength` / `splashSize` 控制边缘浪花强度/大小，`waterOpacity` 控制蓝色水体透明度。
- `trailUsesAlpha`：黑底灰度图设 `false`；透明底彩色图设 `true`（当前默认）。
  `foamPath` 优先与 `trailPath` 相同，以保持白沫与水体形状对齐。
- `ribbonWidth` 控制中段宽度，`widthMultiplier` 为倍率；头尾短距离收尖，尾部透明度渐隐。
- `textureLength` 是纹理重复对应的路径像素长度，数值大则水纹舒展；`textureRepeat` 进一步调整重复次数。

路径用弧长表进行等距采样，材质 UV 根据真实路径距离计算；去掉多层正弦宽度调制与
偏离实际切线的横向抖动，避免中段鼓包、弯曲处材质被挤压、顶点条带发生交错。
`Segments` 热调时顶点和索引一起重建，避免沿用旧尺寸数组。


## 蓄力时序与空间层次

只在水球成长阶段绘制一轮 9 股水流，每隔 0.05 秒（3帧）错峰进入；0.45 秒前停止产生，
全部水流最迟在 0.8 秒（48 帧）收完。已有水球逻辑为 50 帧长到最大、60 帧完成显现。
每股只播放一次，不按时间取模循环；超过截止时间直接返回，不请求材质或切换 GPU 状态。
较晚的水流会根据剩余时间加速收完，避免到终止帧才突然消失。

`Draw` 开头可以直接修改 `streamCount`、`spawnInterval`、`spawnReach`、`stopEmissionSeconds`、`finishSeconds`、
`flowSpeedMultiplier`。当前速度倍率 3.2，纹理滚动速度 2.1；保留浅蓝底色和白沫。
每股具有不同的深度、长度、粗细和亮度，近景前置、远景被球体遮挡；近景入球时收小，
远景入球时略放大。外侧起点距离基准 `spawnReach = 430f`，较上一版 310 再扩大约四成；仍叠加各股远近差异。
起点不随球长大而外移，长距离汇聚保持在原有时间内完成。

预览帧条重点覆盖 0.2～0.87 秒，并保留 3 秒完整动画。
额外像素检查覆盖四个种子：0.2 秒有水流，0.8/0.84/1/1.6/3/10 秒无水流残留。
这些检查针对蓄力视觉，不改变实际从水域吸水的 WaterTrail 判定、伤害和发射逻辑。


## 游戏入口计时回归

预览会提取实际 `WaterBall.DrawChargeFlow` 调用的时间参数并编译执行，
检查世界运行 0/60/3600 秒时，水流始终按新水球自身的年龄开始。
修复前 `VisualTime / 60f` 可触发该检查失败；它来自 KL 的全局视觉时钟，
会让一次性水流误以为已经超过 0.8 秒而不绘制。入口现使用 `time / 60f`，
与水球生长、显现计时一致。模拟图像也通过该实际参数表达式传入时间。
