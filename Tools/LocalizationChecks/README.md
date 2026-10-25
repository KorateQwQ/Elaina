# UI 本地化检查

先运行 `& Tools/LocalizationChecks/parse.ps1`，使用 tModLoader 附带的 Hjson.dll 解析全部本地化文件，再运行 `python Tools/LocalizationChecks/check.py` 检查中英文 UI 键集合、格式参数、调用覆盖和英文值中的中文漏项。Python 读取器支持本项目的单行 UI 值及 tModLoader 自动整理后的嵌套分组，但不能代替完整 HJSON 语法校验。以 `{0}` 等保留符号开头的文案必须用双引号包裹。

英文绘制检查：

```powershell
& Tools/ConstellationSkillPanelPreview/run.ps1 -Culture en-US -LocalizationOnly
& Tools/AlchemyPreview/run.ps1 -Culture en-US
```

两者都读取实际本地化资源并链接正式绘制代码。`zh-Hans` 可检查中文回归。输出按语言分目录保存，覆盖 720p、常规尺寸及 125% UI 缩放。

这是 FNA 离线预览，角色、库存、部分输入控件和技能实例由夹具模拟；不能替代游戏内输入或角色存档测试。技能夹具未提供正式技能描述的全部格式参数时，会展示正式的描述缺失占位。

本次原有全量技能回归在 `current configured upgrade requirement is represented` 处失败：旧断言仍期待木材和点数，而当前配置仅需要点数。原有图标检查还直接读取已缺失的 `AshenWitchSkill_Gray.png`。`-LocalizationOnly` 明确跳过这些与文字无关的检查；不将本地化预览成功报告为全量玩法回归成功。
