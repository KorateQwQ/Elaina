# 技能轮盘检查

```powershell
pwsh -File Tools/SkillWheelPreview/check-timing.ps1
pwsh -File Tools/SkillWheelPreview/run.ps1
dotnet build 伊蕾娜.csproj -t:Compile --no-restore -p:BuildProjectReferences=false --nologo -v:q
```

默认使用项目 `tModLoader.targets` 对应的 `D:/Steam/steamapps/common/tModLoader`，其他安装位置可通过 `-Tml` 指定。

`check-timing.ps1` 直接编译正式 `ElainaSkillWheel`、`ConstellationUIClock` 和绘制曲线，仅替换游戏/SUI 边界。检查游戏帧时间为零、极小、陈旧或过大时仍按真实经过时间展开；慢帧不丢失时间；松键确认、关闭计时重置、隐藏后重开、快速反向和失焦/退出清理。

轮盘与书页共用 `ConstellationUIClock` 的独立 Stopwatch 实现，每个 UI 各持有自己的实例。SUI 在绘制阶段调用 `UpdateStatus`，不能把 `Main.gameTimeCache.ElapsedGameTime` 当作两次显示之间的间隔，尤其是微妙跳帧模式。打开和关闭时重启采样，完全隐藏时停止；不限制每次采样为 100ms，以免低帧率下延长动画。

`run.ps1` 生成 FNA 离线预览到 `.vissandbox/skillwheel-preview/output`，链接正式 `SkillWheelDrawing` 和 `SkillBarDrawing`，覆盖开合中间帧、数量、悬停、冷却、封印及缩放。扇区绘制使用 BasicEffect 适配，未运行完整游戏输入树，也不能测量游戏中的真实 FPS；游戏内仍需复测按住、松开和跳帧模式。

源码编译命令复用已有依赖和还原结果，不打包或重载正在运行的模组。2026-10-20 检查结果：旧轮盘在零游戏帧时间的展开检查中失败，修复后 15 项检查通过；完整源码编译 0 错误。已查看本次生成的 80ms/300ms 展开、100ms 收拢和 1.5 倍悬停预览。
