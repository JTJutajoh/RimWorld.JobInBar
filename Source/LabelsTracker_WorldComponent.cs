using System;
using System.Collections.Generic;
using System.Linq;
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
        public Pawn? Pawn;
        public bool ShowBackstory = true;
        public Color BackstoryColor = Settings.DefaultJobLabelColor;
        
        public bool ShowRoyalTitle = true;
 
        public bool ShowIdeoRole = true;

        public LabelData()
        {
            LogPrefixed.Trace("Creating new empty label data");
            // Empty constructor for scribe.
            // This is only used when loading existing save files.
        }
        
        public LabelData(Pawn pawn)
        {
            LogPrefixed.Trace($"Creating new label data for pawn {pawn.Name}");
            this.Pawn = pawn;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref Pawn, "pawn");

            Scribe_Values.Look(ref ShowBackstory, "ShowBackstory", true);
            Scribe_Values.Look(ref BackstoryColor, "BackstoryColor", Settings.DefaultJobLabelColor);
            Scribe_Values.Look(ref ShowRoyalTitle, "ShowRoyalTitle", true);
            Scribe_Values.Look(ref ShowIdeoRole, "ShowIdeoRole", true);
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
                     LogPrefixed.Trace($"Pawn {pawn.Name} not tracked. Creating new label data for it.");
                     data = new LabelData(pawn);
                     _trackedPawns[pawn] = data;
                 }
                 return data;
             }
             private set => _trackedPawns[pawn] = value;
         }

        // Used for reassembling the dictionary of pawns and their label data.
        // This is necessary because the data is loaded from the save in multiple passes and the dictionary can only
        // be reassembled after all of the data has been loaded, so the lists must persist outside of each run of ExposeData
        private static List<Pawn>? _pawnList = null;
        private static List<LabelData>? _labelDataList = null;
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(
                ref _trackedPawns,
                "TrackedPawns",
                LookMode.Reference,
                LookMode.Deep,
                ref _pawnList,
                ref _labelDataList
            );
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Safety checks to sanitize null refs or mismatched data
                foreach (var pawn in this.TrackedPawns.Keys)
                {
                    if (pawn == null)
                    {
                        LogPrefixed.Warning("Null pawn in label tracker while loading.");
                        this.TrackedPawns.Remove(null!); // Remove null key if it somehow exists
                        continue;
                    }
                    if (this.TrackedPawns[pawn] == null)
                    {
                        LogPrefixed.Warning($"Null label data for pawn {pawn.Name}. Creating new label data.");
                        this.TrackedPawns[pawn] = new LabelData(pawn);
                        continue;
                    }
                    if (this.TrackedPawns[pawn].Pawn != pawn)
                    {
                        LogPrefixed.Warning($"Pawn {pawn.Name} has incorrect pawn reference in label data. Resetting.");
                        this.TrackedPawns[pawn].Pawn = pawn;
                    }
                }
                _pawnList = null;
                _labelDataList = null;
            }
        }
    }
}
