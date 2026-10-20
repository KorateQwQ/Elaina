namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>熊玩偶：站立一帧，移动四帧；当前只跟随玩家，不攻击。</summary>
public sealed class KumaPetProjectile : DollPetProjectile
{
    protected override string TexturePrefix => "伊蕾娜/ElainaModSkills/Skills/DollControl/Kuma/Kuma";
    protected override int MoveFrameCount => 4;
}
