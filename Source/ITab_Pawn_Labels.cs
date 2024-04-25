using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using DarkLog;

namespace JobInBar
{
    public class ITab_Pawn_Labels : ITab
    {

        private Pawn PawnToShowInfoAbout
        {
            get
            {
                Pawn pawn = this.SelPawn ?? (base.SelThing as Corpse).InnerPawn;
                if (pawn == null)
                    LogPrefixed.Error("Label tab found no selected pawn to display.");
                return pawn;
            }
        }

        public override bool IsVisible => this.PawnToShowInfoAbout?.story != null;

        public ITab_Pawn_Labels()
        {
            this.labelKey = "TabLabels";
            this.tutorTag = "Labels";
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            this.size = LabelsCardUtility.LabelCardSize(this.PawnToShowInfoAbout) + new Vector2(8f, 8f) * 2f;
        }

        protected override void FillTab()
        {
            this.UpdateSize();

            Vector2 size = LabelsCardUtility.LabelCardSize(this.PawnToShowInfoAbout);

            LabelsCardUtility.DrawLabelsCard(new Rect(8f, 8f, size.x-17f, size.y-17f), this.PawnToShowInfoAbout);
        }
    }
}
