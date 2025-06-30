using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;

namespace JobInBar.HarmonyPatches
{
    [HarmonyPatch]
    [HarmonyPatchCategory("AddLabels")]
    [SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
    // ReSharper disable once InconsistentNaming
    internal static class Patch_ColonistBarDrawer_DrawColonist_AddLabels
    {
        [HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawColonist")]
        [HarmonyPostfix]
        [UsedImplicitly]
        public static void AddLabels(Rect rect, Pawn colonist, Map pawnMap, bool highlight, bool reordering)
        {
            if (!Settings.ModEnabled) return;

            var bar = Find.ColonistBar;
            if (bar is null)
            {
                Log.Error("Error adding mod labels, ColonistBar was null. Is there another mod replacing it?");
                return;
            }

            var barHeight = 4f * bar.Scale; // from Core

            var pos = new Vector2(rect.center.x, rect.yMax - barHeight + Settings.JobLabelVerticalOffset + 14f);

            try
            {
                var cache = PawnCache.GetOrCache(colonist);
                // Log.Trace($"Now: {DateTime.UtcNow.Ticks} - {cache.LastCached} / 10000 = {(DateTime.UtcNow.Ticks - cache.LastCached) / 10000} > {Settings.CacheRefreshRate} + {cache.TickOffset}");
                if (cache.NeedsRecache)
                {
                    cache = cache.Recache();
                }

                LabelDrawer.DrawLabels(colonist, cache, pos, bar, rect,
                    rect.width + bar.SpaceBetweenColonistsHorizontal);
            }
            catch (Exception e)
            {
                Log.Exception(e, extraMessage: "Top-level uncaught exception", once: true);
            }
        }

        [HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.Notify_RecachedEntries))]
        [HarmonyPostfix]
        [UsedImplicitly]
        public static void Notify_RecachedEntries()
        {
            PawnCache.Clear();
        }
    }
}
