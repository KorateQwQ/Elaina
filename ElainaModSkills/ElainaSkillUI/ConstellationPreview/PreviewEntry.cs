using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationPreview;

public sealed class ConstellationPreviewCommand : ModCommand
{
    public override string Command => "elainapreview";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/elainapreview";
    public override string Description => "打开 / 关闭独立技能星图视觉预览";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (!ConstellationPreviewUI.TogglePreview()) caller.Reply("请进入世界后打开星图预览。");
    }
}
