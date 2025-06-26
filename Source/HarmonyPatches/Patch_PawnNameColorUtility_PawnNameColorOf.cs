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
        // Skip pawns that don't have LabelData cached
        if (LabelsTracker_WorldComponent.Instance?.GetExistingLabelData(pawn) is not { } labelData) return;
        // Mostly copied from vanilla version of the same method.
        // Ignore pawns that should have specific label colors
        if (pawn.MentalStateDef != null
            || pawn.IsPrisoner
            || (pawn.IsSlave && SlaveRebellionUtility.IsRebelling(pawn))
            || pawn.IsWildMan()
#if !(v1_1 || v1_2 || v1_3)
            || pawn.IsColonyMechRequiringMechanitor()
#endif
            || (pawn.Faction?.HostileTo(Faction.OfPlayer!) ?? false))
            return;

        __result = labelData.NameColor ?? __result;
    }
}
