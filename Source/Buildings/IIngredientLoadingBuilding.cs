using Verse;

public interface IIngredientLoadingBuilding
{
    bool NeedsIngredients { get; }

    int GetRequiredCountOf(ThingDef thingDef);

    bool CanAcceptIngredient(Thing thing);
}
