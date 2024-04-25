using System;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Verse.AI;
using Verse;
using UnityEngine;
using HarmonyLib;
using DarkLog;
using System.Collections.Generic;

namespace JobInBar
{
    public struct LabelPreset
    {
        public string LabelText;
        public Color Color;
    }

    /// <summary>
    /// Struct storing all of the user-set data for one pawn's label.
    /// </summary>
    public struct LabelData : IExposable
    {
        public Pawn pawn;

        /// <summary>
        /// Main option to toggle whether ANY labels are shown for this pawn or not.
        /// If false, basically all the rest of the data is meaningless.
        /// </summary>
        public bool ShowAll = true;

        public bool ShowBackstory = true;
        public Color BackstoryColor = Color.white;

        public bool ShowRoyalTitle = true;

        public bool ShowIdeoRole = true;

        public List<LabelPreset> PresetLabels = new();

        public LabelData(Pawn pawn)
        {
            this.pawn = pawn;
            if (pawn == null)
                LogPrefixed.Error("Tried to initialize LabelData for null pawn.");
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");

            Scribe_Values.Look(ref ShowAll, "ShowAll");
            Scribe_Values.Look(ref ShowBackstory, "ShowBackstory");
            Scribe_Values.Look(ref BackstoryColor, "BackstoryColor");
            Scribe_Values.Look(ref ShowRoyalTitle, "ShowRoyalTitle");
            Scribe_Values.Look(ref ShowIdeoRole, "ShowIdeoRole");

            //TODO: Figure out how to serialize this, since the presets are NOT going to be stored in the save file
            // Scribe_Collections.Look(ref PresetLabels, "PresetLabels");
        }
    }

    public static class LabelUtils
    {
        //private static PawnLabelCustomColors_WorldComponent labelsComp;

        public static Color imperialColor = new Color(0.85f, 0.85f, 0.75f);

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
