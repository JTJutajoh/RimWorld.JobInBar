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
        public static int ExtraOffsetPerLine = -4; // Legacy setting that I don't think anyone used
        public static float ExtraOffsetWeapon = 0.85f;

        public static bool DrawBG = true;
        
        public static bool DrawLabelOnlyOnHover = false;

        public static bool DrawCurrentJob = true;

        public static bool DrawIdeoRoles = true;
        public static bool UseIdeoColorForRole = true;
        public static bool RoleColorOnlyIfAbilityAvailable = false;

        public static bool DrawRoyalTitles = true;

#if v1_4 || v1_5 || v1_6
        // Color
        //TODO: Create some way to modify this list in settings.
        public static List<Color> AllColors = new List<Color>()
        {
            GenMapUI.DefaultThingLabelColor.ClampToValueRange(new FloatRange(0f,0.7f)),
            Color.white,
            Color.gray,
            new Color(0.2f, 0.2f, 0.2f), // Dark gray
            Color.black,
            new Color(0.9f, 0.2f, 0.2f), // Bright red
            new Color(0.6f, 0.1f, 0.1f), // Dark red
            new Color(0.8f, 0.4f, 0f), // Orange 
            new Color(0.8f, 0.6f, 0.2f), // Bronze
            new Color(1.0f, 0.8f, 0.4f), // Yellow
            new Color(1.0f, 0.85f, 0f), // Golden yellow
            new Color(0.7f, 0.7f, 0f), // Olive
            new Color(0.6f, 0.8f, 0.2f), // Lime
            new Color(0.2f, 0.8f, 0.2f), // Bright green
            new Color(0.4f, 0.8f, 0.4f), // Light green
            new Color(0.1f, 0.6f, 0.1f), // Dark green
            new Color(0.2f, 0.6f, 0.6f), // Sea green
            new Color(0.0f, 0.6f, 0.6f), // Teal
            new Color(0f, 0.75f, 0.75f), // Bright cyan
            new Color(0.6f, 0.8f, 0.8f), // Light blue
            new Color(0f, 0.45f, 0.85f), // Strong blue
            new Color(0.2f, 0.4f, 0.8f), // Royal blue
            new Color(0.1f, 0.1f, 0.6f), // Dark blue
            new Color(0.4f, 0.4f, 0.8f), // Periwinkle
            new Color(0.4f, 0.2f, 0.6f), // Purple
            new Color(0.8f, 0.2f, 0.8f), // Bright magenta
            new Color(0.8f, 0.4f, 0.6f), // Rose
        };
        public static Color DefaultJobLabelColor = GenMapUI.DefaultThingLabelColor.ClampToValueRange(new FloatRange(0f,0.7f));
#elif v1_3 || v1_2 || v1_1
        public static Color DefaultJobLabelColor = GenMapUI.DefaultThingLabelColor;
#endif
        public static Color CurrentJobLabelColor = new Color(1f, 0.8f, 0.4f);
        public static float LabelAlpha = 0.6f;

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

                if (Verse.ModsConfig.RoyaltyActive)
                {
                    listing.CheckboxLabeled("JobInBar_Settings_Title".Translate(), ref Settings.DrawRoyalTitles,
                        "JobInBar_Settings_Title_desc".Translate());
                    listing.Gap();
                }
                
                if (Verse.ModsConfig.IdeologyActive)
                {
                    listing.CheckboxLabeled("JobInBar_Settings_Role".Translate(), ref Settings.DrawIdeoRoles,
                        "JobInBar_Settings_Role_desc".Translate());
                    if (Settings.DrawIdeoRoles)
                    {
                        DoIndent(listing);
                        listing.CheckboxLabeled("JobInBar_Settings_RoleColor".Translate(),
                            ref Settings.UseIdeoColorForRole, "JobInBar_Settings_RoleColor_desc".Translate());
                        if (Settings.UseIdeoColorForRole)
                        {
                            DoIndent(listing);
                            listing.CheckboxLabeled("JobInBar_Settings_RoleColorAbility".Translate(),
                                ref Settings.RoleColorOnlyIfAbilityAvailable,
                                "JobInBar_Settings_RoleColorAbility_desc".Translate());
                            DoOutdent(listing);
                        }

                        DoOutdent(listing);
                    }
                    listing.Gap();
                }
                
                listing.GapLine();
                
                var buttonWidth = 80f;
                listing.CheckboxLabeled("JobInBar_Settings_DrawCurrentJob".Translate(), ref Settings.DrawCurrentJob, "JobInBar_Settings_DrawCurrentJob_desc".Translate());
#if v1_4 || v1_5 || v1_6
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
#endif
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
                
#if v1_4 || v1_5 || v1_6
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
#endif
                
                listing.Gap();
                
                listing.Label("JobInBar_Settings_Alpha".Translate() + " " + Settings.LabelAlpha.ToString("N2"));
                Settings.LabelAlpha = listing.Slider(Settings.LabelAlpha, 0.25f, 1f);
                Settings.DefaultJobLabelColor.a = LabelAlpha;
                Settings.CurrentJobLabelColor.a = LabelAlpha;

                listing.GapLine();

                Rect r;
                listing.Label("JobInBar_Settings_Positioning".Translate());
                r = listing.Label("JobInBar_Settings_Ypos".Translate() + Settings.JobLabelVerticalOffset);
                r.yMax += 24;
                TooltipHandler.TipRegion(r, "JobInBar_Settings_Ypos_desc".Translate());
                Settings.JobLabelVerticalOffset = (int)listing.Slider(Settings.JobLabelVerticalOffset, -150f, 150f);
                r = listing.Label("JobInBar_Settings_WeaponOffset".Translate() + Settings.ExtraOffsetWeapon);
                r.yMax += 24;
                TooltipHandler.TipRegion(r, "JobInBar_Settings_WeaponOffset_desc".Translate());
                Settings.ExtraOffsetWeapon = (float)listing.Slider(Settings.ExtraOffsetWeapon, 0f, 1.5f);
                // listing.Label("JobInBar_Settings_Ydist".Translate() + Settings.ExtraOffsetPerLine);
                // Settings.ExtraOffsetPerLine = (int)listing.Slider(Settings.ExtraOffsetPerLine, -16f, 16f);
            }

            listing.End();
            // end right column
            //////////////////////////////
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ModEnabled, "ModEnabled", true);
            
            Scribe_Values.Look(ref JobLabelVerticalOffset, "JobLabelVerticalOffset", 14);
            Scribe_Values.Look(ref ExtraOffsetWeapon, "ExtraOffsetWeapon", 0.85f);
            // Scribe_Values.Look(ref ExtraOffsetPerLine, "ExtraOffsetPerLine", -4);

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
