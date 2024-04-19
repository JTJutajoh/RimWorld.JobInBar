using System;
using Verse;
using RimWorld;
using UnityEngine;
using DarkColourPicker_Forked;
using DarkLog;

namespace JobInBar
{
    class Settings : Verse.ModSettings
    {
        public static int JobLabelVerticalOffset = 14;
        public static int ExtraOffsetPerLine = -4;
        public static bool ModEnabled = true;
        public static bool DrawBG = true;
        public static bool HideWhenDrafted = false;
        public static bool DrawJob = true;
        public static bool OnlyDrawJobIfCustom = false;
        public static bool DrawLabelOnlyOnHover = false;
        public static bool DrawCurrentJob = true;
        public static bool DrawIdeoRoles = true;
        public static bool UseIdeoColorForRole = true;
        public static bool RoleColorOnlyIfAbilityAvailable = false;
        public static bool DrawRoyalTitles = true;

        // Color
        public readonly static Color defaultDefaultJobLabelColor = GenMapUI.DefaultThingLabelColor.ClampToValueRange(new FloatRange(0f,0.7f));
        private static Color defaultJobLabelColor = defaultDefaultJobLabelColor;
        public static bool useCustomJobLabelColor = false;
        public static Color DefaultJobLabelColor { get { return Settings.useCustomJobLabelColor ? Settings.defaultJobLabelColor : Settings.defaultDefaultJobLabelColor; } }
        public static Color currentJobLabelColor = Color.yellow;
        public static float labelAlpha = 0.8f;


        // Pair of functions to have indents that don't go into the next column over
        // Utility to copy to other mods maybe
        private void DoIndent(Listing_Standard listing, float amount = 12f)
        {
            listing.ColumnWidth -= amount;
            listing.Indent(amount);
        }
        private void DoOutdent(Listing_Standard listing, float amount = 12f)
        {
            listing.ColumnWidth += amount;
            listing.Outdent(amount);
        }


        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();

            listing.Begin(inRect);
            //////////////////////////////
            /// listing
            //////////////////////////////
            /// begin left column
            listing.ColumnWidth = inRect.width / 2.2f;

            listing.CheckboxLabeled("JobInBar_Settings_Enabled".Translate(), ref Settings.ModEnabled, "JobInBar_Settings_Enabled_desc".Translate());
            listing.GapLine();
            listing.Label("JobInBar_Settings_PerformanceWarning".Translate());
            listing.GapLine();

            if (Settings.ModEnabled)
            {   
                listing.CheckboxLabeled("JobInBar_Settings_DrawOnlyOnHover".Translate(), ref Settings.DrawLabelOnlyOnHover, "JobInBar_Settings_DrawOnlyOnHover_desc".Translate());
                listing.CheckboxLabeled("JobInBar_Settings_Job".Translate(), ref Settings.DrawJob, "JobInBar_Settings_Job_desc".Translate());
                if (Settings.DrawJob)
                {
                    DoIndent(listing);
                    listing.CheckboxLabeled("JobInBar_Settings_OnlyDrawJobIfCustom".Translate(), ref Settings.OnlyDrawJobIfCustom, "JobInBar_Settings_OnlyDrawJobIfCustom_desc".Translate());
                    DoOutdent(listing);
                }
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
                listing.CheckboxLabeled("JobInBar_Settings_DrawCurrentJob".Translate(), ref Settings.DrawCurrentJob, "JobInBar_Settings_DrawCurrentJob_desc".Translate());
                if (Settings.DrawCurrentJob)
                {
                    listing.Gap();
                    DoIndent(listing);
                    listing.Label("JobInBar_Settings_CurrentJobHeader".Translate());
                    Rect colSettingRect_CurJob = listing.Label("JobInBar_Settings_CurrentJobLabelColor".Translate());
                    //colSettingRect.x += 32f * 6;
                    colSettingRect_CurJob.x += colSettingRect_CurJob.width - 32f;
                    colSettingRect_CurJob.y -= 6f;
                    colSettingRect_CurJob.size = new Vector2(32f, 32f);
                    Widgets.DrawBoxSolid(colSettingRect_CurJob.ExpandedBy(2f, 2f), Color.white);
                    Widgets.DrawBoxSolid(colSettingRect_CurJob.ExpandedBy(1f, 1f), Color.black);
                    Widgets.DrawBoxSolid(colSettingRect_CurJob, Settings.currentJobLabelColor);
                    if (Widgets.ButtonInvisible(colSettingRect_CurJob, true))
                    {
                        Find.WindowStack.Add(new Dialog_ColourPicker(Settings.currentJobLabelColor,
                        (newColor) =>
                        {
                            Settings.currentJobLabelColor = newColor;
                            Settings.currentJobLabelColor.a = labelAlpha;
                        }
                        ));
                    }
                    DoOutdent(listing);
                }


                /// end left column
                //////////////////////////////
                //listing.NewColumn();
                //listing.ColumnWidth = 32f;
                //////////////////////////////
                /// begin right column
                listing.NewColumn();
                listing.ColumnWidth = inRect.width / 2.2f;

                listing.Label("JobInBar_Settings_DisplaySettingsLabel".Translate());
                listing.GapLine();
                listing.Gap(24f);

                listing.CheckboxLabeled("JobInBar_Settings_DrawBG".Translate(), ref Settings.DrawBG, "JobInBar_Settings_DrawBG_desc".Translate());

                listing.Gap();
                Rect colSettingRect = listing.Label("JobInBar_Settings_JobLabelColor".Translate());
                //colSettingRect.x += 32f * 6;
                colSettingRect.x += colSettingRect.width - 32f;
                colSettingRect.y -= 6f;
                colSettingRect.size = new Vector2(32f, 32f);
                Widgets.DrawBoxSolid(colSettingRect.ExpandedBy(2f,2f), Color.white);
                Widgets.DrawBoxSolid(colSettingRect.ExpandedBy(1f,1f), Color.black);
                Widgets.DrawBoxSolid(colSettingRect, Settings.useCustomJobLabelColor ? Settings.defaultJobLabelColor : GenMapUI.DefaultThingLabelColor);
                if (Widgets.ButtonInvisible(colSettingRect, true))
                {
                    Find.WindowStack.Add(new Dialog_ColourPicker(Settings.useCustomJobLabelColor ? Settings.defaultJobLabelColor : GenMapUI.DefaultThingLabelColor,
                    (newColor) =>
                    {
                        Settings.defaultJobLabelColor = newColor;
                        Settings.defaultJobLabelColor.a = labelAlpha;
                        Settings.useCustomJobLabelColor = true;
                    }
                    ));
                }
                Rect colResetRect = colSettingRect;
                colResetRect.x += colSettingRect.width + 8;
                colResetRect.width = 100;
                if (Widgets.ButtonText(colResetRect, "JobInBar_Settings_ResetColor".Translate()))
                {
                    Settings.useCustomJobLabelColor = false;
                    Settings.defaultJobLabelColor = Settings.defaultDefaultJobLabelColor;
                    Settings.labelAlpha = 0.8f;
                }
                listing.Gap();
                listing.Label("JobInBar_Settings_Alpha".Translate() + " " + Settings.labelAlpha.ToString("N2"));
                Settings.labelAlpha = listing.Slider(Settings.labelAlpha, 0.25f, 1f);
                Settings.defaultJobLabelColor.a = labelAlpha;
                Settings.currentJobLabelColor.a = labelAlpha;

                listing.GapLine();

                listing.Label("JobInBar_Settings_Positioning".Translate());
                listing.Label("JobInBar_Settings_Ypos".Translate() + Settings.JobLabelVerticalOffset);
                Settings.JobLabelVerticalOffset = (int)listing.Slider(Settings.JobLabelVerticalOffset, -150f, 150f);
                listing.Label("JobInBar_Settings_Ydist".Translate() + Settings.ExtraOffsetPerLine);
                Settings.ExtraOffsetPerLine = (int)listing.Slider(Settings.ExtraOffsetPerLine, -16f, 16f);
            }

            listing.End();
            /// end right column
            //////////////////////////////
            /// end listing
            //////////////////////////////
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref JobLabelVerticalOffset, "JobLabelVerticalOffset", 14);
            Scribe_Values.Look(ref ExtraOffsetPerLine, "ExtraOffsetPerLine", -4);
            Scribe_Values.Look(ref DrawBG, "DrawBG", true);
            Scribe_Values.Look(ref HideWhenDrafted, "HideWhenDrafted", false);
            Scribe_Values.Look(ref ModEnabled, "ModEnabled", true);
            Scribe_Values.Look(ref DrawJob, "DrawJob", true);
            Scribe_Values.Look(ref OnlyDrawJobIfCustom, "OnlyDrawJobIfCustom", false);
            Scribe_Values.Look(ref DrawLabelOnlyOnHover, "DrawLabelOnlyOnHover", false);
            Scribe_Values.Look(ref DrawIdeoRoles, "DrawIdeoRoles", true);
            Scribe_Values.Look(ref UseIdeoColorForRole, "UseIdeoColorForRole", true);
            Scribe_Values.Look(ref RoleColorOnlyIfAbilityAvailable, "RoleColorOnlyIfAbilityAvailable", false);
            Scribe_Values.Look(ref DrawRoyalTitles, "DrawRoyalTitles", true);
            Scribe_Values.Look(ref DrawCurrentJob, "DrawCurrentJob", true);

            Scribe_Values.Look(ref labelAlpha, "labelAlpha", 0.8f);

            Scribe_Values.Look(ref defaultJobLabelColor, "jobLabelColor", GenMapUI.DefaultThingLabelColor);
            if (useCustomJobLabelColor && defaultJobLabelColor.IndistinguishableFrom(new Color(0,0,0, labelAlpha)))
            {
                LogPrefixed.Warning($"[Dark.JobInBar] Found default job label color with broken value. Setting 'useCustomJobLabelColor' to false to ignore the config value (set the color in mod options again to override if it was the color you wanted).");
                useCustomJobLabelColor = false;
            }
            else
            {
                useCustomJobLabelColor = true;
            }
            Scribe_Values.Look(ref useCustomJobLabelColor, "useCustomJobLabelColor", false);
            Scribe_Values.Look(ref currentJobLabelColor, "currentJobLabelColor", Color.yellow);

            base.ExposeData();
        }
    }
}
