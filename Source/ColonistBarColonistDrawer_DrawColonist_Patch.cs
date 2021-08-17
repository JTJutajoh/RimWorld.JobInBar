using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System;





namespace JobInBar
{
    /*[StaticConstructorOnStartup]
    public class Main
    {
        static Main()
        {
            var harmony = new Harmony("Dark.JobInBar");
            harmony.PatchAll(Assembly.GetExecutingAssembly());



            LogMessage("Job In Bar loaded.");
        }

        public static void LogMessage(string text)
        {
            Log.Message("[JobInBar] " + text);
        }
    }*/


    [HarmonyPatch(typeof(ColonistBarColonistDrawer))] // Type containing the method
    [HarmonyPatch("DrawColonist")] // Method to patch
    public class ColonistBarColonistDrawer_DrawColonist_Patch
    {
        public static void Postfix(Rect rect, Pawn colonist, bool highlight)
        {

            ColonistBar bar = Find.ColonistBar;
            float num3 =  4f * bar.Scale;

            float verticalOffset = Settings.JobLabelVerticalOffset;

            Vector2 pos = new Vector2(rect.center.x, rect.yMax - num3 + verticalOffset);
            
            // Prevent broken game state if param is null somehow
            if (colonist == null)
            {
                Log.Message("(Job in bar) 'colonist' passed to ColonistBarColonistDrawer was null. This should never happen. This indicates something may be very wrong with a mod incompatibility. Skipping this pawn for job labels");
                return;
            }

            DrawLabels(colonist, pos, bar, rect, rect.width + bar.SpaceBetweenColonistsHorizontal);
        }

        public static void DrawLabels(Pawn colonist, Vector2 pos, ColonistBar bar, Rect rect, float truncateToWidth=9999f)
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