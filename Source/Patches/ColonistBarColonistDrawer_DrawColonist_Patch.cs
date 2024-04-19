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
            ColonistBar bar = Find.ColonistBar;
            float barHeight =  4f * bar.Scale; // from Core

            Vector2 pos = new Vector2(rect.center.x, rect.yMax - barHeight + Settings.JobLabelVerticalOffset);
            
            if (colonist == null)
            {
                LogPrefixed.ErrorOnce("colonist passed to ColonistBarColonistDrawer_DrawColonist_Patch was null. This should never happen. This indicates something may be very wrong with a mod incompatibility. Skipping this pawn for job labels. Ignoring this error is not recommended.", 23498748);
                return;
            }

            LabelDrawer.DrawLabels(colonist, pos, bar, rect, rect.width + bar.SpaceBetweenColonistsHorizontal);
        }
    }
}