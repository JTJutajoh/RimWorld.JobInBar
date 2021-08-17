using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using ColourPicker;
using UnityEngine;

namespace JobInBar
{
    public class PawnLabelCustomColors_WorldComponent : WorldComponent
    {
        private Dictionary<Pawn, Color> PawnNameColors = new Dictionary<Pawn, Color>();
        private Dictionary<Pawn, Color> PawnJobColors = new Dictionary<Pawn, Color>();
        private Dictionary<Pawn, bool> PawnShowJobLabels = new Dictionary<Pawn, bool>();

        public static PawnLabelCustomColors_WorldComponent instance;

        public PawnLabelCustomColors_WorldComponent(World world) : base(world)
        {
            PawnLabelCustomColors_WorldComponent.instance = this;
        }

        public bool HasCustomColor(Pawn pawn)
        {
            if (PawnNameColors.ContainsKey(pawn) || PawnJobColors.ContainsKey(pawn))
            {
                return true; // No entries, return with the above defaults
            }
            return false;
        }

        public bool GetDrawJobLabelFor(Pawn pawn)
        {
            if (PawnShowJobLabels.ContainsKey(pawn))
            {
                return PawnShowJobLabels[pawn];
            }
            else
            {
                // If there's nothing stored, assume it should be shown until hidden
                return Settings.DefaultShowSetting;
            }
        }

        public void GetJobLabelColorFor(Pawn pawn, out Color jobColor)
        {
            Color ColorColony = new Color(0.9f, 0.9f, 0.9f); // This is from PawnNameColorUtility.ColorColony
            //nameColor = PawnNameColorUtility.PawnNameColorOf(pawn);
            jobColor = Settings.defaultJobLabelColor;

            if (!HasCustomColor(pawn))
            {
                return;
            }
            //Log.Message("Found pawn in database");
            
            /*if (nameColor == ColorColony) // Only return the saved color if the pawn is supposed to have the default colonist label color
            {
                if (PawnNameColors.ContainsKey(pawn.Name))
                    nameColor = PawnNameColors[pawn.Name];
            }*/
            if (PawnJobColors.ContainsKey(pawn))
                jobColor = PawnJobColors[pawn];
        }

        public void SetNameLabelColorFor(Pawn pawn, Color nameColor)
        {
            PawnNameColors[pawn] = nameColor;
        }
        public void SetJobLabelColorFor(Pawn pawn, Color jobColor)
        {
            PawnJobColors[pawn] = jobColor;
        }

        public void SetDrawJobLabelFor(Pawn pawn, bool newVal)
        {
            PawnShowJobLabels[pawn] = newVal;
        }

        // temp lists for serialization
        private List<Pawn> plist = new List<Pawn>();
        private List<Pawn> plist2 = new List<Pawn>();
        private List<Color> clist = new List<Color>();
        private List<bool> blist = new List<bool>();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref PawnJobColors, "PawnJobColors", LookMode.Reference, LookMode.Value, ref plist, ref clist);
            Scribe_Collections.Look(ref PawnShowJobLabels, "PawnShowJobLabels", LookMode.Reference, LookMode.Value, ref plist2, ref blist);
        }
    }
}
