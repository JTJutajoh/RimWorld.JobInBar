using System;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Verse.AI;
using UnityEngine;
using HarmonyLib;

namespace JobInBar
{
    public static class LabelUtils
    {
        //private static PawnLabelCustomColors_WorldComponent labelsComp;

        public static Rect GetLabelBGRect(Vector2 pos, float labelWidth) => new Rect(pos.x - labelWidth / 2f - 4f, pos.y, labelWidth + 8f, 12f);

        public static Rect GetLabelRect(Vector2 pos, float labelWidth)
        {
            Rect bgRect = GetLabelBGRect(pos, labelWidth);

            Text.Anchor = TextAnchor.UpperCenter;
            Rect rect = new Rect(bgRect.center.x - labelWidth / 2f, bgRect.y - 2f, labelWidth, 100f);

            return rect;
        }

        public static string TruncateLabel(string labelString, float truncateToWidth, GameFont font)
        {
            GameFont oldFont = Text.Font;
            Text.Font = font;
            labelString = labelString.Truncate(truncateToWidth);
            Text.Font = oldFont; // reset font

            return labelString;
        }
    }
}
