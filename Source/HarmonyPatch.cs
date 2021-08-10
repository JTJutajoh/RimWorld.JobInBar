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
            var harmony = new Harmony("Dark.JobInBar");
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
            Vector2 lineOffset = new Vector2(0, Text.LineHeightOf(GameFont.Tiny) + Settings.ExtraOffsetPerLine); // 1.3 only
            //Vector2 lineOffset = new Vector2(0, Text.CalcHeight("label", 1000) + Settings.ExtraOffsetPerLine); // 1.2

            //GenMapUI.DrawPawnLabel(colonist, pos, 1f, rect.width + ColonistBar.BaseSpaceBetweenColonistsHorizontal - 2f, ___pawnLabelsCache, GameFont.Tiny, true, true);;
            //Rect bgRect = new Rect(pos.x - )


            // Prevent broken game state if param is null somehow
            if (colonist == null)
            {
                Main.LogMessage("'colonist' passed to ColonistBarColonistDrawer was null. This should never happen. This indicates something may be very wrong with a mod incompatibility. Skipping this pawn for job labels");
                return;
            }

            // first check if any of the labels should be drawn at all (eg disabled in settings)
            if (GetShouldDrawLabel(colonist))
            {
                if (Settings.DrawJob)
                {
                    DrawJobLabel(pos, colonist, rect.width + bar.SpaceBetweenColonistsHorizontal);
                    pos += lineOffset;
                }

                if (GetShouldDrawRoyalTitleLabel(colonist))
                {
                    DrawRoyalTitleLabel(pos, colonist, rect.width + bar.SpaceBetweenColonistsHorizontal);
                    pos += lineOffset;
                }

                if (GetShouldDrawIdeoRoleLabel(colonist))
                {
                    DrawIdeoRoleLabel(pos, colonist, rect.width + bar.SpaceBetweenColonistsHorizontal);
                    pos += lineOffset;
                }
            }
        }

        public static bool GetShouldDrawLabel(Pawn colonist)
        {
            if (Settings.ModEnabled == false)
            {
                return false;
            }

            if ((colonist.Drafted && Settings.HideWhenDrafted))
            {
                return false;
            }
            
            return true;
        }

        public static bool GetShouldDrawIdeoRoleLabel(Pawn colonist)
        {
            if (Settings.DrawIdeoRoles == false)
                return false;

            // check if the pawn HAS an ideology. (Some mods set the ideo to null)
            if (colonist.ideo == null)
            {
                return false;
            }
            if (colonist.ideo.Ideo == null)
            {
                return false;
            }

            // Skip if the pawn has no role
            if (colonist.ideo.Ideo.GetRole(colonist) == null)
                return false;
            
            return true;
        }

        public static bool GetShouldDrawRoyalTitleLabel(Pawn colonist)
        {
            if (Settings.DrawRoyalTitles == false)
                return false;

            // check if the pawn's royalty field is null (in case some mod author decided it was a good idea to null it)
            if (colonist.royalty == null)
            {
                return false;
            }

            // skip if the pawn has no title
            if (colonist.royalty.MainTitle() == null)
                return false;

            return true;
        }

        public static Rect GetLabelBGRect(Vector2 pos, float labelWidth)
        {
            Rect bgRect = new Rect(pos.x - labelWidth / 2f - 4f, pos.y, labelWidth + 8f, 12f);

            return bgRect;
        }

        public static Rect GetLabelRect(Vector2 pos, float labelWidth)
        {
            Rect bgRect = GetLabelBGRect(pos, labelWidth);
            
            Text.Anchor = TextAnchor.UpperCenter;
            Rect rect = new Rect(bgRect.center.x - labelWidth / 2f, bgRect.y - 2f, labelWidth, 100f);

            return rect;
        }

        // Method used to draw all custom labels
        public static void DrawCustomLabel(Vector2 pos, string labelToDraw, Color labelColor)
        {
            //GameFont font2 = Text.Font;
            Text.Font = GameFont.Tiny;
            //Text.Font = font2;

            float pawnLabelNameWidth = Text.CalcSize(labelToDraw).x;

            // calculate the sizes
            Rect rect = GetLabelRect(pos, pawnLabelNameWidth);
            Rect bgRect = GetLabelBGRect(pos, pawnLabelNameWidth);

            if (Settings.DrawBG)
                GUI.DrawTexture(bgRect, TexUI.GrayTextBG);

            // Override the label color with the global opacity mod setting
            labelColor.a = Settings.JobLabelAlpha;

            GUI.color = labelColor;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect, labelToDraw);

            // Reset the gui drawing settings
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void DrawJobLabel(Vector2 pos, Pawn colonist, float truncateToWidth = 9999f)
        {
            string jobLabel = GetJobLabel(colonist, truncateToWidth, GameFont.Tiny);

            DrawCustomLabel(pos, jobLabel, GetLabelColorForPawn(colonist));
        }

        public static void DrawIdeoRoleLabel(Vector2 pos, Pawn colonist, float truncateToWidth = 9999f)
        {
            string roleLabel = GetIdeoRoleLabel(colonist, truncateToWidth, GameFont.Tiny);

            DrawCustomLabel(pos, roleLabel, GetIdeoLabelColorForPawn(colonist));
        }

        public static void DrawRoyalTitleLabel(Vector2 pos, Pawn colonist, float truncateToWidth = 9999f)
        {
            string titleLabel = GetRoyalTitleLabel(colonist, truncateToWidth, GameFont.Tiny);

            Color imperialColor = new Color(0.85f, 0.85f, 0.75f);

            DrawCustomLabel(pos, titleLabel, imperialColor);
        }

        // Fetches the label color setting for the job label (And others in certain situations)
        public static Color GetLabelColorForPawn(Pawn pawn)
        {
            Color LabelColor = new Color(Settings.color_r, Settings.color_g, Settings.color_b);

            //pawn.thingIDNumber;
            // TODO add individual pawn label colors
            // Compare the pawn's id to a dictionary of ids with colors and return that color or a default.

            return LabelColor;
        }

        // Looks up the pawn's ideology and returns the rgb color associated with that ideology, adjusting it for readability
        public static Color GetIdeoLabelColorForPawn(Pawn pawn)
        {
            Color LabelColor = new Color(1f, 1f, 1f);

            if (Settings.UseIdeoColorForRole)
            {
                if (pawn.ideo == null)
                {
                    return LabelColor;
                }
                if (pawn.ideo.Ideo == null)
                {
                    return LabelColor;
                }
                LabelColor = pawn.ideo.Ideo.colorDef.color;

                // Brighten ideo colors so dark ones are readable
                //// Magic number: 0.35f for the lerp to white, to brighten every ideo color by a set amount. 
                ////tested with black to assure minimum readability
                ////TODO make this magic number a setting? Adjust the Lerp value based on the gamma of the original color so that light colors are unchanged?
                LabelColor = Color.Lerp(LabelColor, Color.white, 0.35f);

                return LabelColor;
            }

            // Fall back to using the same method we use for the job label (rgb set in settings)
            LabelColor = GetLabelColorForPawn(pawn);
            return LabelColor;
        }


        public static string TruncateLabel(string labelString, float truncateToWidth, GameFont font)
        {
            GameFont font2 = Text.Font;
            Text.Font = font;
            if (Settings.TruncateJobs)
                labelString = labelString.Truncate(truncateToWidth);
            Text.Font = font2; // reset font

            return labelString;
        }

        public static string GetJobLabel(Pawn colonist, float truncateToWidth, GameFont font)
        {
            string jobLabel = "Job";
            jobLabel = colonist.story.TitleShortCap;

            TruncateLabel(jobLabel, truncateToWidth, font);
            
            return jobLabel;
        }

        public static string GetIdeoRoleLabel(Pawn colonist, float truncateToWidth, GameFont font)
        {
            Precept_Role myRole = colonist.ideo.Ideo.GetRole(colonist);
            string roleLabel = "";

            if (myRole != null)
            {
                roleLabel = myRole.LabelForPawn(colonist);
                TruncateLabel(roleLabel, truncateToWidth, font);
            }

            return roleLabel;
        }

        public static string GetRoyalTitleLabel(Pawn colonist, float truncateToWidth, GameFont font)
        {
            RoyalTitleDef myTitle = colonist.royalty.MainTitle();
            string titleLabel = "";

            if (myTitle != null)
            {
                titleLabel = myTitle.GetLabelCapFor(colonist);
                TruncateLabel(titleLabel, truncateToWidth, font);
            }

            return titleLabel;
        }
    }



    public class Settings : ModSettings
    {
        public static int JobLabelVerticalOffset = 14;
        public static int ExtraOffsetPerLine = 4;
        public static float JobLabelAlpha = 0.8f;
        public static bool ModEnabled = true;
        public static bool DrawBG = true;
        public static bool TruncateJobs = true;
        public static bool HideWhenDrafted = false;
        public static bool DrawJob = true;
        public static bool DrawIdeoRoles = true;
        public static bool UseIdeoColorForRole = true;
        public static bool DrawRoyalTitles = true;

        // Color
        public static float color_r = 0.9f;
        public static float color_g = 0.9f;
        public static float color_b = 0.9f;


        public override void ExposeData()
        {
            Scribe_Values.Look(ref JobLabelVerticalOffset, "JobLabelVerticalOffset", 14);
            Scribe_Values.Look(ref ExtraOffsetPerLine, "ExtraOffsetPerLine", 4);
            Scribe_Values.Look(ref DrawBG, "DrawBG", true);
            Scribe_Values.Look(ref TruncateJobs, "TruncateJobs", true);
            Scribe_Values.Look(ref HideWhenDrafted, "HideWhenDrafted", true);
            Scribe_Values.Look(ref ModEnabled, "ModEnabled", true);
            Scribe_Values.Look(ref DrawJob, "DrawJob", true);
            Scribe_Values.Look(ref DrawIdeoRoles, "DrawIdeoRoles", true);
            Scribe_Values.Look(ref UseIdeoColorForRole, "UseIdeoColorForRole", true);
            Scribe_Values.Look(ref DrawRoyalTitles, "DrawRoyalTitles", true);
            Scribe_Values.Look(ref color_r, "color_r", 0.9f);
            Scribe_Values.Look(ref color_g, "color_g", 0.9f);
            Scribe_Values.Look(ref color_b, "color_b", 0.9f);
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
            /// listing
            //////////////////////////////
            /// begin left column
            listingStandard.ColumnWidth = inRect.width / 2.4f;

            listingStandard.CheckboxLabeled("Enabled:", ref Settings.ModEnabled, "Use this to disable the mod entirely if things are behaving strangely.");

            listingStandard.Gap();
            listingStandard.GapLine();

            listingStandard.Label("Vertical offset: " + Settings.JobLabelVerticalOffset);
            Settings.JobLabelVerticalOffset = (int)listingStandard.Slider(Settings.JobLabelVerticalOffset, -70f, 70f);
            listingStandard.Label("Vertical offset between labels: " + Settings.ExtraOffsetPerLine);
            Settings.ExtraOffsetPerLine = (int)listingStandard.Slider(Settings.ExtraOffsetPerLine, -16f, 16f);
            listingStandard.CheckboxLabeled("Draw Background:", ref Settings.DrawBG, "Draw a black box behind the job title, just like the name label.");
            listingStandard.CheckboxLabeled("Truncate long job titles:", ref Settings.TruncateJobs, "If turned off, long job titles will overflow. Looks ugly but might be the behavior you want.");
            listingStandard.CheckboxLabeled("Hide job titles when drafted:", ref Settings.HideWhenDrafted, "Hides the job title label when the colonist is drafted. Helpful when using other colonist bar mods like Show Draftees Weapon.");
            listingStandard.Gap();
            listingStandard.CheckboxLabeled("Draw job titles:", ref Settings.DrawJob, "Draws a colonist's current job. The main function of the mod.");
            listingStandard.CheckboxLabeled("Draw Royalty titles (If available):", ref Settings.DrawRoyalTitles, "If Royalty is installed, draws the royal titles owned by a pawn below the job label."); 
            listingStandard.CheckboxLabeled("Draw Ideology specialist roles (If available):", ref Settings.DrawIdeoRoles, "If Ideology is installed, draws the specialist roles in the color of their ideology below the job label.");
            listingStandard.Indent(8);
            listingStandard.CheckboxLabeled("Use Ideology color for roles:", ref Settings.UseIdeoColorForRole, "Turn this off if the ideology color makes it hard to read the specialist role color or you find it distracting.");
            /// end left column
            //////////////////////////////
            /// spacer
            listingStandard.NewColumn();
            listingStandard.ColumnWidth = inRect.width / 4.2f;
            /// end spacer
            //////////////////////////////
            /// begin right column
            listingStandard.NewColumn();
            listingStandard.ColumnWidth = inRect.width / 3.5f;

            listingStandard.Gap();
            listingStandard.Gap();
            listingStandard.Label("Label Color");
            Settings.color_r = listingStandard.Slider(Settings.color_r, 0f, 1f);
            Settings.color_g = listingStandard.Slider(Settings.color_g, 0f, 1f);
            Settings.color_b = listingStandard.Slider(Settings.color_b, 0f, 1f);
            listingStandard.Label("Opacity: " + Settings.JobLabelAlpha);
            Settings.JobLabelAlpha = listingStandard.Slider(Settings.JobLabelAlpha, 0f, 1f);

            listingStandard.End();
            /// end right column
            //////////////////////////////
            /// end listing
            //////////////////////////////

            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Job In Bar";
        }
    }

}