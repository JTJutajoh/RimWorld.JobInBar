using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using UnityEngine;
using DarkLog;

namespace JobInBar
{
     /// <summary>
     /// Struct storing all of the user-set data for one pawn's label.
     /// </summary>
     public class LabelData : IExposable
     {
         public Pawn Pawn;
 public bool ShowBackstory = true;
         public Color BackstoryColor = Settings.DefaultJobLabelColor;
 
         public bool ShowRoyalTitle = true;
 
         public bool ShowIdeoRole = true;
 
         public LabelData(Pawn pawn)
         {
             LogPrefixed.Debug($"Creating new label data for pawn {pawn.Name}");
             this.Pawn = pawn;
         }
 
         public void ExposeData()
         {
             Scribe_References.Look(ref Pawn, "pawn");
 
             Scribe_Values.Look(ref ShowBackstory, "ShowBackstory");
             Scribe_Values.Look(ref BackstoryColor, "BackstoryColor");
             Scribe_Values.Look(ref ShowRoyalTitle, "ShowRoyalTitle");
             Scribe_Values.Look(ref ShowIdeoRole, "ShowIdeoRole");
         }
     }


     /// <summary>
     /// Singleton class that keeps track of all pawns that the player has set a label for<br />
     /// Tracked pawns are accessed via indexing. If the pawn is not tracked, indexing it will start tracking it.
     /// </summary>
     public class LabelsTracker_WorldComponent : WorldComponent
     {
         private Dictionary<Pawn, LabelData> _trackedPawns = new();
         public Dictionary<Pawn, LabelData> TrackedPawns => _trackedPawns;

         public static LabelsTracker_WorldComponent? Instance;

         public LabelsTracker_WorldComponent(World world) : base(world)
         {
             LabelsTracker_WorldComponent.Instance = this;
         }

         public LabelData this[Pawn pawn]
         {
             get
             {
                 if (!_trackedPawns.TryGetValue(pawn, out var data))
                 {
                     LogPrefixed.Debug($"Pawn {pawn.Name} not tracked. Creating new label data for it.");
                     data = new LabelData(pawn);
                     _trackedPawns[pawn] = data;
                 }
                 return data;
             }
             set => _trackedPawns[pawn] = value;
         }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look<Pawn, LabelData>(
                ref _trackedPawns,
                "TrackedPawns",
                LookMode.Reference,
                LookMode.Deep
            );
        }
    }
}
