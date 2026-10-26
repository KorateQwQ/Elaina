using static 伊蕾娜.Localization.UIText;
using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.System;
using static 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel.ConstellationDrawing;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

public sealed partial class ConstellationSkillPanel
{
    private ConstellationLayoutEditor _layoutEditor;
    private Vector2 _nodeDragStart;
    private bool Editing => _layoutEditor?.IsEditing == true;
#if DEBUG
    internal static string LayoutFilePath
    {
        get
        {
            string source = ModContent.GetInstance<global::伊蕾娜.伊蕾娜>().SourceFolder;
            return string.IsNullOrWhiteSpace(source) || !Directory.Exists(source) || !File.Exists(Path.Combine(source, "build.txt"))
                ? null
                : Path.Combine(source, ConstellationLayout.AssetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
#endif

    private void LoadLayout()
    {
        var bundled = new ConstellationLayout();
        var authored = new ConstellationLayout();
        string error = null;
        var mod = ModContent.GetInstance<global::伊蕾娜.伊蕾娜>();
        try { bundled = ConstellationLayout.Parse(Encoding.UTF8.GetString(mod.GetFileBytes(ConstellationLayout.AssetPath))); }
        catch (Exception e) when (ConstellationLayout.IsFileError(e))
        {
            error = LocalizeUI("DefaultLayoutFailed");
            mod.Logger.Warn(error, e);
        }
#if DEBUG
        // Development edits the same tracked resource that will be bundled on the next build.
        // Published builds never read user configuration or the developer's source directory.
        try
        {
            if (LayoutFilePath is { } path) authored = ConstellationLayout.Read(path);
        }
        catch (Exception e) when (ConstellationLayout.IsFileError(e))
        {
            error = "源码布局读取失败，已使用模组内布局";
            mod.Logger.Warn(error, e);
        }
#endif
        _layoutEditor = new ConstellationLayoutEditor(_state, bundled, authored);
        if (error != null) Notify(error);
    }

    private void InitializeLayoutEditor()
    {
#if DEBUG
        Button(ToolButton("edit", false), () =>
        {
            if (Editing) { CancelLayoutEdit(); return; }
            if (LayoutFilePath == null)
            {
                Notify("未找到模组源码目录，无法保存发布布局");
                return;
            }
            StopDragging();
            _layoutEditor.Begin();
        }, h => DrawLayoutButton("edit", h), "调整技能图标的位置");
        Button(ToolButton("save", true), () =>
        {
            if (!Editing) return;
            StopDragging();
            string path = LayoutFilePath;
            if (path == null)
            {
                Notify("未找到模组源码目录，布局尚未保存");
                return;
            }
            if (_layoutEditor.Save(path, out string error)) Notify("布局已保存到项目；提交可同步远端，重新构建后随模组发布");
            else
            {
                Notify("保存失败，布局仍可编辑；请检查文件权限");
                ModContent.GetInstance<global::伊蕾娜.伊蕾娜>().Logger.Warn($"保存星图布局失败：{path}: {error}");
            }
        }, h => DrawLayoutButton("save", h), "保存到项目 Layout.json；重新构建后随模组发布", () => Editing);
        Button(ToolButton("cancel", true), CancelLayoutEdit,
            h => DrawLayoutButton("cancel", h), "放弃本次位置修改", () => Editing);
        Button(ToolButton("reset", true), () =>
        {
            if (!Editing) return;
            StopDragging();
            _layoutEditor.RestoreDefaults();
            Recenter();
            Notify("已恢复默认位置，点击保存后生效");
        }, h => DrawLayoutButton("reset", h), "恢复当前模组包内布局，可取消；保存后写入项目", () => Editing);
        Button(ToolButton("points", false), () =>
        {
            if (Editing || !_state.DebugGrantStudyPoints()) return;
            InvalidateDetail(false);
            Notify("研习点已增加，当前 " + _state.Points);
        }, h => DrawDebugButton("points", h), "获得100研习点", () => !Editing);
        Button(ToolButton("level", false), () =>
        {
            if (Editing) return;
            var playerState = Main.LocalPlayer.GetModPlayer<ElainaStatePlayer>();
            if (!playerState.DebugLevelUp())
            {
                Notify($"当前等级已达到上限 Lv. {playerState.GetMaxLevel()}");
                return;
            }
            InvalidateDetail(false);
            Notify($"角色等级提升至 Lv. {playerState.GetLevel()}");
        }, h => DrawDebugButton("level", h), "提升角色当前等级", () => !Editing);
        Button(ToolButton("max-level", false), () =>
        {
            if (Editing || !_state.IsCurrentPlayer) return;
            var playerState = Main.LocalPlayer.GetModPlayer<ElainaStatePlayer>();
            bool changed = playerState.DebugMaxLevel();
            InvalidateDetail(false);
            Notify(changed ? $"角色已升满级 · Lv. {playerState.GetLevel()}"
                : $"当前等级已达到上限 Lv. {playerState.GetMaxLevel()}");
        }, h => DrawDebugButton("max-level", h), "角色等级升至当前上限，经验归零，不推进 Boss 进度", () => !Editing);
        Button(ToolButton("cap", false), () =>
        {
            if (Editing || !_state.IsCurrentPlayer) return;
            var playerState = Main.LocalPlayer.GetModPlayer<ElainaStatePlayer>();
            int pointsBefore = _state.Points;
            if (!playerState.DebugAdvanceMaxLevel(out string bossName, out float bossState))
            {
                Notify($"角色 Progress {playerState.Progress:0.##} · 当前 Boss 列表中没有更高进度的 Boss");
                return;
            }
            InvalidateDetail(false);
            Notify($"角色进度推进至 {bossName} · 等级上限 Lv. {playerState.GetMaxLevel()} · 研习点 +{_state.Points - pointsBefore}");
        }, h => DrawDebugButton("cap", h), visible: () => !Editing,
            dynamicTooltip: () => Main.LocalPlayer.GetModPlayer<ElainaStatePlayer>().DebugGetNextBossTooltip());
        Button(ToolButton("master-skills", false), () =>
        {
            if (Editing) return;
            StopDragging();
            if (!_state.DebugMasterSkills(out int learned, out int levels)) return;
            _ancestors = _state.Ancestors();
            InvalidateDetail(true);
            Notify($"精通完成 · 角色 Lv. {Main.LocalPlayer.GetModPlayer<ElainaStatePlayer>().GetLevel()} · 新学 {learned} 个技能 · 技能共提升 {levels} 级");
        }, h => DrawDebugButton("master-skills", h),
            "先升至当前等级上限，再学会满足角色等级与等级上限要求的星图技能（含隐藏技能）并升至技能当前等级上限；无视 Boss、前置与其他研习条件，不消耗材料和研习点", () => !Editing);
        Button(ToolButton("reset-skills", false), () =>
        {
            if (Editing) return;
            StopDragging();
            if (!_state.DebugResetSkills()) return;
            _ancestors = _state.Ancestors();
            CenterOn(_state.Current);
            InvalidateDetail(true);
            Notify("技能与角色进度已重置 · 等级 1 · 经验 0");
        }, h => DrawDebugButton("reset-skills", h), "清除技能、装配及角色保存的 Progress／进度记录；角色等级回到 1、经验归零，不改变世界 Boss 击杀状态", () => !Editing);
#endif
    }

    private void CancelLayoutEdit()
    {
        if (!Editing) return;
        StopDragging();
        _layoutEditor.Cancel();
        ClampPan();
        Notify("已取消布局修改");
    }

    private void DrawLayoutButton(string kind, bool hover)
    {
#if DEBUG
        if (kind != "edit" && !Editing) return;
        if (kind == "edit")
        {
            // The tools are debug-only; a faint caption says so.
            _draw.Spark(new Vector2(529.5f, 744.5f), 7, Faint);
            _draw.Text("调试", 537, 739, 8.5f, Faint, spacing: 2);
        }
        string label = kind switch
        {
            "edit" => Editing ? "编辑中" : "编辑模式", "save" => "保存", "cancel" => "取消", _ => "恢复默认"
        };
        var area = ToolButton(kind, kind != "edit");
        _draw.GhostButton(area.X, area.Y + 2, area.Width, label, hover, kind == "edit" && Editing);
#endif
    }

    private void DrawDebugButton(string kind, bool hover)
    {
#if DEBUG
        if (Editing) return;
        string label = kind switch
        {
            "points" => "研习点", "level" => "升级", "max-level" => "升满级",
            "cap" => "上限+", "master-skills" => "精通技能", _ => "重置"
        };
        var area = ToolButton(kind, false);
        _draw.GhostButton(area.X, area.Y + 2, area.Width, label, hover);
#endif
    }

    private void DrawLayoutHints()
    {
#if DEBUG
        if (!Editing) return;
        _draw.Text(_layoutEditor.HasChanges ? "布局编辑 · 未保存" : "布局编辑", 36, 166, 13, Gold, serif: true);
        _draw.Text("拖动图标移动 · 空白处平移 · 滚轮缩放 · Shift 对齐 · Esc 取消", 36, 190, 10, Muted);
        if (_state.Current is { } node)
            _draw.FittedText($"{node.name}    X {node.x:0.##}   Y {node.y:0.##}", 224, 696, 500, 11, Gold);
#endif
    }
}
