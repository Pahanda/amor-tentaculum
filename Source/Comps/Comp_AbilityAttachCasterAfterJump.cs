using System.Linq;
using RimWorld;
using Verse;

namespace AmorTentaculum
{
    public class StrikerAttachmentOption
    {
        public HediffDef? attachmentHediff;
        public string attachmentBodyPart = string.Empty;
        public List<HediffDef> targetHasAnyOfHediffs = new();
    }

    public class CompProperties_AbilityAttachCasterAfterJump : CompProperties_AbilityEffect
    {
        public HediffDef? attachmentHediff;
        public string attachmentBodyPart = string.Empty;
        public List<HediffDef> targetHasAnyOfHediffs = new();
        public List<StrikerAttachmentOption> attachmentOptions = new();

        public CompProperties_AbilityAttachCasterAfterJump()
        {
            compClass = typeof(CompAbilityEffect_AttachCasterAfterJump);
            availableWhenTargetIsWounded = true;
        }
    }

    internal static class StrikerLeapCooldown
    {
        public const string AbilityDefName = "GrabberLeap";

        public static void Start(Ability ability, int ticks)
        {
            ability.StartCooldown(ticks);
        }
    }

    public class CompAbilityEffect_AttachCasterAfterJump : CompAbilityEffect_JumpWithPendingTarget
    {
        private const float MinimumStripApparelChance = 0.03f;
        private const float MinimumGrabSuccessChance = 0.33f;
        private const float MaximumGrabFailureChance = 1f - MinimumGrabSuccessChance;
        private CompProperties_AbilityAttachCasterAfterJump AttachProps =>
            (CompProperties_AbilityAttachCasterAfterJump)base.Props;

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn? targetPawn = target.Pawn;
            if (targetPawn == null || !base.CanApplyOn(target, dest))
            {
                return false;
            }

            if (!HasRequiredTargetHediff(targetPawn))
            {
                RejectPlayerCast(targetPawn, "StrikerLeapRejectMissingAnatomy");
                return false;
            }

            if (GetAvailableAttachments(targetPawn).Count == 0)
            {
                RejectPlayerCast(targetPawn, "StrikerLeapRejectNoEligibleBodyPart");
                return false;
            }

            RememberTargetPawn(targetPawn);
            return true;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn? targetPawn = target.Pawn;
            return targetPawn != null &&
                base.Valid(target, throwMessages) &&
                GetAvailableAttachments(targetPawn).Count > 0;
        }

        public override void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
        {
            // Attach the Striker only after its leap has landed successfully.
            Pawn caster = parent.pawn;
            Pawn? capturedTargetPawn = ConsumePendingTargetPawn();
            // Vanilla jump callbacks can receive only the landing cell, not the original Pawn.
            Pawn? targetPawn = capturedTargetPawn ?? target.Pawn;

            if (caster.Destroyed || caster.Dead)
            {
                return;
            }

            StrikerLeapCooldown.Start(parent, parent.def.cooldownTicksRange.RandomInRange);

            if (targetPawn == null || !targetPawn.RaceProps.Humanlike || targetPawn.Destroyed || targetPawn.Dead)
            {
                Log.Error(
                    $"Striker leap {parent.def.defName} completed without a valid pawn target " +
                    $"(captured={capturedTargetPawn?.LabelShort ?? "none"}, callback={target}).");
                return;
            }

            List<AvailableAttachment> availableAttachments = GetAvailableAttachments(targetPawn);
            if (availableAttachments.Count == 0)
            {
                return;
            }

            AvailableAttachment selectedAttachment = SelectBestAttachment(targetPawn, availableAttachments);
            HediffDef? attachmentHediff = selectedAttachment.Hediff;
            BodyPartRecord bodyPart = selectedAttachment.BodyPart;

            if (attachmentHediff == null)
            {
                return;
            }

            // This roll is separate from the apparel-drop rolls below.
            if (!Rand.Chance(GetGrabFailureChance(targetPawn, bodyPart)))
            {
                Hediff hediff = HediffMaker.MakeHediff(attachmentHediff, targetPawn, bodyPart);
                if (hediff is not Hediff_StrikerAttachment attachment)
                {
                    Log.Error($"Hediff {attachmentHediff.defName} is not a Hediff_StrikerAttachment.");
                    return;
                }

                targetPawn.health.AddHediff(attachment, bodyPart);
                if (!attachment.TryAttach(caster))
                {
                    targetPawn.health.RemoveHediff(attachment);
                }
            }

            RollForApparelDrop(targetPawn, bodyPart);
        }

        protected override string PendingTargetScribeKey => "pendingLeapTargetPawn";

        private List<AvailableAttachment> GetAvailableAttachments(Pawn targetPawn)
        {
            if (!HasRequiredTargetHediff(targetPawn))
            {
                return new List<AvailableAttachment>();
            }

            List<StrikerAttachmentOption> options = AttachProps.attachmentOptions;
            if (options.Count == 0)
            {
                options = new List<StrikerAttachmentOption>
                {
                    new()
                    {
                        attachmentHediff = AttachProps.attachmentHediff,
                        attachmentBodyPart = AttachProps.attachmentBodyPart
                    }
                };
            }

            List<AvailableAttachment> available = new();
            foreach (StrikerAttachmentOption option in options)
            {
                if (option.attachmentHediff == null ||
                    string.IsNullOrWhiteSpace(option.attachmentBodyPart) ||
                    !HasAnyRequiredHediff(targetPawn, option.targetHasAnyOfHediffs))
                {
                    continue;
                }

                BodyPartRecord? bodyPart = targetPawn.RaceProps.body.AllParts.FirstOrDefault(
                    part => part.def.defName == option.attachmentBodyPart);
                if (bodyPart == null || HasAttachmentOnPart(targetPawn, bodyPart))
                {
                    continue;
                }

                available.Add(new AvailableAttachment(option.attachmentHediff, bodyPart));
            }

            return available;
        }

        private AvailableAttachment SelectBestAttachment(
            Pawn targetPawn,
            List<AvailableAttachment> availableAttachments)
        {
            // Different hediff options can share a body part, so score each part only once.
            var candidatesByPart = availableAttachments
                .GroupBy(option => option.BodyPart)
                .Select(group => new
                {
                    Options = group.ToList(),
                    SuccessChance = GetGrabSuccessChance(targetPawn, group.Key)
                })
                .ToList();

            float highestSuccessChance = candidatesByPart.Max(candidate => candidate.SuccessChance);
            List<List<AvailableAttachment>> bestPartOptions = candidatesByPart
                .Where(candidate => Math.Abs(candidate.SuccessChance - highestSuccessChance) < 0.000001f)
                .Select(candidate => candidate.Options)
                .ToList();

            List<AvailableAttachment> selectedPartOptions =
                bestPartOptions[Rand.Range(0, bestPartOptions.Count)];
            return selectedPartOptions[Rand.Range(0, selectedPartOptions.Count)];
        }

        private float GetGrabSuccessChance(Pawn targetPawn, BodyPartRecord bodyPart)
        {
            return 1f - GetGrabFailureChance(targetPawn, bodyPart);
        }

        private float GetGrabFailureChance(Pawn targetPawn, BodyPartRecord bodyPart)
        {
            float chanceNoApparelBlocksGrab = 1f;
            // Combine exposed garments as independent failure chances, then apply the overall cap.
            foreach (Apparel apparel in GetExposedApparel(targetPawn, bodyPart))
            {
                chanceNoApparelBlocksGrab *= 1f - GetArmorAdjustedGrabFailureChance(apparel);
            }

            return Math.Min(MaximumGrabFailureChance, 1f - chanceNoApparelBlocksGrab);
        }

        private void RollForApparelDrop(Pawn targetPawn, BodyPartRecord bodyPart)
        {
            if (!targetPawn.Spawned || targetPawn.apparel == null)
            {
                return;
            }

            List<Apparel> apparelToDrop = GetExposedApparel(targetPawn, bodyPart)
                .Where(apparel => Rand.Chance(GetArmorAdjustedChance(apparel)))
                .ToList();

            if (apparelToDrop.Count > 0)
            {
                Predicate<Apparel> apparelSelector = apparel => apparelToDrop.Contains(apparel);
                targetPawn.apparel.DropAll(
                    targetPawn.Position,
                    forbid: false,
                    dropLocked: false,
                    selector: apparelSelector);
            }
        }

        private static List<Apparel> GetExposedApparel(Pawn targetPawn, BodyPartRecord bodyPart)
        {
            if (!targetPawn.Spawned || targetPawn.apparel == null)
            {
                return new List<Apparel>();
            }

            List<BodyPartRecord> targetBodyParts = GetTargetBodyParts(targetPawn, bodyPart.def.defName);
            List<Apparel> wornApparel = targetPawn.apparel.WornApparel.ToList();
            return wornApparel
                .Where(apparel => IsExposedOnTargetBodyPart(apparel, wornApparel, targetBodyParts))
                .ToList();
        }

        private static float GetArmorAdjustedGrabFailureChance(Apparel apparel)
        {
            float bluntProtection = ClampArmorRating(apparel.GetStatValue(StatDefOf.ArmorRating_Blunt));
            float sharpProtection = ClampArmorRating(apparel.GetStatValue(StatDefOf.ArmorRating_Sharp));
            float heatProtection = ClampArmorRating(apparel.GetStatValue(StatDefOf.ArmorRating_Heat));
            float weightedProtection = (sharpProtection + bluntProtection + heatProtection) / 2f;
            if (ModsConfig.OdysseyActive)
            {
                float vacuumResistance = ClampArmorRating(apparel.GetStatValue(StatDefOf.VacuumResistance));
                weightedProtection += vacuumResistance;
            }

            return Math.Min(MaximumGrabFailureChance, weightedProtection * MaximumGrabFailureChance);
        }

        private bool HasRequiredTargetHediff(Pawn targetPawn)
        {
            return HasAnyRequiredHediff(targetPawn, AttachProps.targetHasAnyOfHediffs);
        }

        private static bool HasAnyRequiredHediff(Pawn targetPawn, List<HediffDef> requiredHediffs)
        {
            return requiredHediffs.Count == 0 ||
                targetPawn.health.hediffSet.hediffs.Any(
                    hediff => requiredHediffs.Contains(hediff.def));
        }

        private void RejectPlayerCast(Pawn targetPawn, string reasonKey)
        {
            Pawn caster = parent.pawn;
            if (caster.Drafted && Find.Selector.IsSelected(caster))
            {
                Messages.Message(
                    "StrikerLeapRejectInput".Translate(
                        parent.def.label,
                        targetPawn.LabelShort,
                        reasonKey.Translate()),
                    new LookTargets(caster, targetPawn),
                    MessageTypeDefOf.RejectInput,
                    historical: false);
            }
        }

        private static bool HasAttachmentOnPart(Pawn targetPawn, BodyPartRecord bodyPart)
        {
            return targetPawn.health.hediffSet.hediffs
                .OfType<Hediff_StrikerAttachment>()
                .Any(existingAttachment => existingAttachment.Part == bodyPart);
        }

        private static List<BodyPartRecord> GetTargetBodyParts(Pawn targetPawn, string attachmentBodyPart)
        {
            // Each grab type can only strip clothing covering its specific target region.
            return targetPawn.RaceProps.body.AllParts
                .Where(part => IsPartInAttachmentRegion(part, attachmentBodyPart))
                .ToList();
        }

        private static bool IsPartInAttachmentRegion(BodyPartRecord part, string attachmentBodyPart)
        {
            if (attachmentBodyPart == "Jaw")
            {
                return part.def.defName == "Ear" ||
                    part.def.defName == "Nose" ||
                    part.def.defName == "Eye" ||
                    part.def.defName == "Jaw";
            }

            return part.def.defName == "Genitals" ||
                part.def.defName == "Leg" &&
                (part.woundAnchorTag == "LeftLeg" || part.woundAnchorTag == "RightLeg");
        }

        private static bool IsExposedOnTargetBodyPart(
            Apparel apparel,
            List<Apparel> wornApparel,
            List<BodyPartRecord> targetBodyParts)
        {
            // A higher layer blocks this garment only on the body parts they both cover.
            int apparelDrawOrder = apparel.def.apparel.LastLayer.drawOrder;
            return targetBodyParts.Any(bodyPart =>
                apparel.def.apparel.CoversBodyPart(bodyPart) &&
                !wornApparel.Any(otherApparel =>
                    otherApparel != apparel &&
                    otherApparel.def.apparel.CoversBodyPart(bodyPart) &&
                    otherApparel.def.apparel.LastLayer.drawOrder > apparelDrawOrder));
        }

        private static float GetArmorAdjustedChance(Apparel apparel)
        {
            float bluntProtection = ClampArmorRating(apparel.GetStatValue(StatDefOf.ArmorRating_Blunt));
            float sharpProtection = ClampArmorRating(apparel.GetStatValue(StatDefOf.ArmorRating_Sharp));
            float heatProtection = ClampArmorRating(apparel.GetStatValue(StatDefOf.ArmorRating_Heat));
            float weightedProtection = (sharpProtection + bluntProtection + heatProtection) / 2f;
            float chance = MinimumStripApparelChance + (1f - MinimumStripApparelChance) * (1f - weightedProtection);
            return Math.Max(MinimumStripApparelChance, chance);
        }

        private static float ClampArmorRating(float armorRating)
        {
            return Math.Max(0f, Math.Min(1f, armorRating));
        }

        private readonly struct AvailableAttachment
        {
            public AvailableAttachment(HediffDef hediff, BodyPartRecord bodyPart)
            {
                Hediff = hediff;
                BodyPart = bodyPart;
            }

            public HediffDef Hediff { get; }
            public BodyPartRecord BodyPart { get; }
        }
    }

    public class Verb_CastAbilityJumpWithTarget : Verb_CastAbilityJump
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            // Preserve the Pawn before the vanilla jump order converts it to a landing cell.
            Pawn? targetPawn = target.Pawn;
            if (targetPawn != null)
            {
                CompAbilityEffect_JumpWithPendingTarget? attachComp =
                    ability.CompOfType<CompAbilityEffect_JumpWithPendingTarget>();
                attachComp?.RememberTargetPawn(targetPawn);
            }

            base.OrderForceTarget(target);
        }
    }
}
