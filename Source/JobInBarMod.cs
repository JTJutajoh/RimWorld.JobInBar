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
        private static JobInBarMod instance;
        public static JobInBarMod Instance => instance;
        public JobInBarMod(ModContentPack content) : base(content)
        {
            instance = this;
            LogPrefixed.modInst = this;

            GetSettings<Settings>();

            var harmony = new Harmony("Dark.JobInBar");

            harmony.PatchAll();

            LogPrefixed.Message("Patching complete");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            GetSettings<Settings>().DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "JobInBar_SettingsCategory".Translate();
        }
    }
}
