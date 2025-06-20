using System;
using JobInBar.Utils;
using RimWorld;
using UnityEngine;

namespace JobInBar;

public static class LabelDrawer
{
    public static Pawn? HoveredPawn;

    /// <summary>
    ///     Draws a custom label at the specified position with the specified text, color, and truncation options.
    /// </summary>
    /// <param name="pos">The position at which to draw the label.</param>
    /// <param name="labelToDraw">The text of the label to draw.</param>
    /// <param name="labelColor">The color of the label to draw.</param>
    /// <param name="truncateToWidth">The maximum width, in pixels, of the label after truncation.</param>
    /// <param name="truncate">A value indicating whether to truncate the label if it exceeds the specified width.</param>
    /// <param name="drawBg"></param>
    internal static void DrawCustomLabel(Vector2 pos, string labelToDraw, Color labelColor,
        float truncateToWidth = 9999f, bool truncate = true, bool drawBg = true)
    {
        // Save the current font and restore it after drawing the label
        Text.Font = GameFont.Tiny;

        if (truncate && Settings.TruncateLongLabels)
            labelToDraw = LabelUtils.TruncateLabel(labelToDraw, truncateToWidth, Text.Font);

        var pawnLabelNameWidth = Text.CalcSize(labelToDraw).x;

        var rect = LabelUtils.GetLabelRect(pos, pawnLabelNameWidth);
        var bgRect = LabelUtils.LabelBGRect(pos, pawnLabelNameWidth);

        if (drawBg)
            GUI.DrawTexture(bgRect, TexUI.GrayTextBG!);

        GUI.color = labelColor;
        Text.Font = GameFont.Tiny;
        Widgets.Label(rect, labelToDraw);

        // Reset the GUI color to white
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
    }

    public static void DrawLabels(Pawn colonist, Vector2 pos, ColonistBar bar, Rect rect,
        float truncateToWidth = 9999f)
    {
        HoveredPawn = null;
        var lineOffset =
            new Vector2(0, Text.LineHeightOf(GameFont.Tiny) + Settings.ExtraOffsetPerLine); // 1.3+ only

        // Apply position offsets
        pos = new Vector2(pos.x, pos.y);
        if (colonist.DrawAnyPermanentLabels(rect))
        {
            try
            {
                if (colonist.ShouldDrawJobLabel())
                {
                    DrawCustomLabel(pos, colonist.JobLabel(), colonist.JobLabelColor(), truncateToWidth,
                        drawBg: Settings.DrawJobTitleBackground);
                    pos += lineOffset;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "Job label", true);
            }

            try
            {
                if (colonist.ShouldDrawRoyaltyLabel())
                {
                    DrawCustomLabel(pos, colonist.RoyaltyLabel(), Settings.RoyalTitleColor, truncateToWidth,
                        drawBg: Settings.DrawRoyalTitleBackground);
                    pos += lineOffset;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "Royalty label", true);
            }

            try
            {
                if (colonist.ShouldDrawIdeoLabel())
                {
                    DrawCustomLabel(pos, colonist.IdeoLabel(), colonist.IdeoLabelColor(), truncateToWidth,
                        drawBg: Settings.DrawIdeoRoleBackground);
                    pos += lineOffset;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "Ideology role label", true);
            }
        }

        try
        {
            if (Settings.DrawCurrentTask && Mouse.IsOver(rect))
            {
#if !(v1_1 || v1_2 || v1_3) // RW 1.4 introduced the ShowWeaponsUnderPortraitMode pref
                if (!Settings.MoveWeaponBelowCurrentTask)
                    if ((Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.Always ||
                         (Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.WhileDrafted &&
                          colonist.Drafted)) &&
                        (colonist.equipment?.Primary?.def?.IsWeapon ?? false))
                        pos.y += 28f + Settings.OffsetEquippedExtra;
#endif
                HoveredPawn = colonist;
                DrawCustomLabel(pos, colonist.CurrentTaskDesc(), Settings.CurrentTaskLabelColor, truncate: false,
                    drawBg: Settings.DrawCurrentTaskBackground);
                // pos += lineOffset;
            }
        }
        catch (Exception e)
        {
            Log.Exception(e, "Current job label", true);
        }
    }
}
