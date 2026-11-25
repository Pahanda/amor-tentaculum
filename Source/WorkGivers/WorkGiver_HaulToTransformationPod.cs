using RimWorld;
using Verse;

public class WorkGiver_HaulToTransformationPod :
    WorkGiver_HaulToIngredientContainer<Building_TransformationPod>
{
    protected override ThingRequest BuildingRequest =>
        ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("Building_TransformationPod"));
}
