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
        public static bool DrawIdeoRoles = true;
        public static bool UseIdeoColorForRole = true;
        public static bool DrawRoyalTitles = true;

        // Color
        public static Color jobLabelColor;
        public static float labelAlpha = 0.8f;


        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();

            listingStandard.Begin(inRect);
            //////////////////////////////
            /// listing
            //////////////////////////////
            /// begin left column
            listingStandard.ColumnWidth = inRect.width / 2.2f;

            listingStandard.CheckboxLabeled("JobInBar_Settings_Enabled".Translate(), ref Settings.ModEnabled, "JobInBar_Settings_Enabled_desc".Translate());

            if (Settings.ModEnabled)
            {
                listingStandard.Gap();
                listingStandard.GapLine();

                listingStandard.Label("JobInBar_Settings_Positioning".Translate());
                listingStandard.Label("JobInBar_Settings_Ypos".Translate() + Settings.JobLabelVerticalOffset);
                Settings.JobLabelVerticalOffset = (int)listingStandard.Slider(Settings.JobLabelVerticalOffset, -70f, 70f);
                listingStandard.Label("JobInBar_Settings_Ydist".Translate() + Settings.ExtraOffsetPerLine);
                Settings.ExtraOffsetPerLine = (int)listingStandard.Slider(Settings.ExtraOffsetPerLine, -16f, 16f);

                listingStandard.GapLine();

                listingStandard.CheckboxLabeled("JobInBar_Settings_Truncate".Translate(), ref Settings.TruncateJobs, "JobInBar_Settings_Truncate_desc".Translate());
                listingStandard.CheckboxLabeled("JobInBar_Settings_HideDrafted".Translate(), ref Settings.HideWhenDrafted, "JobInBar_Settings_HideDrafted_desc".Translate());
                listingStandard.Gap();
                listingStandard.CheckboxLabeled("JobInBar_Settings_Job".Translate(), ref Settings.DrawJob, "JobInBar_Settings_Job_desc".Translate());
                listingStandard.CheckboxLabeled("JobInBar_Settings_Title".Translate(), ref Settings.DrawRoyalTitles, "JobInBar_Settings_Title_desc".Translate());
                listingStandard.CheckboxLabeled("JobInBar_Settings_Role".Translate(), ref Settings.DrawIdeoRoles, "JobInBar_Settings_Role_desc".Translate());
                if (Settings.DrawIdeoRoles)
                {
                    listingStandard.CheckboxLabeled("JobInBar_Settings_RoleColor".Translate(), ref Settings.UseIdeoColorForRole, "JobInBar_Settings_RoleColor_desc".Translate());
                }
                /// end left column
                //////////////////////////////
                //////////////////////////////
                /// begin right column
                listingStandard.NewColumn();
                listingStandard.ColumnWidth = inRect.width / 2.2f;

                listingStandard.CheckboxLabeled("JobInBar_Settings_DrawBG".Translate(), ref Settings.DrawBG, "JobInBar_Settings_DrawBG_desc".Translate());

                listingStandard.Gap();
                Rect colSettingRect = listingStandard.Label("JobInBar_Settings_JobLabelColor".Translate());
                colSettingRect.x += 32f * 4;
                colSettingRect.y -= 6f;
                colSettingRect.size = new Vector2(32f, 32f);
                Widgets.DrawBoxSolid(colSettingRect, Settings.jobLabelColor);
                if (Widgets.ButtonInvisible(colSettingRect, true))
                {
                    Find.WindowStack.Add(new Dialog_ColourPicker(Settings.jobLabelColor,
                    (newColor) =>
                    {
                        Settings.jobLabelColor = newColor;
                        Settings.jobLabelColor.a = labelAlpha;
                    }
                    ));
                }
                listingStandard.Gap();
                listingStandard.Label("JobInBar_Settings_Alpha".Translate() + " " + Settings.labelAlpha.ToString("N2"));
                Settings.labelAlpha = listingStandard.Slider(Settings.labelAlpha, 0f, 1f);
                Settings.jobLabelColor.a = labelAlpha;
            }

            listingStandard.End();
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
            Scribe_Values.Look(ref HideWhenDrafted, "HideWhenDrafted", true);
            Scribe_Values.Look(ref ModEnabled, "ModEnabled", true);
            Scribe_Values.Look(ref DrawJob, "DrawJob", true);
            Scribe_Values.Look(ref DrawIdeoRoles, "DrawIdeoRoles", true);
            Scribe_Values.Look(ref UseIdeoColorForRole, "UseIdeoColorForRole", true);
            Scribe_Values.Look(ref DrawRoyalTitles, "DrawRoyalTitles", true);

            Scribe_Values.Look(ref jobLabelColor, "jobLabelColor", GenMapUI.DefaultThingLabelColor);
            Scribe_Values.Look(ref labelAlpha, "labelAlpha", 0.8f);

            base.ExposeData();
        }
    }
}
