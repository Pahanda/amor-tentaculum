using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace AmorTentaculum
{
    // This is based on the Vanilla Factions Expanded Insectoids 2 "thumper" building, but slightly more generic and modular.
    public class BossWave
    {
        public List<PawnKindDefCount> pawns;

        public string GetPawnList()
        {
            var entries = new List<string>();
            foreach (var entry in pawns)
            {
                entries.Add(GenLabel.BestKindLabel(entry.kindDef, Gender.None).CapitalizeFirst()
                    + " x" + entry.count);
            }
            return entries.ToLineList("  - ");
        }
    }

    public class BossWaveDef : Def
    {
        public List<BossWave> waves;
        public int minWaveRepeatIndex;
    }

    public class CompProperties_BossSummoner : CompProperties_UseEffect
    {
        public BossWaveDef wave;
        public bool requiresOrbit = false;
        public CompProperties_BossSummoner()
        {
            compClass = typeof(CompBossSummoner);
        }
    }

    public class CompBossSummoner : CompUseEffect
    {
        public bool activated;
        public int summonDuration;
        public bool waveSpawned;
        private Faction tentacleFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("Tentacles"));
        // TODO: Add some graphics and sound here.


        public CompProperties_BossSummoner Props => (CompProperties_BossSummoner)props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            activated = true;
            GameComponent_Tentacles.Instance.summonerActivated = parent;
            summonDuration = (int)(new FloatRange(45f, 75f).RandomInRange * 60f);
            var wave = GameComponent_Tentacles.Instance.GetNextBossWave(Props.wave);
            Find.LetterStack.ReceiveLetter("BossSummon_LetterLabelWaveSummoned".Translate(
                GenLabel.BestKindLabel(wave.pawns.First().kindDef, Gender.None).CapitalizeFirst()),
                "BossSummon_LetterWaveSummoned".Translate(tentacleFaction.NameColored),
                LetterDefOf.NeutralEvent, parent);
        }

        public override TaggedString ConfirmMessage(Pawn p)
        {
            var wave = GameComponent_Tentacles.Instance.GetNextBossWave(Props.wave);
            return "BossSummon_Warning".Translate(parent.Label, wave.GetPawnList().Named("PAWNS"));
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (GameComponent_Tentacles.Instance.summonerActivated == this.parent)
            {
                GameComponent_Tentacles.Instance.summonerActivated = null;
            }
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            if (Props.requiresOrbit && !p.Map.Biome.inVacuum)
            {
                return "BossSummon_RequiresOrbit".Translate();
            }
            if (activated)
            {
                return "BossSummon_AlreadyActivated".Translate();
            }
            if (GameComponent_Tentacles.Instance.summonerActivated is Thing thing && thing.Destroyed is false)
            {
                return "BossSummon_OtherSummonActive".Translate();
            }
            var lastBossArrival = GameComponent_Tentacles.Instance.lastBossArrival;
            if (lastBossArrival != 0 && Find.TickManager.TicksGame <= lastBossArrival + GenDate.TicksPerDay * 2)
            {
                return "BossSummon_CooldownActive".Translate(((lastBossArrival + GenDate.TicksPerDay * 2) - Find.TickManager.TicksGame).ToStringTicksToPeriod());
            }
            return base.CanBeUsedBy(p);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (activated)
            {
                summonDuration--;
                if (summonDuration <= 0)
                {
                    if (waveSpawned is false)
                    {
                        var wave = GameComponent_Tentacles.Instance.GetNextBossWave(Props.wave);
                        SpawnWave(wave);
                        waveSpawned = true;
                    }
                }
            }
        }

        public void SpawnWave(BossWave wave)
        {
            GameComponent_Tentacles.Instance.lastBossArrival = Find.TickManager.TicksGame;
            GameComponent_Tentacles.Instance.lastWavesIndices[Props.wave] = Props.wave.waves.IndexOf(wave);

            Map map = Find.CurrentMap;
            
            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Walkable(map) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out IntVec3 spawnCell))
            {
                Log.Error("ERROR: Could not find clear edge cell to spawn boss raid!");
                return;
            }

            List<Pawn> bossSquad = new List<Pawn>();
            foreach (PawnKindDefCount pawnBatch in wave.pawns)
            {
                for (int i = 0; i < pawnBatch.count; i++)
                {
                    PawnGenerationRequest request = new PawnGenerationRequest(
                        pawnBatch.kindDef,
                        tentacleFaction,
                        PawnGenerationContext.NonPlayer,
                        mustBeCapableOfViolence: true
                        );

                    Pawn tentacle = PawnGenerator.GeneratePawn(request);
                    GenSpawn.Spawn(tentacle, spawnCell, map);

                    Lord backgroundLord = tentacle.GetLord();
                    

                    bossSquad.Add(tentacle);


                }
            }

            LordJob lordJob = new LordJob_AssaultColony(tentacleFaction);
            LordMaker.MakeNewLord(tentacleFaction, lordJob, map, bossSquad);

            // Clear nonhostile jobs.
            foreach (Pawn tentacle in bossSquad)
            {
                DutyDef customDuty = DefDatabase<DutyDef>.GetNamed("AssaultColony", false);
                tentacle.mindState.duty = new PawnDuty(customDuty);

                tentacle.jobs?.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
                tentacle.mindState?.priorityWork.Clear();
            }

            var enemiesJustSpawned = map.mapPawns.AllPawnsSpawned.Where(p => p.Faction == tentacleFaction && p.HostileTo(Find.FactionManager.OfPlayer)).ToList();
            var lookTargets = new LookTargets(enemiesJustSpawned);

            var waveLeaderKind = wave.pawns.First().kindDef;
            Find.LetterStack.ReceiveLetter(
                "BossSummon_LetterLabelWaveArrived".Translate(GenLabel.BestKindLabel(waveLeaderKind, Gender.None).CapitalizeFirst()),
                "BossSummon_LetterWaveArrived".Translate(tentacleFaction.NameColored, wave.GetPawnList()),
                LetterDefOf.Bossgroup,
                lookTargets);

            parent.GetComp<CompExplosive>().StartWick();
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref activated, "activated");
            Scribe_Values.Look(ref summonDuration, "summonDuration");
            Scribe_Values.Look(ref waveSpawned, "waveSpawned");
        }
    }
}