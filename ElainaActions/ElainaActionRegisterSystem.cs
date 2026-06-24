using KL.ActionsSystem;

namespace 伊蕾娜.ElainaActions;

/// <summary>
/// 注册伊蕾娜模组中的动作类型。
/// </summary>
public class ElainaActionRegisterSystem : ModSystem
{
    /// <summary>
    /// 加载模组时将当前模组的动作类型注册到 KL 动作系统。
    /// </summary>
    public override void Load()
    {
        AnimActionRegistry.RegisterFromMod(Mod);
    }
}