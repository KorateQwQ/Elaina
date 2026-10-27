# 旧内容排除检查

`build.txt` 排除旧 `Projectiles`、`gnpc.cs`、旧冻结/灼烧 Buff、水花粒子、小动物药水，以及依赖它们的旧飞弹生成器和风刃黑洞。另排除 `Items/gItem.cs`、魔力共享戒指、白河豚项链、妮可的冒险谭及其贴图和魔力连接弹幕贴图。源码和原始素材保留供参考；`Directory.Build.targets` 同步排除 C#，避免 IDE 的预编译 DLL 仍包含旧类型。

新版冰技能继续使用 KL 的冻结事件，伊蕾娜帽子继续加载。旧雷云、父子关系、魔力共享和妮可名单的消息枚举编号保留，处理入口停用，不改变后续消息编号。对应的玩家状态、旧妮可标记读写、白河豚项链的保命和受伤免疫逻辑已移除。

此前为保留这些饰品而复制的 `Effects/*/LegacySupport` 资源已撤销，原始资源仍保存在已排除的 `Projectiles` 中。

被排除的旧物品类型不再注册；旧存档中的小动物药水及上述三件饰品不再作为可用物品加载。这次没有转换成其他物品，也没有直接修改玩家存档。

验证（仓库根目录）：

```powershell
dotnet build 伊蕾娜.csproj '-t:Compile;CopyFilesToOutputDirectory' -p:BuildProjectReferences=false -v:q
dotnet msbuild 伊蕾娜.csproj -getItem:Compile > .vissandbox/projectiles-exclusion-check/compile-items.json
python Tools/LegacyContentChecks/check.py '<隔离打包的伊蕾娜.tmod>' .vissandbox/projectiles-exclusion-check/compile-items.json
```

检查器核对两条编译路径的排除配置、正式代码中的旧目录/状态引用、包内容与当前 DLL，以及转换为 rawimg 后的旧贴图排除情况。实际打包可参照 `Tools/FixedTextureBake/package.ps1` 的隔离保存目录方式；不要用旧 DLL 代替本次编译结果。编译和包检查不能代替游戏内加载与联机测试。
