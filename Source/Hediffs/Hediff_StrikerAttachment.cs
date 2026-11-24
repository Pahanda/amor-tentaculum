using RimWorld;
using Verse;

namespace AmorTentaculum
{
    public class Hediff_StrikerAttachment : Hediff_PawnAttachment
    {
        public bool HasAttachedStriker => HasAttachedPawn;

        public override void PostRemoved()
        {
            base.PostRemoved();
            ReleaseAttachedPawns(stunReleasedPawns: true);
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff? culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            // The hediff can remain on the corpse, so release its Strikers at death rather than waiting for removal.
            ReleaseAttachedPawns(stunReleasedPawns: false);
            if (!HasAttachedStriker)
            {
                // Keep the corpse clean once every stored Striker has been released.
                pawn.health.RemoveHediff(this);
            }
        }

        protected override string AttachedPawnLabel => "Striker";

        protected override void NotifyPawnReleased(Pawn releasedPawn, bool stunReleasedPawn, bool releasedOnMap)
        {
            StartLeapCooldown(releasedPawn);
            if (stunReleasedPawn && releasedOnMap)
            {
                // 600 ticks is 10 seconds at RimWorld's 60 ticks per second.
                releasedPawn.stances?.stunner?.StunFor(600, pawn, addBattleLog: false, showMote: true);
            }
        }

        private void StartLeapCooldown(Pawn striker)
        {
            if (striker.Dead || striker.Destroyed)
            {
                return;
            }

            AbilityDef abilityDef = DefDatabase<AbilityDef>.GetNamed(StrikerLeapCooldown.AbilityDefName);
            Ability? ability = striker.abilities?.GetAbility(abilityDef);
            if (ability == null)
            {
                Log.Error(
                    $"Released Striker {striker} does not have ability {StrikerLeapCooldown.AbilityDefName}.");
                return;
            }

            StrikerLeapCooldown.Start(ability, ability.def.cooldownTicksRange.RandomInRange);
        }
    }
}
