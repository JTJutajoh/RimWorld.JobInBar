using System;
using Verse;
using RimWorld;
using UnityEngine;
using ColourPicker;

namespace JobInBar
{
    class Settings : Verse.ModSettings
    {
        public static int JobLabelVerticalOffset = 14;
        public static int ExtraOffsetPerLine = -4;
        public static bool ModEnabled = true;
        public static bool DrawBG = true;
        public static bool TruncateJobs = true;
        public static bool HideWhenDrafted = false;
        public static bool DrawJob = true;
        public static bool DefaultShowSetting = true;
        public static bool DrawIdeoRoles = true;
        public static bool UseIdeoColorForRole = true;
        public static bool RoleColorOnlyIfAbilityAvailable = false;
        public static bool DrawRoyalTitles = true;

        // Color
        public static Color defaultJobLabelColor;
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
            listing.ColumnWidth = inRect.width / 2f;

            listing.CheckboxLabeled("JobInBar_Settings_Enabled".Translate(), ref Settings.ModEnabled, "JobInBar_Settings_Enabled_desc".Translate());
            listing.GapLine();
            listing.Gap(24f);

            if (Settings.ModEnabled)
            {
                listing.CheckboxLabeled("JobInBar_Settings_Truncate".Translate(), ref Settings.TruncateJobs, "JobInBar_Settings_Truncate_desc".Translate());
                listing.CheckboxLabeled("JobInBar_Settings_HideDrafted".Translate(), ref Settings.HideWhenDrafted, "JobInBar_Settings_HideDrafted_desc".Translate());
                listing.Gap();
                listing.CheckboxLabeled("JobInBar_Settings_Job".Translate(), ref Settings.DrawJob, "JobInBar_Settings_Job_desc".Translate());
                if (Settings.DrawJob)
                {
                    DoIndent(listing);
                    listing.CheckboxLabeled("JobInBar_Settings_DefaultShow".Translate(), ref Settings.DefaultShowSetting, "JobInBar_Settings_DefaultShow_desc".Translate());
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


                /// end left column
                //////////////////////////////
                //listing.NewColumn();
                //listing.ColumnWidth = 32f;
                //////////////////////////////
                /// begin right column
                listing.NewColumn();
                listing.ColumnWidth = inRect.width / 2f;

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
                Widgets.DrawBoxSolid(colSettingRect, Settings.defaultJobLabelColor);
                if (Widgets.ButtonInvisible(colSettingRect, true))
                {
                    Find.WindowStack.Add(new Dialog_ColourPicker(Settings.defaultJobLabelColor,
                    (newColor) =>
                    {
                        Settings.defaultJobLabelColor = newColor;
                        Settings.defaultJobLabelColor.a = labelAlpha;
                    }
                    ));
                }
                listing.Gap();
                listing.Label("JobInBar_Settings_Alpha".Translate() + " " + Settings.labelAlpha.ToString("N2"));
                Settings.labelAlpha = listing.Slider(Settings.labelAlpha, 0f, 1f);
                Settings.defaultJobLabelColor.a = labelAlpha;

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
            Scribe_Values.Look(ref TruncateJobs, "TruncateJobs", true);
            Scribe_Values.Look(ref HideWhenDrafted, "HideWhenDrafted", false);
            Scribe_Values.Look(ref ModEnabled, "ModEnabled", true);
            Scribe_Values.Look(ref DrawJob, "DrawJob", true);
            Scribe_Values.Look(ref DefaultShowSetting, "DefaultShowSetting", true);
            Scribe_Values.Look(ref DrawIdeoRoles, "DrawIdeoRoles", true);
            Scribe_Values.Look(ref UseIdeoColorForRole, "UseIdeoColorForRole", true);
            Scribe_Values.Look(ref RoleColorOnlyIfAbilityAvailable, "RoleColorOnlyIfAbilityAvailable", false);
            Scribe_Values.Look(ref DrawRoyalTitles, "DrawRoyalTitles", true);

            Scribe_Values.Look(ref defaultJobLabelColor, "jobLabelColor", GenMapUI.DefaultThingLabelColor);
            Scribe_Values.Look(ref labelAlpha, "labelAlpha", 0.8f);

            base.ExposeData();
        }
    }
}
