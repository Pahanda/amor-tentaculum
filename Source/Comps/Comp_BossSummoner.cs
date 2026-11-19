using RimWorld;
using UnityEngine;
using Verse;
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
        public GraphicData topGraphic;
        public BossWaveDef wave;
        public bool needsOrbit = false;
        public CompProperties_BossSummoner()
        {
            compClass = typeof(CompBossSummoner);
        }
    }

    public class CompBossSummoner : CompUseEffect
    {
        public bool activated;
        public int thumpDuration;
        private Graphic _topGraphic;
        public float topGraphicOffset;
        public bool infestationSpawned;
        public bool isLowering;
        public SoundDef soundThumping;
        public Graphic TopGraphic => _topGraphic ??= Props.topGraphic.Graphic;


        public CompProperties_BossSummoner Props => (CompProperties_BossSummoner)props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            activated = true;
            GameComponent_Tentacles.Instance.summonerActivated = parent;
            thumpDuration = (int)(new FloatRange(30f, 90f).RandomInRange * 60f);
            var wave = GameComponent_Tentacles.Instance.GetNextBossWave(Props.wave);
            Find.LetterStack.ReceiveLetter("VFEI_LetterLabelInsectWaveSummoned".Translate(
                GenLabel.BestKindLabel(wave.insects.First().kindDef, Gender.None).CapitalizeFirst()),
                "VFEI_LetterInsectWaveSummoned".Translate(Faction.OfInsects.NameColored),
                LetterDefOf.NeutralEvent, parent);
        }

        public override TaggedString ConfirmMessage(Pawn p)
        {
            var wave = GameComponent_Tentacles.Instance.GetNextBossWave(Props.wave);
            return "VFEI_InsectSummonWarning".Translate(parent.Label, wave.GetPawnList().Named("PAWNS"));
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (GameComponent_Tentacles.Instance.summonerActivated == this.parent)
            {
                GameComponent_Tentacles.Instance.summonerActivated = null;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            TopGraphic.Draw(parent.DrawPos + new Vector3(0, 0.01f, topGraphicOffset), parent.Rotation, parent);
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            if (Props.needsOrbit && !p.Map.Biome.inVacuum)
            {
                return "VFEI_NotInOrbit".Translate();
            }
            if (activated)
            {
                return "VFEI_AlreadyActivated".Translate();
            }
            if (GameComponent_Tentacles.Instance.summonerActivated is Thing thing && thing.Destroyed is false)
            {
                return "VFEI_OtherThumperIsActive".Translate();
            }
            var lastBossArrival = GameComponent_Tentacles.Instance.lastBossArrival;
            if (lastBossArrival != 0 && Find.TickManager.TicksGame <= lastBossArrival + GenDate.TicksPerDay * 2)
            {
                return "VFEI_OnCooldown".Translate(((lastBossArrival + GenDate.TicksPerDay * 2) - Find.TickManager.TicksGame).ToStringTicksToPeriod());
            }
            return base.CanBeUsedBy(p);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (activated)
            {
                var speedMult = isLowering ? 0.1f : 0.02f;
                var speed = 0.1f * speedMult;
                thumpDuration--;
                if (thumpDuration <= 0)
                {
                    MoveDown(speed);
                    if (infestationSpawned is false)
                    {
                        var wave = GameComponent_Tentacles.Instance.GetNextBossWave(Props.wave);
                        SpawnInfestation(wave);
                        infestationSpawned = true;
                    }
                }
                else
                {
                    if (isLowering)
                    {
                        MoveDown(speed);
                    }
                    else
                    {
                        MoveUp(speed);
                    }
                }
            }
        }

        public void SpawnInfestation(BossWave wave)
        {
            GameComponent_Tentacles.Instance.lastBossArrival = Find.TickManager.TicksGame;
            GameComponent_Tentacles.Instance.lastWavesIndices[Props.wave] = Props.wave.waves.IndexOf(wave);
            var hives = new List<LargeTunnelHiveSpawner>();
            var hiveCount = wave.pawns.First().count;
            IntVec3 intVec = parent.Map.AllCells.Where(x => x.Walkable(parent.Map) && x.Fogged(parent.Map) is false
            && x.GetTerrain(parent.Map) is TerrainDef def && def.IsWater is false).RandomElement();
            IncidentWorker_LargeInfestation.SpawnTunnels(hiveCount, parent.Map, intVec, hives);
            var dict = new Dictionary<PawnKindDef, List<List<PawnKindDef>>>();
            foreach (var pawnCount in wave.pawns)
            {
                var list = new List<PawnKindDef>();
                for (int i = 0; i < pawnCount.count; i++)
                {
                    list.Add(pawnCount.kindDef);
                }
                dict[pawnCount.kindDef] = list.Split(hives.Count);
            }

            for (var i = 0; i < hives.Count; i++)
            {
                var hive = hives[i];
                hive.spawnHive = false;
                hive.otherHives = hives.Where(x => x != hive).ToList();
                hive.pawnsToSpawn = new List<PawnKindDef>();
                foreach (var pawnCount in wave.pawns)
                {
                    var list = dict[pawnCount.kindDef];
                    if (i < list.Count)
                    {
                        hive.pawnsToSpawn.AddRange(list[i]);
                    }
                }
            }

            var insectLeaderKind = wave.pawns.First().kindDef;
            Find.LetterStack.ReceiveLetter("VFEI_LetterLabelInsectWaveArrived".Translate(
                GenLabel.BestKindLabel(insectLeaderKind, Gender.None).CapitalizeFirst()),
                "VFEI_LetterInsectWaveArrived".Translate(Faction.OfInsects.NameColored, wave.GetPawnList()),
                VFEI_DefOf.VFEI_InsectWaveArrived, new TargetInfo(intVec, parent.Map));
            parent.GetComp<CompExplosive>().StartWick();
        }

        private void MoveUp(float speed)
        {
            topGraphicOffset = Mathf.Min(topGraphicOffset + speed, 0.05f);
            if (topGraphicOffset >= 0.05f)
            {
                isLowering = true;
            }
        }

        private void MoveDown(float speed)
        {
            var oldValue = topGraphicOffset;
            topGraphicOffset = Mathf.Max(topGraphicOffset - speed, 0);
            if (topGraphicOffset <= 0 && oldValue > 0)
            {
                isLowering = false;
                FleckMaker.Static(parent.DrawPos, parent.Map, FleckDefOf.PsycastAreaEffect, 5f);
                if (soundThumping is null)
                {
                    soundThumping = new List<SoundDef> {VFEI_DefOf.VFEI2_InsectThumperOne,
                        VFEI_DefOf.VFEI2_InsectThumperTwo, VFEI_DefOf.VFEI2_InsectThumperThree,
                        VFEI_DefOf.VFEI2_InsectThumperFour}.RandomElement();
                }
                soundThumping.PlayOneShot(parent);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref activated, "activated");
            Scribe_Values.Look(ref thumpDuration, "thumpDuration");
            Scribe_Values.Look(ref topGraphicOffset, "topGraphicOffset");
            Scribe_Values.Look(ref isLowering, "isLowering");
            Scribe_Values.Look(ref infestationSpawned, "infestationSpawned");
            Scribe_Defs.Look(ref soundThumping, "soundThumping");
        }
    }
}