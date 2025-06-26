using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;

namespace JobInBar.HarmonyPatches;

[HarmonyPatch]
[HarmonyPatchCategory("ColorName")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
internal static class Patch_PawnNameColorUtility_PawnNameColorOf
{
    [HarmonyPatch(typeof(PawnNameColorUtility), nameof(PawnNameColorUtility.PawnNameColorOf))]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void ApplyCustomNameColor(Pawn pawn, ref Color __result)
    {
        if (!Settings.ModEnabled) return;
        // Mostly copied from vanilla version of the same method.
        // Ignore pawns that should have specific label colors
        if (pawn.MentalStateDef != null
            || pawn.IsPrisoner
            || (pawn.IsSlave && SlaveRebellionUtility.IsRebelling(pawn))
            || pawn.IsWildMan()
            || pawn.IsColonyMechRequiringMechanitor()
            || (pawn.Faction?.HostileTo(Faction.OfPlayer!) ?? false))
            return;
        if (LabelsTracker_WorldComponent.Instance?.GetExistingLabelData(pawn) is not { } labelData) return;

        __result = labelData.NameColor;
    }
}
