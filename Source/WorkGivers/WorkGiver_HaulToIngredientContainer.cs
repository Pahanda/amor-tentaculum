using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

public abstract class WorkGiver_HaulToIngredientContainer<TBuilding> : WorkGiver_Scanner
    where TBuilding : Thing, IIngredientLoadingBuilding
{
    protected abstract ThingRequest BuildingRequest { get; }

    public override ThingRequest PotentialWorkThingRequest => BuildingRequest;

    public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        if (thing is not TBuilding building ||
            !building.NeedsIngredients ||
            !pawn.CanReserve(thing, 1, -1, null, forced))
        {
            return false;
        }

        return FindIngredients(pawn, building).Thing != null;
    }

    public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        if (thing is not TBuilding building || !building.NeedsIngredients)
        {
            return null!;
        }

        ThingCount ingredient = FindIngredients(pawn, building);
        if (ingredient.Thing == null)
        {
            return null!;
        }

        Job job = HaulAIUtility.HaulToContainerJob(pawn, ingredient.Thing, thing);
        job.count = Mathf.Min(job.count, ingredient.Count);
        return job;
    }

    private static ThingCount FindIngredients(Pawn pawn, IIngredientLoadingBuilding building)
    {
        Thing ingredient = GenClosest.ClosestThingReachable(
            pawn.Position,
            pawn.Map,
            ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
            PathEndMode.ClosestTouch,
            TraverseParms.For(pawn),
            9999f,
            IsUsableIngredient);

        if (ingredient == null)
        {
            return default;
        }

        int requiredCount = building.GetRequiredCountOf(ingredient.def);
        return new ThingCount(ingredient, Mathf.Min(ingredient.stackCount, requiredCount));

        bool IsUsableIngredient(Thing thing)
        {
            return pawn.CanReserve(thing) &&
                !thing.IsForbidden(pawn) &&
                building.CanAcceptIngredient(thing);
        }
    }
}
