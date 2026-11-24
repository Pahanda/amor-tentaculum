using System;
using RimWorld;
using Verse;

namespace AmorTentaculum
{
    internal static class HostTentacleResource
    {
        public const float OneTentacleResource = 0.01f;
        private const float WholeTentacleRoundingTolerance = 0.0001f;
        private const float ResourceRoundingTolerance = 0.000001f;

        public static bool TryGet(Pawn pawn, GeneDef resourceGene, out Gene_Resource? resource, out IGeneResourceDrain? drain)
        {
            Gene? gene = pawn.genes?.GetGene(resourceGene);
            resource = gene as Gene_Resource;
            drain = gene as IGeneResourceDrain;
            return resource != null && drain != null;
        }

        public static void Drain(IGeneResourceDrain drain, float amount)
        {
            GeneResourceDrainUtility.OffsetResource(drain, -amount);
        }

        public static void RoundDownToWholeTentacles(Gene_Resource resource, IGeneResourceDrain drain)
        {
            float tentacleCount = resource.Value / OneTentacleResource;
            float roundedTentacleCount = (float)Math.Floor(tentacleCount + WholeTentacleRoundingTolerance);
            float excessResource = resource.Value - roundedTentacleCount * OneTentacleResource;
            if (excessResource > ResourceRoundingTolerance)
                Drain(drain, excessResource);
        }
    }
}
