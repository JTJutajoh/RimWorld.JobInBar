using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace JobInBar
{
    [UsedImplicitly]
    public class JobInBarMod : Mod
    {
        public static JobInBarMod? Instance { get; private set; }

        public JobInBarMod(ModContentPack content) : base(content)
        {
            Instance = this;
            // ReSharper disable once RedundantArgumentDefaultValue
            Log.Initialize(this, "cyan");

            GetSettings<Settings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            try
            {
                GetSettings<Settings>().DoWindowContents(inRect);
            }
            catch (Exception e)
            {
                Log.Exception(e, "Error drawing mod settings window.", true);
                Widgets.DrawBoxSolid(inRect, new Color(0, 0, 0, 0.5f));
                var errorRect = inRect.MiddlePart(0.4f, 0.25f);
                Widgets.DrawBoxSolidWithOutline(errorRect, Widgets.WindowBGFillColor, Color.red, 5);
                Widgets.Label(errorRect.ContractedBy(16f),
                    $"Error rendering settings window:\n\"{e.Message}\", see log for stack trace.\nPlease report this to the mod author.");
            }
        }

        public override string SettingsCategory()
        {
            return "JobInBar_SettingsCategory".Translate();
        }
    }

    [StaticConstructorOnStartup]
    internal static class LoadHarmony
    {
        internal static readonly Harmony Harmony;

        static LoadHarmony()
        {
            Harmony = new Harmony(JobInBarMod.Instance!.Content.PackageId);

#if DEBUG
            // Harmony.DEBUG = true; // For debugging transpilers. DO NOT uncomment this unless you need it!
#endif

            Log.Message("Running Harmony patches...");

            try
            {
                Patch_Vanilla();
            }
            catch (Exception e)
            {
                Log.Exception(e,
                    "Error patching vanilla. This likely means either the wrong game version or a hard incompatibility with another mod.");
            }

            Log.Message("Harmony patching complete");
        }

        private static void Patch_Vanilla()
        {
            //TODO: Replace PatchAll() with categorized patching
            Harmony.PatchAll();
        }
    }
}