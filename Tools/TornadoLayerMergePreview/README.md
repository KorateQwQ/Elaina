# 火焰龙卷叠层合并对比

`FireTornado` 把形状参数相同的叠层合并成一次绘制（KL `TornadoLayered.fx`、`SphereEffect.fx` 的 dualFront/dualBack pass）。
这个工具用 KL 编译好的 XNB shader 和真实贴图，在 FNA 中分别执行合并前的逐层绘制和合并后的绘制，逐像素比较。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/TornadoLayerMergePreview/run.ps1
```

输出在 `.vissandbox/tornado-layer-merge`：`report.txt` 为每个场景的比较结果，`*-body.png` / `*-slash.png` 左为合并前、中为合并后、右为差异 ×50。
修改 `.fx` 后先在 `KL/Effects` 运行 `XNBCompiler.exe` 重新编译。

- 场景覆盖淡入、完整、淡出的 5 个时间点，气流圈的正面和内壁。
- `Vector4` 目标对应开启 Bloom 时的捕获目标（KL `BloomStraightAlphaBlend` / `BloomAdditiveBlend`）。
- `Color` 目标对应关闭 Bloom 时直接画到场景上（XNA `NonPremultiplied` / `Additive`），只比较 RGB。气流圈在 8 位目标上按 `TornadoLayerRenderer.TargetClampsOutput` 退回逐层绘制。
- 容差为 0.5% 相对误差；消融与轮廓的硬阈值上允许少量像素因浮点舍入翻转（每张图不超过 64 个）。

不模拟游戏内的 Bloom 模糊、光照与相机缩放，最终效果仍需在游戏中确认。
