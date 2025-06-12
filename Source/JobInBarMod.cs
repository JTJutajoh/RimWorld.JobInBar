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
