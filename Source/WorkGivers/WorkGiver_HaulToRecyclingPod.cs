using RimWorld;
using Verse;

public class WorkGiver_HaulToRecyclingPod :
    WorkGiver_HaulToIngredientContainer<Building_RecyclingPod>
{
    protected override ThingRequest BuildingRequest =>
        ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("Building_RecyclingPod"));
}
