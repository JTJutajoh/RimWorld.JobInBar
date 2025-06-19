using System;
using System.Linq;
using JobInBar.HarmonyPatches;
using RimWorld.Planet;
using UnityEngine;

namespace JobInBar
{
    public static class PawnLabelExtensions
    {
        public static bool DrawAnyPermanentLabels(this Pawn colonist, Rect rect)
        {
            if (Settings.ModEnabled == false || Patch_PlaySettings_GlobalControl_ToggleLabels.DrawLabels == false)
                return false;

            if (Settings.DrawLabelOnlyOnHover && !Mouse.IsOver(rect))
                return false;

            return true;
        }

        public static bool ShouldDrawJobLabel(this Pawn colonist) => Settings.DrawJobTitle && colonist.story?.Title != null &&
                (LabelsTracker_WorldComponent.Instance?[colonist].ShowBackstory ?? false);

        public static bool ShouldDrawIdeoLabel(this Pawn colonist) =>
            Settings.DrawIdeoRoles &&
            (colonist.ideo?.Ideo?.GetRole(colonist) is not null) &&
            (LabelsTracker_WorldComponent.Instance?[colonist].ShowIdeoRole ?? false);

        public static bool ShouldDrawRoyaltyLabel(this Pawn colonist) =>
            Settings.DrawRoyalTitles && 
            (colonist.royalty?.MainTitle() is not null) &&
            (LabelsTracker_WorldComponent.Instance?[colonist].ShowRoyalTitle ?? false);

        public static Color JobLabelColor(this Pawn pawn) => LabelsTracker_WorldComponent.Instance?[pawn].BackstoryColor ?? Settings.DefaultJobLabelColor;

        // Looks up the pawn's ideology and returns the rgb color associated with that ideology, adjusting it for readability
        public static Color IdeoLabelColor(this Pawn pawn)
        {
            if (!Settings.UseIdeoColorForRole)
            {
                return Settings.IdeoRoleColorOverride;
            }
            try
            {
                if (Settings.UseIdeoColorForRole && pawn.ideo?.Ideo?.GetRole(pawn) is { } role)
                {
                    bool abilityReady = pawn.ideo?.Ideo?.GetRole(pawn)?.AbilitiesFor(pawn)[0]?.CanCast ?? false;
                    if (!Settings.RoleColorOnlyIfAbilityAvailable ||
                        (Settings.RoleColorOnlyIfAbilityAvailable && abilityReady))
                    {
                        // Brighten ideo colors so dark ones are readable
                        // Magic number: 0.35f for the lerp to white, to brighten every ideo color by a set amount. 
                        // tested with black to assure minimum readability
                        return Color.Lerp(role.ideo.colorDef.color, Color.white, 0.35f);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, extraMessage: "Ideo label color", once: true);
            }

            return PawnNameColorUtility.PawnNameColorOf(pawn);
        }

        public static string JobLabel(this Pawn colonist) => colonist.story?.TitleShortCap ?? "";

        public static string IdeoLabel(this Pawn colonist) => colonist.ideo?.Ideo?.GetRole(colonist)?.LabelForPawn(colonist) ?? "";

        public static string RoyaltyLabel(this Pawn colonist) => colonist.royalty?.MainTitle()?.GetLabelCapFor(colonist) ?? "";

        public static float LabelYOffset(this Pawn colonist) 
        {
#if v1_4 || v1_5 || v1_6 
            // var equipment = colonist.equipment;
            //
            // var showWeaponMode = Prefs.ShowWeaponsUnderPortraitMode;
            // var isWeaponShownPref = showWeaponMode == ShowWeaponsUnderPortraitMode.Always ||
            //                         showWeaponMode == ShowWeaponsUnderPortraitMode.WhileDrafted && colonist.Drafted;
            // var hasWeaponEquipped = colonist.equipment?.Primary?.def?.IsWeapon ?? false;
            //
            // if (isWeaponShownPref && hasWeaponEquipped)
            // {
            //     return ColonistBar.BaseSize.y * Find.ColonistBar.Scale * 0.75f * Settings.OffsetEquippedExtra;
            // }
#endif

            return 0f;
        }

        private static Caravan? Caravan(this Pawn pawn) => pawn.ParentHolder as Caravan;

        public static string CurrentTaskDesc(this Pawn pawn)
        {
            var text = "";
            if (pawn.jobs?.curDriver is { } curDriver)
            {
                text = curDriver.GetReport();
            }
            else
            {
                var inCaravan = pawn.Caravan() != null;
                if (inCaravan)
                {
                    text = pawn.Caravan()?.LabelCap + ": " + pawn.Caravan()?.GetInspectString();
                    if (text.Contains('\n'))
                    {
                        text = text.Substring(0, text.IndexOf('\n'));
                    }
                }
            }
            return text.CapitalizeFirst();
        }

        public static bool IsLabelTracked(this Pawn pawn) => LabelsTracker_WorldComponent.Instance?.TrackedPawns.ContainsKey(pawn) ?? false;
    }
}
