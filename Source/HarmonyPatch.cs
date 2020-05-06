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
    [StaticConstructorOnStartup]
    public class Main
    {
        static Main()
        {
            var harmony = new Harmony("com.jtjutajoh.rimworld.mod.jobinbar");
            harmony.PatchAll(Assembly.GetExecutingAssembly());



            LogMessage("Job In Bar loaded.");
        }

        public static void LogMessage(string text)
        {
            Log.Message("[JobInBar] " + text);
        }
    }


    [HarmonyPatch(typeof(ColonistBarColonistDrawer))] // Type containing the method
    [HarmonyPatch("DrawColonist")] // Method to patch
    public class LabelPatch
    {
        Settings settings;
        
      
        public LabelPatch()
        {
            settings = LoadedModManager.GetMod<JobInBarMod>().GetSettings<Settings>();
        }


        public static void Postfix(
            ColonistBarColonistDrawer __instance,
            Dictionary<string, string> ___pawnLabelsCache,
            Rect rect, Pawn colonist, Map pawnMap, bool highlight, bool reordering
            )
        {

            ColonistBar bar = Find.ColonistBar;
            //float num3 = 0; // Should relate to scale
            float num3 =  4f * bar.Scale;

            //float verticalOffset = 10f;
            //float verticalOffset = -48f;
            float verticalOffset = Settings.JobLabelVerticalOffset;

            Vector2 pos = new Vector2(rect.center.x, rect.yMax - num3 + verticalOffset);


            //GenMapUI.DrawPawnLabel(colonist, pos, 1f, rect.width + ColonistBar.BaseSpaceBetweenColonistsHorizontal - 2f, ___pawnLabelsCache, GameFont.Tiny, true, true);;
            //Rect bgRect = new Rect(pos.x - )
            DrawJobLabel(pos, colonist, rect.width + bar.SpaceBetweenColonistsHorizontal);
        }

        public static void DrawJobLabel(
            Vector2 pos,
            Pawn colonist,
            float truncateToWidth = 9999f
            )
        {
            string jobLabel = GetJobLabel(colonist, truncateToWidth, GameFont.Tiny);


            float pawnLabelNameWidth = 60f;// GenMapUI.GetPawnLabelNameWidth(colonist, truncateToWidth, truncatedLabelsCache);


            //GenMapUI.GetPawnLabelNameWidth(colonist, truncateToWidth, truncatedLabelsCache);
            GameFont font2 = Text.Font;
            Text.Font = GameFont.Tiny;
            pawnLabelNameWidth = Text.CalcSize(jobLabel).x;
            Text.Font = font2;
            



            Rect bgRect = new Rect(pos.x - pawnLabelNameWidth / 2f - 4f, pos.y, pawnLabelNameWidth + 8f, 12f);
            Rect rect;
            if (true) // Originally if alignCenter
            {
                Text.Anchor = TextAnchor.UpperCenter;
                rect = new Rect(bgRect.center.x - pawnLabelNameWidth / 2f, bgRect.y - 2f, pawnLabelNameWidth, 100f);
            }

            GUI.DrawTexture(bgRect, TexUI.GrayTextBG);

            Color color = PawnNameColorUtility.PawnNameColorOf(colonist);
            color.a = color.a * 0.8f; // Lighten the job label so it doesn't overpower the name
            GUI.color = color;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect, jobLabel);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static string GetJobLabel(Pawn colonist, float truncateToWidth, GameFont font)
        {
            string jobLabel = "Job";
            jobLabel = colonist.story.TitleShortCap;

            GameFont font2 = Text.Font;
            Text.Font = font;
            jobLabel = jobLabel.Truncate(truncateToWidth);
            Text.Font = font2;

            /*if (colonist.story.title.Length > 0)
            {
                jobLabel = colonist.story.title;
            }
            else if (colonist.story.adulthood.title.Length > 0)
            {
                jobLabel = colonist.story.adulthood.title;
            }
            else
            {
                jobLabel = colonist.story.childhood.title;
            }*/

            /*Main.LogMessage(
                "Job story for " + colonist.story.TitleShortCap + " | title:" + colonist.story.title + " | childhood:" + colonist.story.childhood + " | adulthood:" + colonist.story.adulthood
                );

            */



            return jobLabel;
        }
    }

    public class Settings : ModSettings
    {
        public static float JobLabelVerticalOffset = 16f;
        public static float JobLabelAlpha = 0.8f;


        public override void ExposeData()
        {
            Scribe_Values.Look(ref JobLabelVerticalOffset, "JobLabelVerticalOffset", 16f);
            Scribe_Values.Look(ref JobLabelAlpha, "JobLabelAlpha", 0.8f);

            base.ExposeData();
        }
    }

    public class JobInBarMod : Mod
    {
        Settings settings;

        public JobInBarMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<Settings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();

            listingStandard.Begin(inRect);
            //////////////////////////////
            

            listingStandard.Label("Vertical offset: " + Settings.JobLabelVerticalOffset);
            Settings.JobLabelVerticalOffset = listingStandard.Slider(Settings.JobLabelVerticalOffset, -70f, 70f);
            listingStandard.Label("Text Alpha: " + Settings.JobLabelAlpha);
            Settings.JobLabelAlpha = listingStandard.Slider(Settings.JobLabelAlpha, 0f, 1f);

            //////////////////////
            listingStandard.End();

            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Job In Bar".Translate();
        }
    }
}