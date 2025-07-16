using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;

namespace JobInBar.HarmonyPatches;

[HarmonyPatch]
[HarmonyPatchCategory("ExposeData")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class Patch_Pawn_ExposeData
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExposeData))]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void ExposeData(Pawn __instance)
    {
        Scribe.EnterNode("JobInBar");

        LabelData? labelData = null;
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            if (__instance.TryGetLabelData(out labelData))
            {
                Log.Trace($"Saving {__instance.LabelCap} label data");
                Scribe_Deep.Look(ref labelData, "LabelData");
            }
        }
        else
        {
            Scribe_Deep.Look(ref labelData, "LabelData");
            if (Scribe.mode == LoadSaveMode.LoadingVars && labelData != null)
            {
                Log.Trace($"Loading {__instance.LabelCap} label data");
                __instance.SetLabelData(labelData);
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit && labelData != null && labelData.Pawn != __instance)
            {
                Log.Warning($"Pawn \"{__instance.LabelCap}\" loaded mismatched label data for \"{labelData.Pawn?.LabelCap ?? "null"}\" (scribe mode: {Scribe.mode}).");
            }
        }

        Scribe.ExitNode();
    }
}
