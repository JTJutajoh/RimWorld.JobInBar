using System;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace JobInBar
{
    public class JobInBarUtils
    {
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

        public static bool GetShouldDrawJobLabel(Pawn colonist)
        {
            return Settings.DrawJob;
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

        // Fetches the label color setting for the job label (And others in certain situations)
        public static Color GetLabelColorForPawn(Pawn pawn)
        {
            Color LabelColor = Settings.jobLabelColor;

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
}
