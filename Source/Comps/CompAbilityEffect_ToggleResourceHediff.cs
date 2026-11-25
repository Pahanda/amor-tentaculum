using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AmorTentaculum
{
    public class CompProperties_AbilityToggleResourceHediff : CompProperties_AbilityEffect
    {
        public HediffDef? activeHediff;
        public GeneDef? resourceGene;
        public float resourceReserve = HostTentacleResource.OneTentacleResource;
        public string? noResourceMessage;

        public CompProperties_AbilityToggleResourceHediff()
        {
            compClass = typeof(CompAbilityEffect_ToggleResourceHediff);
        }

        public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
                yield return error;

            if (activeHediff == null)
                yield return $"{nameof(activeHediff)} must be defined.";
            if (resourceGene == null)
                yield return $"{nameof(resourceGene)} must be defined.";
            if (resourceReserve < 0f || float.IsNaN(resourceReserve) || float.IsInfinity(resourceReserve))
                yield return $"{nameof(resourceReserve)} must be non-negative and finite.";
        }
    }

    public class CompAbilityEffect_ToggleResourceHediff : CompAbilityEffect
    {
        private new CompProperties_AbilityToggleResourceHediff Props =>
            (CompProperties_AbilityToggleResourceHediff)props;

        public override bool CanCast =>
            base.CanCast && HasMinimumResource(parent.pawn);

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return HasMinimumResource(parent.pawn);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn pawn = parent.pawn;
            HediffDef activeHediffDef = Props.activeHediff
                ?? throw new InvalidOperationException($"{nameof(Props.activeHediff)} is not configured.");
            Hediff? activeHediff = pawn.health.hediffSet.GetFirstHediffOfDef(activeHediffDef);
            if (activeHediff != null)
            {
                GeneDef resourceGene = Props.resourceGene
                    ?? throw new InvalidOperationException($"{nameof(Props.resourceGene)} is not configured.");
                if (HostTentacleResource.TryGet(pawn, resourceGene, out Gene_Resource? resource, out IGeneResourceDrain? drain))
                {
                    HostTentacleResource.RoundDownToWholeTentacles(resource!, drain!);
                }
                else
                {
                    Log.Error($"{activeHediffDef.defName} could not round down stored tentacles on {pawn.LabelShort} because its resource gene is invalid.");
                }

                pawn.health.RemoveHediff(activeHediff);
                return;
            }

            if (!HasMinimumResource(pawn))
            {
                if (!string.IsNullOrWhiteSpace(Props.noResourceMessage))
                {
                    Messages.Message(
                        Props.noResourceMessage.Translate(pawn.LabelShort),
                        new LookTargets(pawn),
                        MessageTypeDefOf.RejectInput,
                        historical: false);
                }
                return;
            }

            pawn.health.AddHediff(activeHediffDef);
        }

        private bool HasMinimumResource(Pawn pawn)
        {
            GeneDef resourceGene = Props.resourceGene
                ?? throw new InvalidOperationException($"{nameof(Props.resourceGene)} is not configured.");

            return HostTentacleResource.TryGet(pawn, resourceGene, out Gene_Resource? resource, out _) &&
                resource!.Value > Props.resourceReserve;
        }
    }
}
