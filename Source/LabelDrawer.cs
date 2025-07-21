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
    /// <param name="width"></param>
    /// <param name="labelColor">The color of the label to draw.</param>
    /// <param name="anchor"></param>
    /// <param name="drawBg"></param>
    /// <param name="extraHeight"></param>
    internal static void DrawCustomLabel(Vector2 pos, string labelToDraw, float width, Color labelColor,
        TextAnchor anchor, bool drawBg = true, float extraHeight = 0f)
    {
        if (drawBg)
        {
            var labelBgRect = new Rect(pos.x - (width / 2f), pos.y, width, 12f);
            GUI.DrawTexture(labelBgRect, TexUI.GrayTextBG!); //BUG: If Tiny font is disabled, the bg doesn't adjust
        }

        GUI.color = labelColor;
        Text.Anchor = anchor;
        var labelRect = new Rect(pos.x - width / 2f, pos.y - 2f, width, 16f + extraHeight);
        Widgets.Label(labelRect, labelToDraw);
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
    }

    internal static void DrawLabels(Pawn colonist, PawnCache? cache, Vector2 pos, ColonistBar bar, Rect rect)
    {
        if (Event.current!.type != EventType.Repaint) return;

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

        var lineOffset =
            new Vector2(0, Text.LineHeightOf(GameFont.Tiny) + Settings.ExtraOffsetPerLine); // 1.3+ only

        Text.Font = GameFont.Tiny;
        // Apply position offsets
        var rowPos = new Vector2(pos.x, pos.y);
        if (cache.DrawAnyPermanentLabels)
        {
            foreach (var label in cache.LabelOrder)
            {
                var labelDrawn = false;
                switch (label)
                {
                    case LabelType.JobTitle:
                        try
                        {
                            if (cache.GetJobLabel(out var jobLabel))
                            {
                                DrawCustomLabel(rowPos,
                                    jobLabel,
                                    cache.TitleLabelWidth,
                                    cache.JobColor,
                                    TextAnchor.MiddleCenter,
                                    drawBg: Settings.DrawJobTitleBackground);
                                labelDrawn = true;
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Exception(e, "Job label", true);
                        }

                        break;
                    case LabelType.RoyalTitle:
                        try
                        {
                            if (cache.GetRoyalTitle(out var royalTitle))
                            {
                                DrawCustomLabel(rowPos,
                                    royalTitle,
                                    cache.RoyaltyLabelWidth,
                                    cache.RoyalTitleColor,
                                    TextAnchor.MiddleCenter,
                                    drawBg: Settings.DrawRoyalTitleBackground);
                                labelDrawn = true;
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Exception(e, "Royalty label", true);
                        }

                        break;
                    case LabelType.IdeoRole:
                        try
                        {
                            if (cache.GetIdeoRole(out var ideoRole))
                            {
                                DrawCustomLabel(rowPos,
                                    ideoRole,
                                    cache.IdeoRoleLabelWidth,
                                    cache.IdeoRoleColor,
                                    TextAnchor.MiddleCenter,
                                    drawBg: Settings.DrawIdeoRoleBackground);
                                labelDrawn = true;
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Exception(e, "Ideology role label", true);
                        }

                        break;
                }

                if (labelDrawn)
                    rowPos += lineOffset;
            }
        }

        try
        {
            if (Settings.DrawCurrentTask && cache is { IsHovered: true, CurrentTask: not null })
            {
                if (Settings.CurrentTaskUseAbsolutePosition)
                    rowPos = new Vector2(pos.x, pos.y + Settings.CurrentTaskAbsoluteY + 36f);

#if !(v1_1 || v1_2 || v1_3) // RW 1.4 introduced the ShowWeaponsUnderPortraitMode pref
                else if (!Settings.MoveWeaponBelowCurrentTask)
                    if ((Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.Always ||
                         (Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.WhileDrafted &&
                          colonist.Drafted)) &&
                        (colonist.equipment?.Primary?.def?.IsWeapon ?? false))
                        rowPos.y += 28f + Settings.OffsetEquippedExtra;
#endif
                DrawCustomLabel(rowPos,
                    cache.CurrentTask,
                    cache.CurrentTaskLabelWidth,
                    cache.CurrentTaskColor,
                    TextAnchor.UpperCenter,
                    drawBg: Settings.DrawCurrentTaskBackground,
                    extraHeight: 48f);
                // rowPos += lineOffset;
            }
        }
        catch (Exception e)
        {
            Log.Exception(e, "Current job label", true);
        }

        Text.Font = GameFont.Small;
    }
}
