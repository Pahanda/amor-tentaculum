using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace AmorTentaculum;

public class Building_ParalysisWormTrap : Building
{
    private const float TriggerRadius = 1.5f;
    private const int WormCount = 4;

    protected override void Tick()
    {
        base.Tick();

        if (this.IsHashIntervalTick(60) &&
            GenRadial.RadialDistinctThingsAround(Position, Map, TriggerRadius, true)
                .OfType<Pawn>()
                .Any(pawn => pawn.HostileTo(Faction)))
        {
            ReleaseWorms();
        }
    }

    private void ReleaseWorms()
    {
        Map map = Map;
        IntVec3 trapPosition = Position;
        Faction? faction = Faction ?? RimWorld.Faction.OfPlayer;
        PawnKindDef pawnKind = DefDatabase<PawnKindDef>.GetNamed("TentacleParalysisWorm");

        DefDatabase<SoundDef>.GetNamed("Explosion_FirefoamPopper")
            .PlayOneShot(new TargetInfo(trapPosition, map));
        Destroy(DestroyMode.Vanish);

        for (int i = 0; i < WormCount; i++)
        {
            Pawn worm = PawnGenerator.GeneratePawn(
                new PawnGenerationRequest(pawnKind, faction, PawnGenerationContext.NonPlayer));

            if (!CellFinder.TryRandomClosewalkCellNear(
                    trapPosition,
                    map,
                    2,
                    out IntVec3 spawnPosition,
                    cell => cell.Standable(map)))
            {
                spawnPosition = trapPosition;
            }

            GenSpawn.Spawn(worm, spawnPosition, map, WipeMode.VanishOrMoveAside);
        }
    }
}
