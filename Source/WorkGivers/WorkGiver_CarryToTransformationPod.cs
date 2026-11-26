using RimWorld;
using Verse;

public class WorkGiver_CarryToTransformationPod : WorkGiver_CarryToBuilding
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("Building_TransformationPod"));
}
