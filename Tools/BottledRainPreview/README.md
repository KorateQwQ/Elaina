# 瓶中雨：功能与 FNA 预览

正式实现位于 `ElainaModAlchemy/Rain/`，物品入口为 `item/Curios/BottledRain.cs`。

2026-10-24 后续：游戏内默认云层已换为 Shader 体积云；本目录保留原贴图版本作对照及回退预览。当前正式视觉、状态恢复检查与打包命令见 `Tools/BottledRainShaderPreview/README.md`。下述玩法仍适用，原贴图预览不代表当前默认云层。

投掷消耗一瓶，撞击或飞行 45 tick 后，同一弹幕展开为持续 60 秒的固定雨云。云向上展开最多 96 像素，受天花板限制；雨幕宽 320 像素（20 格），从云下沿向下最多 320 像素，逐列遇到实心地面、屋顶或液面停止。平台与植物允许雨水通过。无伤害、不割草，不产生真实液体，也不修改全局天气。

当前每 10 tick 由单机/服务器为雨幕内现有植物调度一次额外的原版随机生长机会（用户已从最初的 30 tick 调快）。覆盖药草、树苗、宝石树苗、南瓜、仙人掌、竹子、藤蔓及原版风动植物；树木空间、土壤、深度及其他药草的开花条件仍由原版决定。原版地表/地下单格更新是非公开方法，`RainPlantGrowth` 按当前安装程序集缓存其委托；更新 tModLoader 后应重跑真实引擎检查。已标记为对应植物集合的模组植物也会收到原版调度内的 `TileLoader.RandomUpdate`，不承诺所有模组自定义生长系统都支持。

受影响植物现在由 `RainPlantIndicatorSystem` 显示向上增益箭头，覆盖与遮挡沿用同一套浇灌扫描；当前预览见 `Tools/BottledRainShaderPreview`。本目录的旧贴图预览不模拟该叠加层。

成熟水叶草转为真实的 `BloomingHerbs`（style 4），能正常收获种子；开花状态保留到收获，不临时篡改 `Main.raining`。植物变更沿用原版服务器图块同步。云的阶段、年龄与展开高度使用弹幕 `ai` 同步，晚加入按已过时长计算剩余时间。

外观直接复用正式绘制源码及 Terraria 的 `MagicPixel.xnb`（只采样 1×1）、`Projectile_238.xnb`（54×168 六帧图集，逐帧取样）。包含开瓶碎片、上升展开、蓝紫云层、细雨、落地水花、微光及末尾淡出，不新增正式贴图。

```powershell
pwsh -File Tools/AlchemyItemChecks/run.ps1 -RainOnly
pwsh -File Tools/BottledRainPreview/run.ps1
pwsh -File Tools/AlchemyPreview/run.ps1 -Scene curio-rain
dotnet msbuild 伊蕾娜.csproj -t:Compile -p:BuildProjectReferences=false -v:quiet -clp:ErrorsOnly
```

输出位于 `.vissandbox/bottled-rain-preview/`：

- `bottled-rain.gif`：共享实际绘制的 90 帧动画。
- `rain-sequence.png`：12、120、600、3575 tick；第三格为亮背景。
- `rain-125.png`：125% 缩放。

FNA 的地形、雨落距离和人物是夹具，药草使用真实原版图集；植物生长另由真实 tML Tilemap 检查验证，不能把画面中的预设开花当成功能验证。预览不模拟网络传输、完整游戏碰撞更新、音频或存档。

2026-10-24：29 项真实引擎检查通过，包括种子成熟开花、种子收获判定、南瓜成熟、树苗长树、屋顶阻挡、无全局天气/液体写入、客户端拒绝改图块、弹幕转换、过期及低顶展开。已查看 FNA 多阶段、明暗底及 125% 图和炼金手记 100%/125% 相关场景；正式资源完整性检查通过。完整 `AlchemyItemChecks` 当前被既有 `RevealingDust pixel asset has catalog resolution` 不一致阻断；瓶中雨检查有独立入口。仍需游戏中验证投掷手感、实际多人同步及整合包的植物钩子兼容。
