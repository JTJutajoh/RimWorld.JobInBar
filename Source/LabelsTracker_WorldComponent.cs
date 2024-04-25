using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using DarkColourPicker_Forked;
using UnityEngine;
using DarkLog;

namespace JobInBar
{
    /// <summary>
    /// "Singleton" class that keeps track of all pawns that the player has set a label for
    /// </summary>
    public class LabelsTracker_WorldComponent : WorldComponent
    {
        private Dictionary<Pawn, LabelData> TrackedPawns = new();

        public static LabelsTracker_WorldComponent instance;

        public LabelsTracker_WorldComponent(World world) : base(world)
        {
            LabelsTracker_WorldComponent.instance = this;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look<Pawn, LabelData>(ref TrackedPawns, "TrackedPawns", LookMode.Reference, LookMode.Deep);
        }
    }
}
