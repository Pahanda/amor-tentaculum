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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastBossArrival, "lastInsectoidBossArrival");
            Scribe_Collections.Look(ref lastWavesIndices, "lastWavesIndices", LookMode.Def, LookMode.Value);
            Scribe_References.Look(ref summonerActivated, "summonerActivated");
        }
    }
}