using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AmorTentaculum
{
    public class HediffCompProperties_DeathAfterTicks : HediffCompProperties
    {
        public int ticksToDeath = 6;

        public HediffCompProperties_DeathAfterTicks()
        {
            compClass = typeof(HediffComp_DeathAfterTicks);
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
                yield return error;

            if (ticksToDeath <= 0)
                yield return $"{nameof(ticksToDeath)} must be greater than zero.";
        }
    }

    public class HediffComp_DeathAfterTicks : HediffComp
    {
        private int ticksRemaining = -1;

        private HediffCompProperties_DeathAfterTicks Props =>
            (HediffCompProperties_DeathAfterTicks)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn.Dead || Pawn.Destroyed)
                return;

            if (ticksRemaining < 0)
                ticksRemaining = Props.ticksToDeath;

            ticksRemaining--;
            if (ticksRemaining <= 0)
                Pawn.Kill(null, parent);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", -1);
        }
    }
}
