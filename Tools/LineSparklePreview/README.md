# LineSparkle 共享 Bloom 检查

运行 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/LineSparklePreview/run.ps1`。
通过 `-Dotnet` 和 `-TmlPath` 可覆盖本机 .NET / tModLoader 路径。
输出位于 `.vissandbox/line-sparkle-preview/out`。

复用 BloomBackbufferPreview 的实际 KL Bloom 编译器，直接抽取当前 `LineSparkle` 和 `LineRotSparkle` 的绘制、请求方法与字段，使用正式 `Effects/Tex/Sparkle/ShotLine.png`、ReColor/Bloom XNB。替身只提供 Dust 状态与设备上下文，不模拟游戏 AI、角色、照明或联网。

检查弹幕占位图形、四个 LineSparkle 和两个 LineRotSparkle 全部进入同一个 `Projectiles.After` 请求桶；不同 id 只是批次分组，不增加 Bloom 层。比较开关 Bloom 的像素差异，检查生命进度 0、0.5、0.9、1，以及提交后失活、改变类型、替换 customData 的粒子均不再绘制。输出白天、夜晚与淡出截图供视觉检查。

所有宿主源码使用 `.cs.txt`，生成项目位于已排除的 `.vissandbox`，不参与模组构建与打包。
