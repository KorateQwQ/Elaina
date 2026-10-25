# 宝箱提示的熊头像角标

正式绘制为 `DollChestCursorDrawing.cs`，挂接为 `DollChestCursorSystem.cs`。在原版 `TileInteractionsMouseOver`（包括模组 MouseOver 回调）结束后记录宝箱提示；在原版 `DrawInterface_40_InteractItemIcon` 绘制后追加头像。位置使用本次提示图标的实际帧尺寸、`cursorItemIconPush`、`cursorScale` 和现有 UI SpriteBatch，因此宝箱物品、钥匙、动态帧与模组物品图标均使用相同的右下角定位。

标记表示有空闲玩偶可以提供远程开箱入口，不代表钥匙、进度或模组自己的条件已满足。没有空闲玩偶、非宝箱、鼠标被界面拦截、全屏地图、旧提示缓存淡出时不绘制。纯文字的命名宝箱提示没有物品图标，不额外制造图标或覆盖原名称。原版在玩家更新后增加 GameUpdateCount，记录允许当前更新及紧接着的绘制 tick，同时复查提示 ID 和启用标记，避免头像因计时差消失或残留。

`Assets/ChestCursorBear.png` 是 19×16 RGBA 预制资源，使用指令盘同一个 `kuma_head` 裁剪与下巴补边方法，增加一像素深色外轮廓，不包含红色收回叉号。二值 alpha 无需额外预乘转换；正式游戏只加载打包 PNG。

```powershell
python Tools/DollCommandIcons/BuildChestCursorBadge.py
python Tools/DollCommandIcons/BuildChestCursorBadge.py --check
& Tools/DollCursorPreview/run.ps1
& Tools/DollControlPreview/garden.ps1
dotnet msbuild 伊蕾娜.csproj '-t:ResolveReferences;Compile' -p:BuildProjectReferences=false -v:quiet
```

`run.ps1` 支持 `-Tml` 与 `-VanillaItems` 指定 tML 安装、原版物品 PNG 目录。输出 `.vissandbox/doll-cursor/chest-cursor-badges.png` 和 `cursor-ui-1.0.png`、`cursor-ui-1.5.png`。FNA 直接链接正式绘制，读取正式头像及原版木箱、金箱、金钥匙、暗影钥匙图片；覆盖 1/1.5/2 倍图标、UI 150%、cursorScale 1.2 与额外提示偏移。图片已打开检查，是离线预览，不是游戏截图。

`ChestCursorNativeChecks.cs.txt` 的 14 条显示条件检查随 `garden.ps1` 执行：宝箱与两种钥匙、玩家更新/绘制的 tick 差、旧缓存、被替换提示、界面、地图、全忙、非宝箱、纯文字与死亡。和原有检查合计 392 条。完整游戏鼠标输入及第三方模组定制提示仍需实机验证。
