using RimWorld;
using Verse;

public class WorkGiver_CarryToRecyclingPod : WorkGiver_CarryToBuilding
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("Building_RecyclingPod"));
}
