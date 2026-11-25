using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AmorTentaculum
{
    public class HediffCompProperties_ResourceDrain : HediffCompProperties
    {
        public GeneDef? resourceGene;
        public int drainIntervalTicks = 60;
        public float resourceDrain = 0.001f;
        public float resourceReserve = HostTentacleResource.OneTentacleResource;
        public string? stopMessage;

        public HediffCompProperties_ResourceDrain()
        {
            compClass = typeof(HediffComp_ResourceDrain);
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
                yield return error;

            if (resourceGene == null)
                yield return $"{nameof(resourceGene)} must be defined.";
            if (drainIntervalTicks <= 0)
                yield return $"{nameof(drainIntervalTicks)} must be greater than zero.";
            if (resourceDrain <= 0f || float.IsNaN(resourceDrain) || float.IsInfinity(resourceDrain))
                yield return $"{nameof(resourceDrain)} must be a positive, finite value.";
            if (resourceReserve < 0f || float.IsNaN(resourceReserve) || float.IsInfinity(resourceReserve))
                yield return $"{nameof(resourceReserve)} must be non-negative and finite.";
        }
    }

    public class HediffComp_ResourceDrain : HediffComp
    {
        private HediffCompProperties_ResourceDrain Props =>
            (HediffCompProperties_ResourceDrain)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            GeneDef resourceGene = Props.resourceGene
                ?? throw new InvalidOperationException($"{nameof(Props.resourceGene)} is not configured.");
            if (!HostTentacleResource.TryGet(Pawn, resourceGene, out Gene_Resource? resource, out IGeneResourceDrain? drain))
            {
                Log.Error($"{parent.def.defName} requires a valid Gene_Resource and IGeneResourceDrain on {Pawn.LabelShort}.");
                Pawn.health.RemoveHediff(parent);
                return;
            }

            float resourceAboveReserve = resource!.Value - Props.resourceReserve;
            if (resourceAboveReserve <= 0f)
            {
                StopEffect();
                return;
            }

            if (!Pawn.IsHashIntervalTick(Props.drainIntervalTicks))
                return;

            HostTentacleResource.Drain(drain!, Math.Min(Props.resourceDrain, resourceAboveReserve));
            if (resource.Value > Props.resourceReserve)
                return;

            StopEffect();
        }

        private void StopEffect()
        {
            Pawn.health.RemoveHediff(parent);
            if (Pawn.Faction == Faction.OfPlayer && !string.IsNullOrWhiteSpace(Props.stopMessage))
            {
                Messages.Message(
                    Props.stopMessage.Translate(Pawn.LabelShort),
                    new LookTargets(Pawn),
                    MessageTypeDefOf.NegativeEvent,
                    historical: false);
            }
        }
    }
}
