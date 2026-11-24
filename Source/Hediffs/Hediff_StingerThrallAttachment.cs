using RimWorld;
using Verse;

namespace AmorTentaculum
{
    public class Hediff_StingerThrallAttachment : Hediff_PawnAttachment
    {
        private const int ThrallDurationTicks = GenDate.TicksPerHour * 6;

        private Faction? previousFaction;
        private int ticksRemaining = ThrallDurationTicks;
        private int expirationTick = -1;
        private bool hostDied;

        public bool HasAttachedStinger => HasAttachedPawn;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            expirationTick = Find.TickManager.TicksGame + ThrallDurationTicks;
            previousFaction = pawn.Faction;
            pawn.SetFaction(Faction.OfPlayer);
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!pawn.Dead && !pawn.Destroyed &&
                expirationTick >= 0 &&
                Find.TickManager.TicksGame >= expirationTick)
            {
                pawn.Kill(null, this);
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            ReleaseAttachedPawns(stunReleasedPawns: false);
            if (!hostDied && !pawn.Dead && !pawn.Destroyed && previousFaction != null)
            {
                pawn.SetFaction(previousFaction);
            }
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff? culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            hostDied = true;
            ReleaseAttachedPawns(stunReleasedPawns: false);
            if (!HasAttachedStinger)
            {
                pawn.health.RemoveHediff(this);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref previousFaction, "previousFaction");
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", ThrallDurationTicks);
            Scribe_Values.Look(ref expirationTick, "expirationTick", -1);
            Scribe_Values.Look(ref hostDied, "hostDied");

            if (Scribe.mode == LoadSaveMode.PostLoadInit && expirationTick < 0)
            {
                expirationTick = Find.TickManager.TicksGame + ticksRemaining;
            }
        }

        protected override string AttachedPawnLabel => "Stinger";
    }
}
