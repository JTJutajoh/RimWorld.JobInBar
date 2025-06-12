using System;
using RimWorld;
using HarmonyLib;
using Verse;
using UnityEngine;
using DarkLog;

namespace JobInBar
{
    [HarmonyPatch(typeof(ColonistBarColonistDrawer))]
    [HarmonyPatch("DrawColonist")]
    public class ColonistBarColonistDrawer_DrawColonist_Patch
    {
        public static void Postfix(Rect rect, Pawn colonist, Map pawnMap, bool highlight, bool reordering)
        {
            var bar = Find.ColonistBar;
            var barHeight =  4f * bar.Scale; // from Core

            var pos = new Vector2(rect.center.x, rect.yMax - barHeight + Settings.JobLabelVerticalOffset);

            try
            {
                LabelDrawer.DrawLabels(colonist, pos, bar, rect, rect.width + bar.SpaceBetweenColonistsHorizontal);
            }
            catch (Exception e)
            {
                LogPrefixed.Exception(e, extraMessage: "Top-level uncaught exception", once: true);
            }
        }
    }
}