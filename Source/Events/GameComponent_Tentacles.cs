using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AmorTentaculum
{
    public class GameComponent_Tentacles : GameComponent
    {
        public static GameComponent_Tentacles Instance;

        public int lastBossArrival;
        public Thing summonerActivated;

        public Dictionary<BossWaveDef, int> lastWavesIndices = new Dictionary<BossWaveDef, int>();

        public GameComponent_Tentacles()
        {
            Init();
        }

        private void Init()
        {
            Instance = this;
        }

        public GameComponent_Tentacles(Game game)
        {
            Init();
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            Init();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            Init();
        }

        public int GetNextWaveIndex(BossWaveDef def)
        {
            if (!lastWavesIndices.TryGetValue(def, out int waveIndex))
            {
                waveIndex = 0;
            }
            else
            {
                waveIndex += 1;
            }
            if (waveIndex > def.waves.Count - 1)
            {
                if (waveIndex >= def.minWaveRepeatIndex)
                {
                    waveIndex = def.minWaveRepeatIndex;
                }
                else
                {
                    waveIndex = 0;
                }
            }
            return waveIndex;
        }

        public BossWave GetInsectWave(BossWaveDef def, int waveIndex)
        {
            return def.waves[waveIndex];
        }

        public BossWave GetNextBossWave(BossWaveDef def)
        {
            return def.waves[GetNextWaveIndex(def)];
        }

        private List<PlanetTile> tmpTiles = new List<PlanetTile>();

        public void AddInsectHive(Settlement hive)
        {
            var insectTerritory = insectTiles[hive] = new InsectTerritory();
            var hiveTile = hive.Tile;
            tmpTiles.Clear();
            var radius = 30 * insectTerritoryScale;
            var tilesCountMax = 50 * insectTerritoryScale;
            hiveTile.Layer.filler.FloodFill(hiveTile,
                curTile => Find.WorldGrid.ApproxDistanceInTiles(hiveTile, curTile) <= radius && !Find.World.Impassable(curTile),
                (curTile, dist) =>
                {
                    if (dist <= 1)
                    {
                        tmpTiles.Add(curTile);
                    }
                    else if (Rand.Chance((2f * insectTerritoryScale) / (float)dist))
                    {
                        var neighbors = new List<PlanetTile>();
                        Find.WorldGrid.GetTileNeighbors(curTile, neighbors);
                        if (neighbors.Any(x => tmpTiles.Contains(x)))
                        {
                            tmpTiles.Add(curTile);
                        }
                    }

                    if (tmpTiles.Count >= tilesCountMax)
                    {
                        return true;
                    }

                    return false;
                });
            for (int j = 0; j < tmpTiles.Count; j++)
            {
                insectTerritory.tiles.Add(tmpTiles[j]);
            }
            tmpTiles.Clear();
        }

        public float InfestationMtbDays(PlanetTile tile)
        {
            if (tile.LayerDef != PlanetLayerDefOf.Surface || !tile.Valid)
            {
                return -1f;
            }
            foreach (var insectData in insectTiles)
            {
                if (insectData.Value.tiles.Contains(tile))
                {
                    var dist = Find.WorldGrid.ApproxDistanceInTiles(tile, insectData.Key.Tile);
                    return dist * 5f;
                }
            }
            return -1f;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (insectTiles != null)
            {
                insectTiles.RemoveAll(x => x.Key is null || Find.World.worldObjects.Contains(x.Key) is false);
            }
            Scribe_Collections.Look(ref insectTiles, "insectTiles", LookMode.Reference, LookMode.Deep, ref settlementsTmp, ref insectTerritoriesTmp);
            Scribe_Values.Look(ref insectTerritoryScale, "insectTerritoryScale", 1f);
            Scribe_Values.Look(ref lastInsectoidBossArrival, "lastInsectoidBossArrival");
            Scribe_Collections.Look(ref lastWavesIndices, "lastWavesIndices", LookMode.Def, LookMode.Value);
            Scribe_References.Look(ref summonerActivated, "summonerActivated");
            Scribe_Values.Look(ref nextEmpressEvilFiringTick, "nextEmpressEvilFiringTick");
            Scribe_Deep.Look(ref hordeModeManager, "hordeModeManager");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                insectTiles ??= new Dictionary<Settlement, InsectTerritory>();
                lastWavesIndices ??= new Dictionary<BossWaveDef, int>();
                hordeModeManager ??= new HordeModeManager();
            }
        }

        private List<Settlement> settlementsTmp;
        private List<InsectTerritory> insectTerritoriesTmp;
    }
}