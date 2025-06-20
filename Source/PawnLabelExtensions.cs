using System;
using System.Linq;
using JobInBar.HarmonyPatches;
using RimWorld.Planet;
using UnityEngine;

namespace JobInBar;

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

    public static bool ShouldDrawJobLabel(this Pawn colonist)
    {
        // Global setting
        if (!Settings.DrawJobTitle) return false;

        // Pawn-specific setting
        if (!LabelsTracker_WorldComponent.Instance?[colonist].ShowBackstory ?? false) return false;

        // Story tracker null check
        if (colonist.story is not { } story) return false;

        // story.title FIELD is the player-set custom one. story.Title PROPERTY defaults to the backstory if no custom is set
        var title = Settings.OnlyDrawCustomJobTitles ? story.title : story.Title;

        return title != null;
    }

    public static bool ShouldDrawIdeoLabel(this Pawn colonist)
    {
        return Settings.DrawIdeoRoles &&
               colonist.ideo?.Ideo?.GetRole(colonist) is not null &&
               (LabelsTracker_WorldComponent.Instance?[colonist].ShowIdeoRole ?? false);
    }

    public static bool ShouldDrawRoyaltyLabel(this Pawn colonist)
    {
        return Settings.DrawRoyalTitles &&
               colonist.royalty?.MainTitle() is not null &&
               (LabelsTracker_WorldComponent.Instance?[colonist].ShowRoyalTitle ?? false);
    }

    public static Color JobLabelColor(this Pawn pawn)
    {
        return LabelsTracker_WorldComponent.Instance?[pawn].BackstoryColor ?? Settings.DefaultJobLabelColor;
    }

    // Looks up the pawn's ideology and returns the rgb color associated with that ideology, adjusting it for readability
    public static Color IdeoLabelColor(this Pawn pawn)
    {
        if (!Settings.UseIdeoColorForRole) return Settings.IdeoRoleColorOverride;
        try
        {
            if (Settings.UseIdeoColorForRole && pawn.ideo?.Ideo?.GetRole(pawn) is { } role)
            {
                bool abilityReady = pawn.ideo?.Ideo?.GetRole(pawn)?.AbilitiesFor(pawn)?[0]?.CanCast ?? false;
                if (!Settings.RoleColorOnlyIfAbilityAvailable ||
                    (Settings.RoleColorOnlyIfAbilityAvailable && abilityReady))
                    // Brighten ideo colors so dark ones are readable
                    // Magic number: 0.35f for the lerp to white, to brighten every ideo color by a set amount.
                    // tested with black to assure minimum readability
                    return Color.Lerp(role.ideo?.colorDef?.color ?? Settings.IdeoRoleColorOverride, Color.white, 0.35f);
            }
        }
        catch (Exception e)
        {
            Log.Exception(e, "Ideo label color", true);
        }

        return PawnNameColorUtility.PawnNameColorOf(pawn);
    }

    public static string JobLabel(this Pawn colonist)
    {
        return colonist.story?.TitleShortCap ?? "";
    }

    public static string IdeoLabel(this Pawn colonist)
    {
        return colonist.ideo?.Ideo?.GetRole(colonist)?.LabelForPawn(colonist) ?? "";
    }

    public static string RoyaltyLabel(this Pawn colonist)
    {
        return colonist.royalty?.MainTitle()?.GetLabelCapFor(colonist) ?? "";
    }

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

    private static Caravan? Caravan(this Pawn pawn)
    {
        return pawn.ParentHolder as Caravan;
    }

    public static string CurrentTaskDesc(this Pawn pawn)
    {
        // Default to "Unknown", overwritten if a job is found
        string text = "JobInBar_Unknown".Translate();
        if (pawn.jobs?.curDriver?.GetReport() is { } report)
        {
            text = report;
        }
        else
        {
            if (pawn.Caravan() is not { } caravan) return text.CapitalizeFirst()!;

            text = caravan.LabelCap + ": " + caravan.GetInspectString();
            if (text.Contains('\n')) text = text.Substring(0, text.IndexOf('\n'));
        }

        return text.CapitalizeFirst()!;
    }

    public static bool IsLabelTracked(this Pawn pawn)
    {
        return LabelsTracker_WorldComponent.Instance?.TrackedPawns.ContainsKey(pawn) ?? false;
    }
}
