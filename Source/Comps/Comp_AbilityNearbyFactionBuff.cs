using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AmorTentaculum
{
    public class CompProperties_AbilityNearbyFactionBuff : CompProperties_AbilityEffect
    {
        public HediffDef? selfBuff;
        public HediffDef? friendlyBuff;
        public EffecterDef? activationEffecter;
        public float radius;

        public CompProperties_AbilityNearbyFactionBuff()
        {
            compClass = typeof(CompAbilityEffect_NearbyFactionBuff);
        }

        public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
                yield return error;

            if (selfBuff == null)
                yield return $"{nameof(selfBuff)} must be defined.";
            if (friendlyBuff == null)
                yield return $"{nameof(friendlyBuff)} must be defined.";
            if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius))
                yield return $"{nameof(radius)} must be a positive, finite value.";
        }
    }

    public class CompAbilityEffect_NearbyFactionBuff : CompAbilityEffect
    {
        public new CompProperties_AbilityNearbyFactionBuff Props => (CompProperties_AbilityNearbyFactionBuff)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            HediffDef selfBuff = Props.selfBuff
                ?? throw new InvalidOperationException($"{nameof(Props.selfBuff)} is not configured.");
            HediffDef friendlyBuff = Props.friendlyBuff
                ?? throw new InvalidOperationException($"{nameof(Props.friendlyBuff)} is not configured.");
            Map map = caster.Map
                ?? throw new InvalidOperationException("The nearby faction buff ability requires the caster to be on a map.");

            caster.health.AddHediff(selfBuff);

            if (Props.activationEffecter != null)
            {
                Effecter effecter = new Effecter(Props.activationEffecter);
                effecter.Trigger(new TargetInfo(caster.Position, map), TargetInfo.Invalid);
                effecter.Cleanup();
            }

            if (caster.Faction == null)
                return;

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == caster || pawn.Dead || pawn.health == null || pawn.Faction != caster.Faction)
                    continue;

                if (pawn.Position.DistanceTo(caster.Position) <= Props.radius)
                    pawn.health.AddHediff(friendlyBuff);
            }
        }
    }
}
