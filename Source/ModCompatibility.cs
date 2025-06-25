using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using JobInBar.HarmonyPatches;
using UnityEngine;

namespace JobInBar;

/// <summary>
/// Colony Groups compatibility. It replaces the whole colonist bar and manually calls methods of mods that modify the bar (like this one),
/// but it finds the method by name. So this class exists just as a redirect with a consistent name.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class ColonistBarColonistDrawer_DrawColonist_Patch
{
    [UsedImplicitly]
    public static void Postfix(Rect rect, Pawn colonist, Map pawnMap, bool highlight, bool reordering)
    {
        Patch_ColonistBarDrawer_DrawColonist_AddLabels.AddLabels(rect, colonist, pawnMap, highlight, reordering);
    }
}
