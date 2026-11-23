using RimWorld;
using Verse;

namespace AmorTentaculum
{
    public abstract class CompAbilityEffect_JumpWithPendingTarget :
        CompAbilityEffect,
        ICompAbilityEffectOnJumpCompleted
    {
        private Pawn? pendingTargetPawn;

        protected abstract string PendingTargetScribeKey { get; }

        internal void RememberTargetPawn(Pawn targetPawn)
        {
            pendingTargetPawn = targetPawn;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (target.Pawn is Pawn targetPawn)
            {
                RememberTargetPawn(targetPawn);
            }

            base.Apply(target, dest);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            // A save during flight must retain the target until the landing callback runs.
            Scribe_References.Look(ref pendingTargetPawn, PendingTargetScribeKey);
        }

        public abstract void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target);

        protected Pawn? ConsumePendingTargetPawn()
        {
            Pawn? targetPawn = pendingTargetPawn;
            pendingTargetPawn = null;
            return targetPawn;
        }
    }
}
