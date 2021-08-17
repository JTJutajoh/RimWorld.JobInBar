using System;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace JobInBar
{
    public class LabelUtils
    {
        //private static PawnLabelCustomColors_WorldComponent labelsComp;

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
            PawnLabelCustomColors_WorldComponent labelsComp = PawnLabelCustomColors_WorldComponent.instance;
            
            if (labelsComp.GetDrawJobLabelFor(colonist)) // Check if this particular colonist has their job label enabled or not
            {
                return Settings.DrawJob;
            }
            else
            {
                return false;
            }
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
        public static Color GetJobLabelColorForPawn(Pawn pawn)
        {
            Color LabelColor = Settings.defaultJobLabelColor;

            PawnLabelCustomColors_WorldComponent labelsComp = PawnLabelCustomColors_WorldComponent.instance;

            labelsComp.GetJobLabelColorFor(pawn, out LabelColor);

            return LabelColor;
        }
        // Looks up the pawn's ideology and returns the rgb color associated with that ideology, adjusting it for readability
        public static Color GetIdeoLabelColorForPawn(Pawn pawn)
        {
            Color LabelColor = new Color(1f, 1f, 1f);

            if (Settings.RoleColorOnlyIfAbilityAvailable)
            {
                if (pawn.ideo.Ideo.GetRole(pawn).AbilitiesFor(pawn)[0].CanCast == false)
                {
                    return PawnNameColorUtility.PawnNameColorOf(pawn);
                }
            }

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
            LabelColor = GetJobLabelColorForPawn(pawn);
            return LabelColor;
        }
        public static string TruncateLabel(string labelString, float truncateToWidth, GameFont font)
        {
            if (Settings.TruncateJobs == false)
                return labelString;
            GameFont font2 = Text.Font;
            Text.Font = font;
            labelString = labelString.Truncate(truncateToWidth);
            Text.Font = font2; // reset font

            return labelString;
        }

        public static string GetJobLabel(Pawn colonist)
        {
            string jobLabel = "Job";
            jobLabel = colonist.story.TitleShortCap;

            return jobLabel;
        }

        public static string GetIdeoRoleLabel(Pawn colonist)
        {
            Precept_Role myRole = colonist.ideo.Ideo.GetRole(colonist);
            string roleLabel = "";

            if (myRole != null)
            {
                roleLabel = myRole.LabelForPawn(colonist);
            }

            return roleLabel;
        }

        public static string GetRoyalTitleLabel(Pawn colonist)
        {
            RoyalTitleDef myTitle = colonist.royalty.MainTitle();
            string titleLabel = "";

            if (myTitle != null)
            {
                titleLabel = myTitle.GetLabelCapFor(colonist);
            }

            return titleLabel;
        }
    }
}
