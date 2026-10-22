namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>玩偶指令盘的选项，顺序即轮盘上从十二点方向顺时针的排列。</summary>
public enum DollCommand
{
    Mine,
    MineChain,
    BreakWall,
    BreakWallChain,
    Attack,
    Cancel,
    Dismiss,
}

internal static class DollCommands
{
    public const int Count = 7;
    private const string IconPrefix = "伊蕾娜/ElainaModSkills/Skills/DollControl/Commands/DollCommand_";

    public static string IconPath(DollCommand command) => IconPrefix + command;

    /// <summary>选中后进入世界标记状态的指令；其余指令在确认时立即生效。</summary>
    public static bool IsMarkingTool(DollCommand command) => command <= DollCommand.BreakWallChain;

    public static bool TargetsWalls(DollCommand command) =>
        command is DollCommand.BreakWall or DollCommand.BreakWallChain;

    public static bool IsChain(DollCommand command) =>
        command is DollCommand.MineChain or DollCommand.BreakWallChain;
}
