namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

// 编队 1/2/3 分别为白猫、黄狗、兔子；使用离线制作并打包的独立动作帧。
public sealed class SecondDollProjectile : DollPetProjectile
{
    public override int FormationSlot => 1;
    protected override string TexturePrefix => "伊蕾娜/ElainaModSkills/Skills/DollControl/NewDoll/Ready/WhiteCat";
    protected override int IdleFrameCount => 3;
    protected override int MoveFrameCount => 4;
    protected override int MineFrameCount => 4;
    protected override int UseFrameCount => 4;
    protected override int HammerFrameCount => 4;
}

public sealed class ThirdDollProjectile : DollPetProjectile
{
    public override int FormationSlot => 2;
    protected override string TexturePrefix => "伊蕾娜/ElainaModSkills/Skills/DollControl/NewDoll/Ready/YellowDog";
    protected override int IdleFrameCount => 3;
    protected override int MoveFrameCount => 4;
    protected override int MineFrameCount => 4;
    protected override int UseFrameCount => 4;
    protected override int HammerFrameCount => 4;
}

public sealed class FourthDollProjectile : DollPetProjectile
{
    public override int FormationSlot => 3;
    protected override string TexturePrefix => "伊蕾娜/ElainaModSkills/Skills/DollControl/NewDoll/Ready/Rabbit";
    protected override int IdleFrameCount => 3;
    protected override int MoveFrameCount => 4;
    protected override int MineFrameCount => 4;
    protected override int UseFrameCount => 4;
    protected override int HammerFrameCount => 4;
}
