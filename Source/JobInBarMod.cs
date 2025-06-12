using System;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using DarkLog;

namespace JobInBar
{
    public class JobInBarMod : Verse.Mod
    {
        public static JobInBarMod? Instance { get; private set; }

        public JobInBarMod(ModContentPack content) : base(content)
        {
            Instance = this;
            LogPrefixed.modInst = this;

            GetSettings<Settings>();

            var harmony = new Harmony("Dark.JobInBar");

            try
            {
                harmony.PatchAll();
            }
            catch (Exception e)
            {
                LogPrefixed.Exception(e, "Harmony patching", false);
            }

            LogPrefixed.Message("Patching complete");
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
                LogPrefixed.Exception(e, "Settings window", true);
                Widgets.Label(inRect, $"Error rendering settings window: {e.Message}, see log for stack trace. Please report this to the mod author.");
            }
        }

        public override string SettingsCategory()
        {
            return "JobInBar_SettingsCategory".Translate();
        }
    }
}
