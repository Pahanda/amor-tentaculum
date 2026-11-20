using HarmonyLib;
using RimWorld;
using Verse;

namespace AmorTentaculum;

[StaticConstructorOnStartup]
internal static class AmorTentaculumEnragePatcher
{
    static AmorTentaculumEnragePatcher()
    {
        new Harmony("pahandabear.retentacled.enrage")
            .CreateClassProcessor(typeof(AmorTentaculumEnrageOnDeathPatch))
            .Patch();
    }
}

[HarmonyPatch(typeof(TaleUtility), "Notify_PawnDied")]
internal static class AmorTentaculumEnrageOnDeathPatch
{
    private const float EnrageRadius = 40f;

    [HarmonyPostfix]
    private static void Postfix(Pawn victim)
    {
        if (victim?.Map == null || victim.Faction == null)
            return;

        string survivorRace;
        string enrageHediff;
        switch (victim.def.defName)
        {
            case "TentacleDefender":
                survivorRace = "TentacleValkyrie";
                enrageHediff = "ValkyrieEnrage";
                break;
            case "TentacleValkyrie":
                survivorRace = "TentacleDefender";
                enrageHediff = "DefenderEnrage";
                break;
            default:
                return;
        }

        HediffDef? enrageDef = DefDatabase<HediffDef>.GetNamedSilentFail(enrageHediff);
        if (enrageDef == null)
        {
            Log.Error($"Amor Tentaculum enrage hediff '{enrageHediff}' is missing.");
            return;
        }

        foreach (Pawn pawn in victim.Map.mapPawns.AllPawnsSpawned)
        {
            if (pawn == victim || pawn.Dead || pawn.health == null ||
                pawn.def.defName != survivorRace || pawn.Faction != victim.Faction ||
                pawn.Position.DistanceTo(victim.Position) > EnrageRadius)
            {
                continue;
            }

            Hediff? existing = pawn.health.hediffSet.GetFirstHediffOfDef(enrageDef);
            if (existing != null)
                pawn.health.RemoveHediff(existing);

            pawn.health.AddHediff(enrageDef);
        }
    }
}
