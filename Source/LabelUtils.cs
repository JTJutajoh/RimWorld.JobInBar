using System;
using System.Linq;
using Verse;
using UnityEngine;
using HarmonyLib;
using DarkLog;
using System.Collections.Generic;

namespace JobInBar
{
    public static class LabelUtils
    {
        // Color is copied directly from vanilla
        public static readonly Color ImperialColor = new Color(0.85f, 0.85f, 0.75f);

        public static Rect LabelBGRect(Vector2 pos, float labelWidth) => new Rect(pos.x - labelWidth / 2f - 4f, pos.y, labelWidth + 8f, 12f);

        public static Rect GetLabelRect(Vector2 pos, float labelWidth)
        {
            var bgRect = LabelBGRect(pos, labelWidth);

            Text.Anchor = TextAnchor.UpperCenter;
            var rect = new Rect(bgRect.center.x - labelWidth / 2f, bgRect.y - 2f, labelWidth, 100f);

            return rect;
        }

        public static string TruncateLabel(string labelString, float truncateToWidth, GameFont font)
        {
            var oldFont = Text.Font;
            Text.Font = font;
            labelString = labelString.Truncate(truncateToWidth);
            Text.Font = oldFont;

            return labelString;
        }
    }
}
