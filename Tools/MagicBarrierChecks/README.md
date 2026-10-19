# 魔力护盾回归验证

在模组根目录运行：

```powershell
& Tools/MagicBarrierChecks/run.ps1
& Tools/MagicBarrierChecks/preview.ps1
```

`run.ps1` 编译正式模组并调用实际减伤逻辑、生命恢复 IL 钩子和弹幕 AI。
图形反馈与引擎弹幕分配由测试替身提供，覆盖：

- 护盾技能及前置条件、直接受击减伤与耗魔、零魔力时抵挡负生命恢复。
- 首次负生命恢复立即生成护盾，持续 180 帧期间复用同一弹幕。
- 连续受击续期，不重置展开进度；淡出中受击平滑恢复。
- 恢复率为零或正数后不再续期，仅有负累计值也不触发显示。
- 远端与服务器不重复创建弹幕；持续续期每 20 帧同步一次；正式 ExtraAI 连同基类状态完成往返读写。

`preview.ps1` 使用 FNA 隐藏窗口，自动从当前 `MagicBarrierProj.cs` 提取 AI、续期和绘制代码，
并使用 KL 的球体 OBJ、顶点格式、模型解析器、投影矩阵、噪声贴图，以及正式 `MagicBarrier3D.xnb`。
固定随机溶解偏移，导出持续负恢复、连续受击、淡出中续期三组状态，共 27 帧。
输出位于 `.vissandbox/magic-barrier-preview/out/contact-sheet.png`，逐帧计时记录为 `frames.csv`。
脚本默认 tModLoader 路径为 `D:/Steam/steamapps/common/tModLoader`，可通过 `-TmlPath` 改写。
拼图需要 Python/Pillow。

这些是 FNA 离线预览与本地逻辑检查，不是游戏截图；未模拟真实网络延迟、丢包或完整游戏生命周期。
多人游戏仍需检查拥有者与旁观者同时看到续期效果。
