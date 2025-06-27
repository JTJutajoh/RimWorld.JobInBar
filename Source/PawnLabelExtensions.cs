using System;
using System.Linq;
using JobInBar.HarmonyPatches;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace JobInBar;

public static class PawnLabelExtensions
{
    public static bool DrawAnyPermanentLabels(this Pawn pawn)
    {
        if (Settings.ModEnabled == false || Patch_PlaySettings_GlobalControl_ToggleLabels.DrawLabels == false)
            return false;

        if (Settings.IgnoreGuests && pawn.HomeFaction != Faction.OfPlayer && !pawn.IsSlaveOfColony)
            return false;

        if (Settings.IgnoreSlaves && pawn.IsSlave)
            return false;

#if !(v1_1 || v1_2 || v1_3 || v1_4 || v1_5)
        if (Settings.IgnoreSubhuman && pawn.IsSubhuman)
            return false;
#elif v1_5 // 1.5 didn't include the IsSubhuman property, but did include relevant pawn types (ghouls)
        if (Settings.IgnoreSubhuman && pawn.IsMutant)
            return false;
#endif

        if (Settings.OnlyDrafted && !pawn.Drafted)
            return false;

        if (Settings.OnlyCurrentMap && pawn.Map != Find.CurrentMap)
            return false;

        if (Settings.DrawLabelOnlyOnHover && LabelDrawer.HoveredPawn != pawn)
            return false;

        if (Settings.MinColonistBarScaleBehavior != Settings.MinScaleBehavior.ShowAll &&
            Find.ColonistBar?.Scale < Settings.MinColonistBarScale)
        {
            if (Settings.MinColonistBarScaleBehavior == Settings.MinScaleBehavior.HideAll)
                return false;
            if (Settings.MinColonistBarScaleBehavior == Settings.MinScaleBehavior.ShowOnHover)
                return LabelDrawer.HoveredPawn == pawn;
        }

        //TODO: Setting to exclude certain types of pawns from labels (like temporary quest pawns)

        return true;
    }

    public static bool ShouldDrawJobLabel(this Pawn colonist)
    {
        // Global setting
        if (!Settings.DrawJobTitle) return false;

        if (colonist.DrawAnyPermanentLabels() == false) return false;

        // Pawn-specific setting
        if (!LabelsTracker_WorldComponent.Instance?[colonist].ShowBackstory ?? false) return false;

        // Story tracker null check
        if (colonist.story is not { } story) return false;

        if (Settings.MinColonistBarScaleBehavior != Settings.MinScaleBehavior.ShowAll &&
            Find.ColonistBar?.Scale < Settings.MinColonistBarScale)
        {
            switch (Settings.MinColonistBarScaleBehavior)
            {
                case Settings.MinScaleBehavior.ShowOnlyCustomExceptOnHover:
                    return story.title != null || LabelDrawer.HoveredPawn == colonist;
                case Settings.MinScaleBehavior.ShowOnlyCustom:
                    return story.title != null;
            }
        }

        // story.title FIELD is the player-set custom one. story.Title PROPERTY defaults to the backstory if no custom is set
        var title = Settings.OnlyDrawCustomJobTitles ? story.title : story.Title;

        return title != null;
    }

    public static bool ShouldDrawRoyaltyLabel(this Pawn pawn)
    {
        if (ModsConfig.RoyaltyActive == false) return false;

        if (pawn.DrawAnyPermanentLabels() == false) return false;
        if (!Settings.DrawRoyalTitles || !pawn.HasRoyalTitle() ||
            !(LabelsTracker_WorldComponent.Instance?[pawn].ShowRoyalTitle ?? false)) return false;

        if (Settings.MinColonistBarScaleBehavior == Settings.MinScaleBehavior.ShowAll ||
            !(Find.ColonistBar?.Scale < Settings.MinColonistBarScale)) return true;

        if (Settings.MinColonistBarScaleBehavior == Settings.MinScaleBehavior.ShowOnlyCustomExceptOnHover)
            return LabelDrawer.HoveredPawn == pawn;

        return true;
    }

    internal static bool HasRoyalTitle(this Pawn pawn)
    {
        return pawn.royalty?.MainTitle() is not null;
    }

    public static Color JobLabelColor(this Pawn pawn)
    {
        return LabelsTracker_WorldComponent.Instance?[pawn].BackstoryColor ?? Settings.DefaultJobLabelColor;
    }


#if !(v1_1 || v1_2)
    public static bool ShouldDrawIdeoLabel(this Pawn pawn)
    {
        if (ModsConfig.IdeologyActive == false) return false;

        if (pawn.DrawAnyPermanentLabels() == false) return false;
        if (!Settings.DrawIdeoRoles || !pawn.HasIdeoRole() ||
            !(LabelsTracker_WorldComponent.Instance?[pawn].ShowIdeoRole ?? false)) return false;

        if (Settings.MinColonistBarScaleBehavior == Settings.MinScaleBehavior.ShowAll ||
            !(Find.ColonistBar?.Scale < Settings.MinColonistBarScale)) return true;

        if (Settings.MinColonistBarScaleBehavior == Settings.MinScaleBehavior.ShowOnlyCustomExceptOnHover)
            return LabelDrawer.HoveredPawn == pawn;

        return true;
    }

    internal static bool HasIdeoRole(this Pawn pawn)
    {
        return ModsConfig.IdeologyActive && pawn.ideo?.Ideo?.GetRole(pawn) is not null;
    }

    // Looks up the pawn's ideology and returns the rgb color associated with that ideology, adjusting it for readability
    public static Color IdeoLabelColor(this Pawn pawn)
    {
        var fallbackColor = GenMapUI.DefaultThingLabelColor;
        // If the user has disabled the automatic color assignment, check for any individual color settings and otherwise use the global setting
        if (!Settings.UseIdeoColorForRole)
            return LabelsTracker_WorldComponent.Instance?[pawn].IdeoRoleColor ?? fallbackColor;

        Precept_Role? role = null;
        // Get a cached color override (or null if no override has been set)
        Color? ideoColor = LabelsTracker_WorldComponent.Instance?[pawn].IdeoRoleColor;
        // If there isn't an override saved in the cache, determine a color
        if (ideoColor == null)
        {
            try
            {
                role = pawn.ideo?.Ideo?.GetRole(pawn);
                ideoColor = role?.ideo?.colorDef?.color;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Ideo label color.", true);
                return fallbackColor;
            }

            ideoColor = Color.Lerp(ideoColor ?? fallbackColor, Color.white, 0.35f);
        }

        if (role == null) return ideoColor.Value;

        if (Settings.RoleColorOnlyIfAbilityAvailable)
        {
            return pawn.IdeoRoleAbilityReady(role) ? ideoColor.Value : fallbackColor;
        }

        return ideoColor.Value;
    }

    internal static bool IdeoRoleAbilityReady(this Pawn pawn, Precept_Role? role = null)
    {
        if (!ModsConfig.IdeologyActive) return false;

        role ??= pawn.ideo?.Ideo?.GetRole(pawn);
        return role?.AbilitiesFor(pawn)?.FirstOrDefault()?.CanCast ?? false;
    }

    public static string IdeoLabel(this Pawn colonist)
    {
        return ModsConfig.IdeologyActive ? colonist.ideo?.Ideo?.GetRole(colonist)?.LabelForPawn(colonist) ?? "" : "";
    }
#else
    /// <summary>
    /// Pre-Ideology stub.
    /// </summary>
    public static bool ShouldDrawIdeoLabel(this Pawn pawn) => false;
#endif

    internal static Color RoyalTitleColor(this Pawn pawn)
    {
        return LabelsTracker_WorldComponent.Instance?[pawn].RoyalTitleColor ?? Settings.RoyalTitleColorDefault;
    }

    public static string JobLabel(this Pawn colonist)
    {
        return colonist.story?.TitleShortCap ?? "";
    }

    public static string RoyaltyLabel(this Pawn colonist)
    {
        return colonist.royalty?.MainTitle()?.GetLabelCapFor(colonist) ?? "";
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
