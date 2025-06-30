using System;
using RimWorld;
using UnityEngine;

namespace JobInBar;

internal static class LabelDrawer
{
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
            GUI.DrawTexture(bgRect, TexUI.GrayTextBG!); //BUG: If Tiny font is disabled, the bg doesn't adjust

        GUI.color = labelColor;
        Text.Font = GameFont.Tiny; //TODO: Dynamically calculate font size instead of just hard coding the rect sizes
        Widgets.Label(rect, labelToDraw);

        // Reset the GUI color to white
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
    }

    internal static void DrawLabels(Pawn colonist, PawnCache? cache, Vector2 pos, ColonistBar bar, Rect rect,
        float truncateToWidth = 9999f)
    {
        if (cache is null)
        {
            cache = PawnCache.GetOrCache(colonist);
            if (cache is null)
            {
                Log.ErrorOnce($"Error caching pawn {(colonist?.Name?.ToString() ?? "null")}",
                    (colonist?.GetHashCode().ToString() ?? "null pawn"));
                throw new ArgumentNullException(nameof(cache));
            }
        }

        cache.IsHovered = Mouse.IsOver(rect);

        if (cache is { OnlyDrawWhenHovered: true, IsHovered: false })
            return;

        var lineOffset =
            new Vector2(0, Text.LineHeightOf(GameFont.Tiny) + Settings.ExtraOffsetPerLine); // 1.3+ only

        // Apply position offsets
        pos = new Vector2(pos.x, pos.y);
        if (cache.DrawAnyPermanentLabels)
        {
            //TODO: Come up with a way of letting the user customize label order
            try
            {
                if (cache.GetJobLabel(out var jobLabel))
                {
                    DrawCustomLabel(pos, jobLabel, colonist.JobLabelColor(), truncateToWidth,
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
                if (cache.GetRoyalTitle(out var royalTitle))
                {
                    DrawCustomLabel(pos, royalTitle, colonist.RoyalTitleColor(), truncateToWidth,
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
                if (cache.GetIdeoRole(out var ideoRole))
                {
                    DrawCustomLabel(pos, ideoRole, colonist.IdeoLabelColor(), truncateToWidth,
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
            if (Settings.DrawCurrentTask && cache is { IsHovered: true, CurrentTask: not null })
            {
#if !(v1_1 || v1_2 || v1_3) // RW 1.4 introduced the ShowWeaponsUnderPortraitMode pref
                if (!Settings.MoveWeaponBelowCurrentTask)
                    if ((Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.Always ||
                         (Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.WhileDrafted &&
                          colonist.Drafted)) &&
                        (colonist.equipment?.Primary?.def?.IsWeapon ?? false))
                        pos.y += 28f + Settings.OffsetEquippedExtra;
#endif
                DrawCustomLabel(pos, cache.CurrentTask, Settings.CurrentTaskLabelColor, truncate: false,
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
