using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using static 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel.ConstellationDrawing;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

public sealed partial class ConstellationSkillPanel
{
    private ConstellationLayoutEditor _layoutEditor;
    private Vector2 _nodeDragStart;
    private bool Editing => _layoutEditor?.IsEditing == true;
    internal static string LayoutFilePath => Path.Combine(Main.SavePath, "ModConfigs", "Elaina", "ConstellationLayout.json");

    private void LoadLayout()
    {
        var bundled = new ConstellationLayout();
        var saved = new ConstellationLayout();
        string error = null;
        var mod = ModContent.GetInstance<global::伊蕾娜.伊蕾娜>();
        try { bundled = ConstellationLayout.Parse(Encoding.UTF8.GetString(mod.GetFileBytes(ConstellationLayout.AssetPath))); }
        catch (Exception e) when (ConstellationLayout.IsFileError(e))
        {
            error = "默认布局读取失败，已使用技能默认位置";
            mod.Logger.Warn(error, e);
        }
        try { saved = ConstellationLayout.Read(LayoutFilePath); }
        catch (Exception e) when (ConstellationLayout.IsFileError(e))
        {
            error = "本地布局读取失败，已使用默认布局";
            mod.Logger.Warn(error, e);
        }
        _layoutEditor = new ConstellationLayoutEditor(_state, bundled, saved);
        if (error != null) Notify(error);
    }

    private void InitializeLayoutEditor()
    {
#if DEBUG
        Button(new Rectangle(566, 741, 92, 40), () =>
        {
            if (Editing) { CancelLayoutEdit(); return; }
            StopDragging();
            _layoutEditor.Begin();
        }, h => DrawLayoutButton("edit", h), "调整技能图标的位置");
        Button(new Rectangle(667, 741, 70, 40), () =>
        {
            if (!Editing) return;
            StopDragging();
            if (_layoutEditor.Save(LayoutFilePath, out string error)) Notify("布局已保存，下次打开时自动应用");
            else
            {
                Notify("保存失败，布局仍可编辑；请检查文件权限");
                ModContent.GetInstance<global::伊蕾娜.伊蕾娜>().Logger.Warn($"保存星图布局失败：{LayoutFilePath}: {error}");
            }
        }, h => DrawLayoutButton("save", h), "保存到 " + LayoutFilePath, () => Editing);
        Button(new Rectangle(746, 741, 70, 40), CancelLayoutEdit,
            h => DrawLayoutButton("cancel", h), "放弃本次位置修改", () => Editing);
        Button(new Rectangle(825, 741, 116, 40), () =>
        {
            if (!Editing) return;
            StopDragging();
            _layoutEditor.RestoreDefaults();
            Recenter();
            Notify("已恢复默认位置，点击保存后生效");
        }, h => DrawLayoutButton("reset", h), "恢复模组默认布局，可取消；保存后才覆盖本地布局", () => Editing);
        Button(new Rectangle(667, 741, 174, 40), () =>
        {
            if (Editing || !_state.DebugGrantStudyPoints()) return;
            InvalidateDetail(false);
            Notify("研习点已增加，当前 " + _state.Points);
        }, h => DrawDebugButton("points", h), "获得100研习点", () => !Editing);
        Button(new Rectangle(850, 741, 91, 40), () =>
        {
            if (Editing) return;
            StopDragging();
            if (!_state.DebugResetSkills()) return;
            _ancestors = _state.Ancestors();
            CenterOn(_state.Current);
            InvalidateDetail(true);
            Notify("所有技能学习状态已重置");
        }, h => DrawDebugButton("reset-skills", h), "清除所有技能学习状态和装配，保留研习点及星图布局", () => !Editing);
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
        float x = kind switch { "edit" => 566, "save" => 667, "cancel" => 746, _ => 825 };
        float width = kind switch { "edit" => 92, "reset" => 116, _ => 70 };
        string label = kind switch
        {
            "edit" => Editing ? "编辑中" : "编辑模式", "save" => "保存", "cancel" => "取消", _ => "恢复默认"
        };
        Color color = kind == "save" || Editing && kind == "edit" ? Gold : Muted;
        _draw.Box(x, 746, width, 30, color * (hover ? .13f : .045f));
        _draw.Frame(x, 746, width, 30, color * (hover ? .7f : .32f));
        _draw.Text(label, x + width / 2, 755, 11, hover ? Ink : color, .5f);
#endif
    }

    private void DrawDebugButton(string kind, bool hover)
    {
#if DEBUG
        if (Editing) return;
        float x = kind == "points" ? 667 : 850, width = kind == "points" ? 174 : 91;
        Color color = kind == "points" ? Gold : new Color(214, 177, 155);
        _draw.Box(x, 746, width, 30, color * (hover ? .13f : .045f));
        _draw.Frame(x, 746, width, 30, color * (hover ? .7f : .32f));
        _draw.Text(kind == "points" ? "获得100研习点" : "重置", x + width / 2, 755, 11, hover ? Ink : color, .5f);
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
