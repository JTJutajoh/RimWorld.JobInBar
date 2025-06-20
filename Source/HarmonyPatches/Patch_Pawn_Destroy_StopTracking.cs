using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;

namespace JobInBar.HarmonyPatches;

/// <summary>
/// Patch that runs whenever a pawn is naturally destroyed so that <see cref="LabelsTracker_WorldComponent"/> stops tracking
/// them.
/// </summary>
[HarmonyPatch]
[HarmonyPatchCategory("StopTracking")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
// ReSharper disable once InconsistentNaming
internal static class Patch_Pawn_Destroy_StopTracking
{
    [HarmonyPatch(typeof(Pawn), "Destroy")]
    [HarmonyPostfix]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    static void StopTracking(Pawn __instance)
    {
        if (LabelsTracker_WorldComponent.Instance?.Remove(__instance) ?? false)
        {
            Log.Trace($"Removed pawn {__instance.NameShortColored} from cache.");
        }
    }
}
