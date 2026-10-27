# 角色资源槽偏好与技能栏检查

在模组根目录运行：

```powershell
./Tools/ResourceDisplayChecks/run.ps1
```

脚本仅编译 DLL，并使用真实 tModLoader 资源槽切换 API、角色 SaveData/LoadData 和正式技能栏的 Enabled/IsInteractable 属性离线检查，不启动或重载游戏。

覆盖首次入场默认选择、手动切换与保存恢复、退出已进入菜单时的最终选择、跨角色隔离、旧存档兼容、远端玩家隔离，以及非伊蕾娜/菜单/非活动角色的技能栏禁用。资源槽绘制使用无贴图替身，不验证画面；角色入场调用资源槽恢复入口，不执行标题修改和联网 RPC。

新角色默认值由 `ElainaModplayer.AddStartingItems` 在 `mediumCoreDeath == false` 时设置；中核死亡补发物品不会重置这一偏好。
