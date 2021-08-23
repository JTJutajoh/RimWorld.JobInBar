using RimWorld;
using HarmonyLib;
using Verse;
using UnityEngine;

namespace JobInBar
{
    [HarmonyPatch(typeof(ColonistBarColonistDrawer))] // Type containing the method
    [HarmonyPatch("DrawColonist")] // Method to patch
    public class ColonistBarColonistDrawer_DrawColonist_Patch
    {
        public static void Postfix(Rect rect, Pawn colonist, Map pawnMap, bool highlight, bool reordering)
        {
            ColonistBar bar = Find.ColonistBar;
            float barHeight =  4f * bar.Scale; // from Core

            Vector2 pos = new Vector2(rect.center.x, rect.yMax - barHeight + Settings.JobLabelVerticalOffset);
            
            // Prevent broken game state if param is null somehow
            if (colonist == null)
            {
                Log.Error("(Job in bar) 'colonist' passed to ColonistBarColonistDrawer was null. This should never happen. This indicates something may be very wrong with a mod incompatibility. Skipping this pawn for job labels");
                return;
            }

            LabelDrawer.DrawLabels(colonist, pos, bar, rect, rect.width + bar.SpaceBetweenColonistsHorizontal);
        }
    }
}