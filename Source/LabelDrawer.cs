using System;
using DarkLog;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace JobInBar
{
    public static class LabelDrawer
    {
        /// <summary>
        /// Draws a custom label at the specified position with the specified text, color, and truncation options.
        /// </summary>
        /// <param name="pos">The position at which to draw the label.</param>
        /// <param name="labelToDraw">The text of the label to draw.</param>
        /// <param name="labelColor">The color of the label to draw.</param>
        /// <param name="truncateToWidth">The maximum width, in pixels, of the label after truncation.</param>
        /// <param name="truncate">A value indicating whether to truncate the label if it exceeds the specified width.</param>
        private static void DrawCustomLabel(Vector2 pos, string labelToDraw, Color labelColor, float truncateToWidth = 9999f, bool truncate = true)
        {
            // Save the current font and restore it after drawing the label
            //GameFont font2 = Text.Font;
            Text.Font = GameFont.Tiny;
            //Text.Font = font2;

            if (truncate)
                labelToDraw = LabelUtils.TruncateLabel(labelToDraw, truncateToWidth, Text.Font);

            var pawnLabelNameWidth = Text.CalcSize(labelToDraw).x;

            var rect = LabelUtils.GetLabelRect(pos, pawnLabelNameWidth);
            var bgRect = LabelUtils.LabelBGRect(pos, pawnLabelNameWidth);

            if (Settings.DrawBG)
                GUI.DrawTexture(bgRect, TexUI.GrayTextBG);

            // Override the label color with the global opacity mod setting
            labelColor.a = Settings.LabelAlpha;

            GUI.color = labelColor;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect, labelToDraw);

            // Reset the GUI color to white
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void DrawLabels(Pawn colonist, Vector2 pos, ColonistBar bar, Rect rect, float truncateToWidth = 9999f)
        {
            var lineOffset = new Vector2(0, Text.LineHeightOf(GameFont.Tiny) + Settings.ExtraOffsetPerLine); // 1.3+ only

            // Apply position offsets
            pos = new Vector2(pos.x, pos.y + colonist.LabelYOffset());
            if (colonist.DrawAnyPermanentLabels(rect))
            {
                try
                {
                    if (colonist.ShouldDrawJobLabel())
                    {
                        DrawCustomLabel(pos, colonist.JobLabel(), colonist.JobLabelColor(), truncateToWidth);
                        pos += lineOffset;
                    }
                }
                catch (Exception e)
                {
                    LogPrefixed.Exception(e, extraMessage: "Job label", once: true);
                }

                try
                {
                    if (colonist.ShouldDrawRoyaltyLabel())
                    {
                        DrawCustomLabel(pos, colonist.RoyaltyLabel(), LabelUtils.ImperialColor, truncateToWidth);
                        pos += lineOffset;
                    }
                }
                catch (Exception e)
                {
                    LogPrefixed.Exception(e, extraMessage: "Royalty label", once: true);
                }

                try
                {
                    if (colonist.ShouldDrawIdeoLabel())
                    {
                        DrawCustomLabel(pos, colonist.IdeoLabel(), colonist.IdeoLabelColor(), truncateToWidth);
                        pos += lineOffset;
                    }
                }
                catch (Exception e)
                {
                    LogPrefixed.Exception(e, extraMessage: "Ideology role label", once: true);
                }
            }

            try
            {
                if (Settings.DrawCurrentJob && Mouse.IsOver(rect))
                {
                    DrawCustomLabel(pos, colonist.CurrentTaskDesc(), Settings.CurrentJobLabelColor, truncate: false);
                    pos += lineOffset;
                }
            }
            catch (Exception e)
            {
                LogPrefixed.Exception(e, extraMessage: "Current job label", once: false);
            }
        }
    }
}
