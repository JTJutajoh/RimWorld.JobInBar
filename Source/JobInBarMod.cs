using System;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace JobInBar
{
    public class JobInBarMod : Verse.Mod
    {
        public JobInBarMod(ModContentPack content) : base(content)
        {
            GetSettings<Settings>();

            Harmony harmony = new Harmony("Dark.JobInBar");

            harmony.PatchAll();
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
