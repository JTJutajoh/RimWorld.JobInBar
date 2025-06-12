using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;
using DarkLog;

namespace JobInBar
{
    class Settings : Verse.ModSettings
    {
        public static bool ModEnabled = true;

        public static int JobLabelVerticalOffset = 14;
        public static int ExtraOffsetPerLine = -4;

        public static bool DrawBG = true;
        
        public static bool DrawLabelOnlyOnHover = false;

        public static bool DrawCurrentJob = true;

        public static bool DrawIdeoRoles = true;
        public static bool UseIdeoColorForRole = true;
        public static bool RoleColorOnlyIfAbilityAvailable = false;

        public static bool DrawRoyalTitles = true;

        // Color
        //TODO: Create some way to modify this list in settings.
        public static List<Color> AllColors = new List<Color>()
        {
            Color.white,
            Color.gray,
            Color.black,
            Color.blue,
            Color.cyan,
            Color.green,
            Color.yellow,
            Color.red,
            Color.magenta,
            new Color(0.5f,0f,0f),
            new Color(0f,0.5f,0f),
            new Color(0f,0f,0.5f),
            new Color(0.8f,0.35f,0f),
            new Color(0.2f,0.7f,0.7f),
            new Color(0.1f,0.3f,0.9f),
            new Color(0.8f,0.8f,0.2f),
            new Color(0.5f,0.9f,0.4f),
            new Color(0.8f,0.7f,0f),
            new Color(0.7f,0.95f,0.4f),
            new Color(0.5f,0.85f,0.7f),
            new Color(0.1f,0.3f,0.15f),
            new Color(0f,0.2f,0.9f),
            new Color(0.7f,0.5f,0.6f),
            new Color(0.75f,0.75f,0.1f),
        };
        public static Color DefaultJobLabelColor = GenMapUI.DefaultThingLabelColor.ClampToValueRange(new FloatRange(0f,0.7f));
        public static Color CurrentJobLabelColor = new Color(1f, 0.8f, 0.4f, 0.8f);
        public static float LabelAlpha = 0.8f;

        private static void DoIndent(Listing_Standard listing, float amount = 12f)
        {
            listing.ColumnWidth -= amount;
            listing.Indent(amount);
        }
        private static void DoOutdent(Listing_Standard listing, float amount = 12f)
        {
            listing.ColumnWidth += amount;
            listing.Outdent(amount);
        }

        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();

            listing.Begin(inRect);
            
            //////////////////////////////
            // begin left column
            listing.ColumnWidth = inRect.width / 2.2f;

            listing.CheckboxLabeled("JobInBar_Settings_Enabled".Translate(), ref Settings.ModEnabled, "JobInBar_Settings_Enabled_desc".Translate());
            listing.GapLine();
            listing.Label("JobInBar_Settings_PerformanceWarning".Translate());
            listing.GapLine();

            if (Settings.ModEnabled)
            {   
                listing.CheckboxLabeled("JobInBar_Settings_DrawOnlyOnHover".Translate(), ref Settings.DrawLabelOnlyOnHover, "JobInBar_Settings_DrawOnlyOnHover_desc".Translate());
                listing.Gap();

                listing.CheckboxLabeled("JobInBar_Settings_Title".Translate(), ref Settings.DrawRoyalTitles, "JobInBar_Settings_Title_desc".Translate());
                listing.Gap();
                listing.CheckboxLabeled("JobInBar_Settings_Role".Translate(), ref Settings.DrawIdeoRoles, "JobInBar_Settings_Role_desc".Translate());
                if (Settings.DrawIdeoRoles)
                {
                    DoIndent(listing);
                    listing.CheckboxLabeled("JobInBar_Settings_RoleColor".Translate(), ref Settings.UseIdeoColorForRole, "JobInBar_Settings_RoleColor_desc".Translate());
                    if (Settings.UseIdeoColorForRole)
                    {
                        DoIndent(listing);
                        listing.CheckboxLabeled("JobInBar_Settings_RoleColorAbility".Translate(), ref Settings.RoleColorOnlyIfAbilityAvailable, "JobInBar_Settings_RoleColorAbility_desc".Translate());
                        DoOutdent(listing);
                    }
                    DoOutdent(listing);
                }
                
                listing.Gap();
                listing.GapLine();
                
                var buttonWidth = 80f;
                listing.CheckboxLabeled("JobInBar_Settings_DrawCurrentJob".Translate(), ref Settings.DrawCurrentJob, "JobInBar_Settings_DrawCurrentJob_desc".Translate());
                if (Settings.DrawCurrentJob)
                {
                    listing.Gap();
                    DoIndent(listing);
                    listing.Label("JobInBar_Settings_CurrentJobHeader".Translate());
                    var colSettingRect_CurJob = listing.Label("JobInBar_Settings_CurrentJobLabelColor".Translate());
                    
                    if (Widgets.ButtonTextSubtle(new Rect(colSettingRect_CurJob.xMax - buttonWidth - 32f - 8f, colSettingRect_CurJob.yMin, buttonWidth, 32f), "JobInBar_Change".Translate()))
                    {
                        Find.WindowStack.Add(new Dialog_ChooseColor(
                            "JobInBar_Settings_ColorPickerHeading".Translate(),
                            Settings.DefaultJobLabelColor,
                            AllColors, 
                            (color) =>
                            {
                                Settings.CurrentJobLabelColor = color;
                                Settings.CurrentJobLabelColor.a = LabelAlpha;
                            }
                        ));
                    }
                    Widgets.DrawBoxSolid(new Rect(colSettingRect_CurJob.xMax - 32f, colSettingRect_CurJob.yMin, 32f, 32f), Settings.CurrentJobLabelColor);
                    DoOutdent(listing);
                }
                // end left column
                //////////////////////////////

                //////////////////////////////
                // begin right column
                listing.NewColumn();
                listing.ColumnWidth = inRect.width / 2.2f;

                listing.Label("JobInBar_Settings_DisplaySettingsLabel".Translate());
                listing.GapLine();
                listing.Gap(24f);

                listing.CheckboxLabeled("JobInBar_Settings_DrawBG".Translate(), ref Settings.DrawBG, "JobInBar_Settings_DrawBG_desc".Translate());

                listing.Gap();
                
                var colSettingRect = listing.Label("JobInBar_Settings_JobLabelColor".Translate());
                if (Widgets.ButtonTextSubtle(new Rect(colSettingRect.xMax - buttonWidth - 32f - 8f, colSettingRect.yMin, buttonWidth, 32f), "JobInBar_Change".Translate()))
                {
                    Find.WindowStack.Add(new Dialog_ChooseColor(
                        "JobInBar_Settings_ColorPickerHeading".Translate(),
                        Settings.DefaultJobLabelColor,
                        AllColors, 
                        (color) =>
                        {
                            Settings.DefaultJobLabelColor = color;
                            Settings.DefaultJobLabelColor.a = LabelAlpha;
                        }
                    ));
                }
                Widgets.DrawBoxSolid(new Rect(colSettingRect.xMax - 32f, colSettingRect.yMin, 32f, 32f), Settings.DefaultJobLabelColor);
                
                listing.Gap();
                
                listing.Label("JobInBar_Settings_Alpha".Translate() + " " + Settings.LabelAlpha.ToString("N2"));
                Settings.LabelAlpha = listing.Slider(Settings.LabelAlpha, 0.25f, 1f);
                Settings.DefaultJobLabelColor.a = LabelAlpha;
                Settings.CurrentJobLabelColor.a = LabelAlpha;

                listing.GapLine();

                listing.Label("JobInBar_Settings_Positioning".Translate());
                listing.Label("JobInBar_Settings_Ypos".Translate() + Settings.JobLabelVerticalOffset);
                Settings.JobLabelVerticalOffset = (int)listing.Slider(Settings.JobLabelVerticalOffset, -150f, 150f);
                listing.Label("JobInBar_Settings_Ydist".Translate() + Settings.ExtraOffsetPerLine);
                Settings.ExtraOffsetPerLine = (int)listing.Slider(Settings.ExtraOffsetPerLine, -16f, 16f);
            }

            listing.End();
            // end right column
            //////////////////////////////
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ModEnabled, "ModEnabled", true);
            
            Scribe_Values.Look(ref JobLabelVerticalOffset, "JobLabelVerticalOffset", 14);
            Scribe_Values.Look(ref ExtraOffsetPerLine, "ExtraOffsetPerLine", -4);

            Scribe_Values.Look(ref DefaultJobLabelColor, "jobLabelColor", GenMapUI.DefaultThingLabelColor);
            Scribe_Values.Look(ref LabelAlpha, "labelAlpha", 0.8f);
            Scribe_Values.Look(ref CurrentJobLabelColor, "currentJobLabelColor", new Color(1f, 0.8f, 0.4f, 0.8f));
            
            Scribe_Values.Look(ref DrawBG, "DrawBG", true);
            Scribe_Values.Look(ref DrawLabelOnlyOnHover, "DrawLabelOnlyOnHover", false);
            
            Scribe_Values.Look(ref DrawIdeoRoles, "DrawIdeoRoles", true);
            Scribe_Values.Look(ref UseIdeoColorForRole, "UseIdeoColorForRole", true);
            Scribe_Values.Look(ref RoleColorOnlyIfAbilityAvailable, "RoleColorOnlyIfAbilityAvailable", false);
            
            Scribe_Values.Look(ref DrawRoyalTitles, "DrawRoyalTitles", true);
            
            Scribe_Values.Look(ref DrawCurrentJob, "DrawCurrentJob", true);

            base.ExposeData();
        }
    }
}
