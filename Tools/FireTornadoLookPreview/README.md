# 火焰龙卷观感预览

把完整的 `FireTornado` 绘制（主体合并层、高亮残片、暗条带、石头粒子、正反两面气流圈、火星粒子）放进 KL 真实的 Bloom 管线（从 KL 源码编译，同 `Tools/BloomBackbufferPreview`），合成到 8 位场景上，用于比较配色与强度方案。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/FireTornadoLookPreview/run.ps1
```

输出在 `.vissandbox/fire-tornado-look/out`：

- `day-*.png` / `night-*.png`：白天/夜晚背景、第 40/75 帧，从左到右依次是 `Program.cs.txt` 中 `Looks()` 列出的方案，缩小一半。`current` 与 `FireTornado` 当前数值一致，改游戏数值时同步修改 `Look` 的默认值。
- `current-night-crop.png`：当前方案夜晚第 75 帧的全分辨率局部，用于检查火星细节。火星的运动与绘制在 `ElainaModSkills/Skills/Fire/FireTornadoSparks.cs`，游戏中的 `FireTornadoSparkDust` 与预览共用；预览按帧数预热粒子，种子固定，背面火星画在龙卷之前、正面火星画在最后，对应游戏中 Bloom 请求的 order。
- `seq-<方案>.png`：淡出进度 0 → 0.9 的序列（淡入为反向），用于检查火焰层先消融、暗色层延后的时序。

背景、地面与树只是色块；背面气流圈画在 BehindNPCsAndTiles 层，之后画上的色块会遮住它。不模拟光照、相机缩放与其他图层，最终效果仍以游戏内为准。
