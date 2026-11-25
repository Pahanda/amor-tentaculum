using RimWorld;
using Verse;
using Verse.Sound;

public class Building_RecyclingPod : Building_TentaclePod
{
    protected override string PawnConfirmationKey => "ConfirmRecyclePawn";

    protected override string CancelConfirmationKey => "ConfirmCancelRecycling";

    protected override bool RequiresPawnConfirmation(Pawn pawn) => DestroyOccupantBrain;

    protected override bool RequiresCancelConfirmation => DestroyOccupantBrain;

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (!BeingTransportedOnGravship && DestroyOccupantBrain && Occupant != null)
        {
            KillOccupant();
        }

        base.DeSpawn(mode);
    }

    public override void EjectContents()
    {
        Pawn occupant = Occupant;
        if (occupant != null && def.building.subcoreScannerHediff != null)
        {
            occupant.health?.AddHediff(def.building.subcoreScannerHediff);
        }
        if (occupant != null && DestroyOccupantBrain)
        {
            KillOccupant();
        }

        base.EjectContents();
    }

    private void KillOccupant()
    {
        Pawn occupant = Occupant;
        occupant.forceNoDeathNotification = true;
        occupant.forceNoDeathNotification = false;
        ThoughtUtility.GiveThoughtsForPawnExecuted(occupant, null, PawnExecutionKind.Ripscanned);
        Messages.Message("MessagePawnKilledProcessingPod".Translate(occupant.Named("PAWN")), occupant, MessageTypeDefOf.NegativeHealthEvent);
    }

    protected override void CompleteProcessing()
    {
        Pawn occupant = Occupant;
        if (!DestroyOccupantBrain && occupant != null)
        {
            Messages.Message("MessageProcessingPodCompleted".Translate(occupant.Named("PAWN")), occupant, MessageTypeDefOf.PositiveEvent);
        }

        EjectContents();
        ThingDef outputDef = def.building.subcoreScannerOutputDef;
        if (outputDef != null)
        {
            GenPlace.TryPlaceThing(ThingMaker.MakeThing(outputDef), InteractionCell, Map, ThingPlaceMode.Near);
        }

        if (def.building.subcoreScannerComplete != null)
        {
            def.building.subcoreScannerComplete.PlayOneShot(this);
        }
    }
}
