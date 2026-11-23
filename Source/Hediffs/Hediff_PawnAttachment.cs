using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AmorTentaculum
{
    public abstract class Hediff_PawnAttachment : HediffWithComps, IThingHolder
    {
        protected ThingOwner<Pawn>? innerContainer;

        protected bool HasAttachedPawn => innerContainer != null && innerContainer.Count > 0;

        protected abstract string AttachedPawnLabel { get; }

        protected Hediff_PawnAttachment()
        {
            innerContainer = new ThingOwner<Pawn>(this, LookMode.Reference, removeContentsIfDestroyed: false);
        }

        public bool TryAttach(Pawn attachedPawn)
        {
            if (attachedPawn.Destroyed || attachedPawn.Dead)
            {
                return false;
            }

            ThingOwner<Pawn> container = InnerContainer;
            if (container.Count > 0)
            {
                return container.InnerListForReading.Contains(attachedPawn);
            }

            bool wasSpawned = attachedPawn.Spawned;
            Map? map = attachedPawn.Map;
            IntVec3 position = attachedPawn.Position;
            if (wasSpawned)
            {
                attachedPawn.DeSpawn(DestroyMode.WillReplace);
            }

            if (!container.TryAdd(attachedPawn))
            {
                if (wasSpawned && map != null && !attachedPawn.Destroyed)
                {
                    GenSpawn.Spawn(attachedPawn, position, map);
                }

                Log.Error($"Unable to store {AttachedPawnLabel} {attachedPawn} in attachment hediff {def.defName}.");
                return false;
            }

            return true;
        }

        protected void ReleaseAttachedPawns(bool stunReleasedPawns)
        {
            ThingOwner<Pawn>? container = innerContainer;
            if (container == null)
            {
                return;
            }

            while (container.Count > 0)
            {
                Pawn attachedPawn = container.InnerListForReading[0];
                if (!TryReleasePawn(container, attachedPawn, stunReleasedPawns))
                {
                    return;
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                innerContainer ??= new ThingOwner<Pawn>(this, LookMode.Reference, removeContentsIfDestroyed: false);
                innerContainer.removeContentsIfDestroyed = false;
            }
        }

        public void GetChildHolders(System.Collections.Generic.List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return InnerContainer;
        }

        public IThingHolder ParentHolder => pawn.ParentHolder;

        protected virtual void NotifyPawnReleased(Pawn releasedPawn, bool stunReleasedPawn, bool releasedOnMap)
        {
        }

        private ThingOwner<Pawn> InnerContainer =>
            innerContainer ??= new ThingOwner<Pawn>(this, LookMode.Reference, removeContentsIfDestroyed: false);

        private bool TryReleasePawn(
            ThingOwner<Pawn> container,
            Pawn attachedPawn,
            bool stunReleasedPawn)
        {
            Map? map = pawn.MapHeld;
            if (map == null || !pawn.PositionHeld.IsValid)
            {
                return TryReleasePawnToWorld(container, attachedPawn, stunReleasedPawn);
            }

            if (!container.TryDrop(attachedPawn, pawn.PositionHeld, map, ThingPlaceMode.Near, 1, out Pawn releasedPawn))
            {
                Log.Error($"Unable to drop {AttachedPawnLabel} {attachedPawn} from {def.defName} near {pawn}.");
                return false;
            }

            NotifyPawnReleased(releasedPawn, stunReleasedPawn, releasedOnMap: true);
            return true;
        }

        private bool TryReleasePawnToWorld(
            ThingOwner<Pawn> container,
            Pawn attachedPawn,
            bool stunReleasedPawn)
        {
            if (!container.Remove(attachedPawn))
            {
                Log.Error($"Unable to release {AttachedPawnLabel} {attachedPawn} from {def.defName}.");
                return false;
            }

            Find.WorldPawns.PassToWorld(attachedPawn, PawnDiscardDecideMode.KeepForever);
            NotifyPawnReleased(attachedPawn, stunReleasedPawn, releasedOnMap: false);
            return true;
        }
    }
}
