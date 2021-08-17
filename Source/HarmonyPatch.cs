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
    public class LabelPatch
    {
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
                Log.Message("'colonist' passed to ColonistBarColonistDrawer was null. This should never happen. This indicates something may be very wrong with a mod incompatibility. Skipping this pawn for job labels");
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