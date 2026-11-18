using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AmorTentaculum;

internal static class AmorHostPresenceUtility
{
    private static readonly HashSet<string> VisibleAmorHostHediffDefNames = new(StringComparer.Ordinal)
    {
        "AmorQueenHost",
        "AmorBaronessHost",
        "AmorNymphHost",
        "AmorBaronHost",
        "AmorRogueHost",
        "AmorKnightHost",
        "AmorServantHost"
    };

    public static bool IsEligibleColonist(Pawn pawn)
    {
        return pawn.IsColonist && !pawn.IsSlave && !pawn.IsPrisoner;
    }

    public static bool IsHost(Pawn pawn)
    {
        return pawn.health?.hediffSet?.hediffs?.Any(hediff => VisibleAmorHostHediffDefNames.Contains(hediff.def.defName)) == true;
    }

    private static bool AnyEligibleColonist(Pawn pawn, Func<Pawn, bool> predicate)
    {
        if (!IsEligibleColonist(pawn) || pawn.Faction == null)
        {
            return false;
        }

        return PawnsFinder.AllMaps_SpawnedPawnsInFaction(pawn.Faction)
            .Any(colonist => IsEligibleColonist(colonist) && predicate(colonist));
    }

    public static bool AnyHost(Pawn pawn)
    {
        return AnyEligibleColonist(pawn, IsHost);
    }

    public static bool AnyNonHost(Pawn pawn)
    {
        return AnyEligibleColonist(pawn, colonist => !IsHost(colonist));
    }
}

public sealed class ThoughtWorker_AmorHostsPresent : ThoughtWorker_Precept
{
    protected override ThoughtState ShouldHaveThought(Pawn pawn)
    {
        return AmorHostPresenceUtility.AnyHost(pawn)
            ? ThoughtState.ActiveAtStage(0)
            : ThoughtState.Inactive;
    }
}

public sealed class ThoughtWorker_AmorNoHostsPresent : ThoughtWorker_Precept
{
    protected override ThoughtState ShouldHaveThought(Pawn pawn)
    {
        return AmorHostPresenceUtility.AnyHost(pawn)
            ? ThoughtState.Inactive
            : ThoughtState.ActiveAtStage(0);
    }
}

public sealed class ThoughtWorker_AmorNonHostsPresent : ThoughtWorker_Precept
{
    protected override ThoughtState ShouldHaveThought(Pawn pawn)
    {
        return AmorHostPresenceUtility.AnyNonHost(pawn)
            ? ThoughtState.ActiveAtStage(0)
            : ThoughtState.Inactive;
    }
}

public sealed class ThoughtWorker_Precept_AmorHostExperience : ThoughtWorker_Precept
{
    protected override ThoughtState ShouldHaveThought(Pawn pawn)
    {
        if (def.hediff == null || def.stages.NullOrEmpty())
        {
            return ThoughtState.Inactive;
        }

        Hediff? hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(def.hediff);
        if (hediff?.def.stages == null)
        {
            return ThoughtState.Inactive;
        }

        return ThoughtState.ActiveAtStage(Math.Min(hediff.CurStageIndex, def.stages.Count - 1));
    }
}
