namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>玩偶指令与选择模式；连锁模式由操作条切换，不单独占用指令盘。</summary>
public enum DollCommand
{
    Mine,
    MineChain,
    BreakWall,
    BreakWallChain,
    // 保留既有指令编号；原编号 4 的攻击占位与编号 7 的放置种植盆已移除（改由远程放置物块完成）。
    Cancel = 5,
    Dismiss,
}

internal static class DollCommands
{
    public const int Count = 4;
    private static readonly DollCommand[] WheelCommands =
        [DollCommand.Mine, DollCommand.BreakWall, DollCommand.Cancel, DollCommand.Dismiss];

    public static DollCommand AtSlot(int slot) => WheelCommands[slot];
    public static DollCommand BaseCommand(DollCommand command) => command switch
    {
        DollCommand.MineChain => DollCommand.Mine,
        DollCommand.BreakWallChain => DollCommand.BreakWall,
        _ => command
    };
    private const string IconPrefix = "伊蕾娜/ElainaModSkills/Skills/DollControl/Commands/DollCommand_";

    public static string IconPath(DollCommand command) => IconPrefix + command;

    /// <summary>选中后进入世界标记状态的指令；其余指令在确认时立即生效。</summary>
    public static bool IsMarkingTool(DollCommand command) => command <= DollCommand.BreakWallChain;

    public static bool TargetsWalls(DollCommand command) =>
        command is DollCommand.BreakWall or DollCommand.BreakWallChain;

    public static bool IsChain(DollCommand command) =>
        command is DollCommand.MineChain or DollCommand.BreakWallChain;
}
