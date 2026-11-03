# 火焰射线音效导出

旧版音效位于 `Projectiles/Flame`（该目录不打包）。运行：

```
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/FireLaserSounds/run.ps1
```

用 tModLoader 自带的 MP3Sharp 解码，生成 `ElainaModSkills/Skills/Fire/Sounds/`：

- `FireLaserCharge.wav`：`射线施法音效.mp3`，蓄力时播放一次；裁掉前导静音。
- `FireLaserIgnite.wav`：`火焰射线释放音效.mp3`，爆开时播放一次；裁掉前导静音，与爆开画面对齐。

射线持续期间与旧版一致，使用原版喷火器音效 `SoundID.Item34`，不需要导出。`持续施法.mp3` 旧版从未使用，且开头有冲击峰，循环时每圈都会“爆”一下，不要用作循环音。

加 `-Check` 只核对正式文件与生成结果一致，不写入。
