using System.Linq;
using RimWorld;
using Verse;

namespace AmorTentaculum
{
    public class CompProperties_AbilityStingerLatch : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityStingerLatch()
        {
            compClass = typeof(CompAbilityEffect_StingerLatch);
            availableWhenTargetIsWounded = true;
        }
    }

    public class CompAbilityEffect_StingerLatch : CompAbilityEffect_JumpWithPendingTarget
    {
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn? targetPawn = target.Pawn;
            return targetPawn != null &&
                targetPawn.Downed &&
                HasAvailableHead(targetPawn) &&
                base.CanApplyOn(target, dest);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn? targetPawn = target.Pawn;
            return targetPawn != null &&
                targetPawn.Downed &&
                HasAvailableHead(targetPawn) &&
                base.Valid(target, throwMessages);
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            Pawn? targetPawn = target.Pawn;
            return targetPawn != null &&
                targetPawn.Downed &&
                HasAvailableHead(targetPawn);
        }

        public override void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
        {
            Pawn stinger = parent.pawn;
            Pawn? targetPawn = ConsumePendingTargetPawn() ?? target.Pawn;

            if (stinger.Destroyed || stinger.Dead)
            {
                return;
            }

            parent.StartCooldown(parent.def.cooldownTicksRange.RandomInRange);
            if (targetPawn == null ||
                targetPawn.Destroyed ||
                targetPawn.Dead ||
                !targetPawn.Downed ||
                !HasAvailableHead(targetPawn))
            {
                return;
            }

            BodyPartRecord head = GetAvailableHead(targetPawn)!;
            Hediff hediff = HediffMaker.MakeHediff(
                StingerHediffDefOf.StingerThrallAttachment,
                targetPawn,
                head);
            if (hediff is not Hediff_StingerThrallAttachment attachment)
            {
                Log.Error(
                    $"Hediff {StingerHediffDefOf.StingerThrallAttachment.defName} is not a " +
                    $"{nameof(Hediff_StingerThrallAttachment)}.");
                return;
            }

            targetPawn.health.AddHediff(attachment, head);
            if (!attachment.TryAttach(stinger))
            {
                targetPawn.health.RemoveHediff(attachment);
            }
        }

        protected override string PendingTargetScribeKey => "pendingStingerTargetPawn";

        private static bool HasAvailableHead(Pawn targetPawn)
        {
            return GetAvailableHead(targetPawn) != null &&
                !targetPawn.health.hediffSet.hediffs.OfType<Hediff_StingerThrallAttachment>().Any();
        }

        private static BodyPartRecord? GetAvailableHead(Pawn targetPawn)
        {
            return targetPawn.health.hediffSet.GetNotMissingParts()
                .FirstOrDefault(part => part.def == BodyPartDefOf.Head);
        }
    }

    [DefOf]
    public static class StingerHediffDefOf
    {
        public static HediffDef StingerThrallAttachment = null!;

        static StingerHediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StingerHediffDefOf));
        }
    }
}
