namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>熊玩偶：站立三帧，移动四帧，挖掘指令标记时挖矿四帧；不攻击。</summary>
public sealed class KumaPetProjectile : DollPetProjectile
{
    protected override string TexturePrefix => "伊蕾娜/ElainaModSkills/Skills/DollControl/Kuma/Kuma";
    protected override int IdleFrameCount => 3;
    protected override int MoveFrameCount => 4;
    protected override int MineFrameCount => 4;
}
