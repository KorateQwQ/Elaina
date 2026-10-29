# 瓶中雨穿墙投掷与瞄准预览

瓶子穿过墙、地面、半砖、斜坡和种植盆，沿抛物线到鼠标选定位置后展开。鼠标位置在出手时锁定；飞行速度随距离计算，不再按固定速度飞行 45 帧。现有雨云向上展开和雨水遮挡规则保留：雨云不会向上穿过屋顶，雨水不会浇灌实心遮挡下方的植物。

手持瓶中雨时，`RainAimPreviewSystem.PostDrawTiles` 使用与正式弹幕共用的 `RainThrowTrajectory` 预测落点和展开位置。蓝色虚线显示投掷路径，金色菱形标出落点，淡蓝覆盖区随屋顶、地面和液体截断。绿色箭头和角框标出已有植物；种植盆、黏土盆中的草药连同容器框出，空盆不标记。打开背包、全屏地图、悬停 UI、投掷动作期间、死亡或切走物品时隐藏。

范围与植物复用 `RainPlantGrowth.MeasureRain` / `CollectCoveredPlants`，扫描不会生长植物或发送网络消息。最下沿统一排除只有边界接触、没有实际重叠的下一行。预览是静态即时反馈，没有按帧累加的动画；绘制只用已有 MagicPixel，没有生成运行时固定贴图。

## 复现

```powershell
pwsh -File Tools/AlchemyItemChecks/run.ps1 -RainOnly
pwsh -File Tools/BottledRainAimPreview/run.ps1
dotnet msbuild 伊蕾娜.csproj /t:Compile /p:Configuration=Debug /p:BuildProjectReferences=false
```

`check.ps1` 链接正式瓶中雨物品、雨云、弹道、范围及植物源码，使用本机真实 tModLoader/FNA、Tilemap、碰撞及原版生长代码。只适配与本任务无关的 AlchemyItem 配方/目录基类，不模拟 Terraria 类型；KL 使用相邻项目现有 Debug DLL。旧综合炼金检查当前依赖其他子系统的完整玩家/UI 上下文，因此 `-RainOnly` 转到此独立入口，不扩大本次修复范围。

检查包括：近/远/上下/左右/零距离与倒重力、穿过实心墙/半砖/斜坡、实际物品 Shoot 仅创建一瓶、非所有者不重复创建、飞行中位置/速度/ai 复制后抵达相同落点、预览可见条件、多层盆栽/空盆/屋顶遮挡、范围最下沿以及既有生长、开花、过期回归。

`run.ps1` 先运行上述检查，从真实 Tilemap 导出 `scene.json`，再直接链接正式 `RainAimDrawing` 与 `RainPlantIndicatorDrawing`，加载本机原版图块贴图生成 FNA 离线图。输出在 `.vissandbox/bottled-rain-aim/`：`aim-night-1.00.png`、`aim-day-1.00.png`、`aim-night-1.25.png`、`aim-day-1.25.png`。场景包括投掷路径穿墙、屋顶挡雨、上下两层种植盆、黏土盆、空盆及范围外植物。

2026-10-30：127 项真实引擎检查通过。已查看夜间 100% 与日间 125% FNA 图。正式源码编译通过，保留项目既有警告。尚未验证运行中游戏的实际操作和真实多人网络；离线同步检查只复制弹幕状态，不建立连接。此工具不启动或操作用户游戏，不修改存档。
