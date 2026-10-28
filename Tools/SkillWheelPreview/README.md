# 技能轮盘检查

2026-10-28：玩偶条目现在直接调用召唤/指令入口，不改变当前术式。玩偶盘松开开启键后保持显示，点击环带执行，点击中心 X 仅关闭，点击外部不执行指令；确认点击保留输入所有权直到松开。`check-timing.ps1` 已更新为 17 条技能盘与 18 条玩偶盘断言，覆盖新的点击语义、中心关闭、边界命中和原有计时/生命周期。`run.ps1 -DollCommands` 使用正式 `CancelHub` 绘制开关生成中心关闭按钮的 FNA 离线预览，已检查 100% 和 150% 缩放。


```powershell
pwsh -File Tools/SkillWheelPreview/check-timing.ps1
pwsh -File Tools/SkillWheelPreview/run.ps1
dotnet build 伊蕾娜.csproj -t:Compile --no-restore -p:BuildProjectReferences=false --nologo -v:q
```

默认使用项目 `tModLoader.targets` 对应的 `D:/Steam/steamapps/common/tModLoader`，其他安装位置可通过 `-Tml` 指定。

`check-timing.ps1` 直接编译正式 `ElainaSkillWheel`、`ConstellationUIClock` 和绘制曲线，仅替换游戏/SUI 边界。检查游戏帧时间为零、极小、陈旧或过大时仍按真实经过时间展开；慢帧不丢失时间；松键确认、关闭计时重置、隐藏后重开、快速反向和失焦/退出清理。

2026-10-27：同一入口加入正式 `DollCommandWheel`、`DollCommand` 和 `DollTimingChecks.cs.txt`。修改前，玩偶盘在零游戏帧时间的展开检查失败；接入独立时钟后，15 项技能盘与 14 项玩偶盘检查全部通过。玩偶检查覆盖零/极小/陈旧时间、慢帧、完整展开、松键取消任务、关闭计时重置、隐藏重开、右键取消与松键释放、失焦、退出 UI 树和世界重置。源码编译 0 错误；已查看本次 `run.ps1 -DollCommands` 生成的 100% 和 150% FNA 离线预览。预览验证绘制外观，计时检查隔离了游戏/SUI 边界，仍需反馈问题的玩家在原跳帧设置下复测。

2026-10-28：玩偶盘阵心改为随指向切换：指向某个指令（及其选中后的淡出）时显示 `WheelItem.Label` 功能名，指针回到中心或未指向任何项时恢复取消按钮。英文名放不下时在中间空格处折两行。`run.ps1 -DollCommands` 现输出 `doll-idle`、`doll-hub-cancel`、四个指令的中/英文悬停（`doll-hover-*`）和取消任务选中淡出帧（`doll-chosen-close-*`）；预览按 UI.hjson 手写标签，不经本地化。计时桩补了 `UIText.LocalizeUI`。17 项技能盘与 18 项玩偶盘检查通过，`-t:Compile` 源码编译 0 错误；已查看 100% 与 150% 预览。同日“取消任务”图标由紫色禁止环（带亮边高光，与阵心取消按钮重复）改为任务单 + 红色叉号角标，与“收回玩偶”的角标同一画法；由 `Tools/DollCommandIcons/BuildPixelDollCommandIcons.py --apply --only DollCommand_Cancel` 生成，已在本预览的 100%/150% 轮盘中核对。

轮盘与书页共用 `ConstellationUIClock` 的独立 Stopwatch 实现，每个 UI 各持有自己的实例。SUI 在绘制阶段调用 `UpdateStatus`，不能把 `Main.gameTimeCache.ElapsedGameTime` 当作两次显示之间的间隔，尤其是微妙跳帧模式。打开和关闭时重启采样，完全隐藏时停止；不限制每次采样为 100ms，以免低帧率下延长动画。

`run.ps1` 生成 FNA 离线预览到 `.vissandbox/skillwheel-preview/output`，链接正式 `SkillWheelDrawing` 和 `SkillBarDrawing`，覆盖开合中间帧、数量、悬停、冷却、封印及缩放。扇区绘制使用 BasicEffect 适配，未运行完整游戏输入树，也不能测量游戏中的真实 FPS；游戏内仍需复测按住、松开和跳帧模式。

源码编译命令复用已有依赖和还原结果，不打包或重载正在运行的模组。2026-10-20 检查结果：旧轮盘在零游戏帧时间的展开检查中失败，修复后 15 项检查通过；完整源码编译 0 错误。已查看本次生成的 80ms/300ms 展开、100ms 收拢和 1.5 倍悬停预览。
