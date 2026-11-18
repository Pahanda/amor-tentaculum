using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AmorTentaculum;

[StaticConstructorOnStartup]
internal static class AmorTentaculumIdeologyPatcher
{
    static AmorTentaculumIdeologyPatcher()
    {
        if (ModsConfig.IdeologyActive)
        {
            new Harmony("pahandabear.retentacled.ideology").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}

[HarmonyPatch(typeof(TaleUtility), "Notify_PawnDied")]
internal static class AmorTentaculumPawnDiedPatch
{
    [HarmonyPostfix]
    private static void Postfix(Pawn victim, DamageInfo? dinfo)
    {
        if (victim?.def?.HasModExtension<AmorTentaculumRaceExtension>() != true)
        {
            return;
        }

        Pawn? killer = dinfo?.Instigator as Pawn;
        if (killer == null)
        {
            return;
        }

        HistoryEventDef? eventDef = DefDatabase<HistoryEventDef>.GetNamedSilentFail("AmorTentaculumCreatureKilled");
        if (eventDef == null)
        {
            Log.Error("Amor Tentaculum ideology history event is missing.");
            return;
        }

        Find.HistoryEventsManager.RecordEvent(
            new HistoryEvent(eventDef, new SignalArgs(killer.Named(HistoryEventArgsNames.Doer))),
            true);
    }
}
