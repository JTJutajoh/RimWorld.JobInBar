using System;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace JobInBar
{
    public class LabelDrawer
    {
        // Method used to draw all custom labels
        public static void DrawCustomLabel(Vector2 pos, string labelToDraw, Color labelColor, float truncateToWidth = 9999f)
        {
            //GameFont font2 = Text.Font;
            Text.Font = GameFont.Tiny;
            //Text.Font = font2;

            labelToDraw = JobInBarUtils.TruncateLabel(labelToDraw, truncateToWidth, Text.Font);

            float pawnLabelNameWidth = Text.CalcSize(labelToDraw).x;

            // calculate the sizes
            Rect rect = JobInBarUtils.GetLabelRect(pos, pawnLabelNameWidth);
            Rect bgRect = JobInBarUtils.GetLabelBGRect(pos, pawnLabelNameWidth);

            if (Settings.DrawBG)
                GUI.DrawTexture(bgRect, TexUI.GrayTextBG);

            // Override the label color with the global opacity mod setting
            labelColor.a = Settings.labelAlpha;

            GUI.color = labelColor;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect, labelToDraw);

            // Reset the gui drawing settings
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void DrawJobLabel(Vector2 pos, Pawn colonist, float truncateToWidth = 9999f)
        {
            string jobLabel = JobInBarUtils.GetJobLabel(colonist);

            DrawCustomLabel(pos, jobLabel, JobInBarUtils.GetJobLabelColorForPawn(colonist), truncateToWidth);
        }
        public static void DrawIdeoRoleLabel(Vector2 pos, Pawn colonist, float truncateToWidth = 9999f)
        {
            string roleLabel = JobInBarUtils.GetIdeoRoleLabel(colonist);

            DrawCustomLabel(pos, roleLabel, JobInBarUtils.GetIdeoLabelColorForPawn(colonist), truncateToWidth);
        }
        public static void DrawRoyalTitleLabel(Vector2 pos, Pawn colonist, float truncateToWidth = 9999f)
        {
            string titleLabel = JobInBarUtils.GetRoyalTitleLabel(colonist);

            Color imperialColor = new Color(0.85f, 0.85f, 0.75f);

            DrawCustomLabel(pos, titleLabel, imperialColor, truncateToWidth);
        }

        public static void DrawLabels(Pawn colonist, Vector2 pos, ColonistBar bar, Rect rect, float truncateToWidth = 9999f)
        {
            Vector2 lineOffset = new Vector2(0, Text.LineHeightOf(GameFont.Tiny) + Settings.ExtraOffsetPerLine); // 1.3 only
            // first check if any of the labels should be drawn at all (eg disabled in settings)
            if (JobInBarUtils.GetShouldDrawLabel(colonist))
            {
                if (JobInBarUtils.GetShouldDrawJobLabel(colonist))
                {
                    LabelDrawer.DrawJobLabel(pos, colonist, truncateToWidth);
                    pos += lineOffset;
                }

                if (JobInBarUtils.GetShouldDrawRoyalTitleLabel(colonist))
                {
                    LabelDrawer.DrawRoyalTitleLabel(pos, colonist, truncateToWidth);
                    pos += lineOffset;
                }

                if (JobInBarUtils.GetShouldDrawIdeoRoleLabel(colonist))
                {
                    LabelDrawer.DrawIdeoRoleLabel(pos, colonist, truncateToWidth);
                    pos += lineOffset;
                }
            }
        }
    }
}
