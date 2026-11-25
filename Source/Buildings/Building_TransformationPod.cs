using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;

public class TransformationPodRecipe
{
    public ThingDef? larva;
    public PawnKindDef? pawnKind;
}

public class TransformationPodExtension : DefModExtension
{
    public List<TransformationPodRecipe> recipes = new();
}

public class Building_TransformationPod : Building_TentaclePod
{
    private string? selectedPawnKindDefName;

    private TransformationPodExtension Settings =>
        def.GetModExtension<TransformationPodExtension>()
        ?? throw new System.InvalidOperationException($"{def.defName} requires a {nameof(TransformationPodExtension)}.");

    private int LoadedLarvaCount =>
        Settings.recipes.Sum(recipe => recipe.larva == null ? 0 : innerContainer.TotalStackCountOfDef(recipe.larva));

    private TransformationPodRecipe? SelectedRecipe
    {
        get
        {
            return Settings.recipes.FirstOrDefault(recipe =>
                recipe.pawnKind?.defName == selectedPawnKindDefName);
        }
    }

    public override bool AllRequiredIngredientsLoaded =>
        base.AllRequiredIngredientsLoaded &&
        SelectedRecipe?.larva != null &&
        LoadedLarvaCount == 1 &&
        innerContainer.TotalStackCountOfDef(SelectedRecipe.larva) == 1;

    public override int GetRequiredCountOf(ThingDef thingDef)
    {
        if (Settings.recipes.Any(recipe => recipe.larva == thingDef))
        {
            return SelectedRecipe?.larva == thingDef
                ? System.Math.Max(0, 1 - innerContainer.TotalStackCountOfDef(thingDef))
                : 0;
        }

        return base.GetRequiredCountOf(thingDef);
    }

    protected override bool ShouldShowStartProcessingGizmos =>
        !ScannerInitialized || SelectedRecipe == null;

    protected override IEnumerable<Gizmo> GetStartProcessingGizmos()
    {
        foreach (TransformationPodRecipe recipe in Settings.recipes)
        {
            if (recipe.larva == null || recipe.pawnKind == null)
            {
                continue;
            }

            TransformationPodRecipe selectedRecipe = recipe;
            Command_Action command = new Command_Action();
            command.defaultLabel = "TransformationPodChooseBoss".Translate(selectedRecipe.pawnKind.LabelCap);
            command.defaultDesc = "TransformationPodChooseBossDesc".Translate(
                selectedRecipe.pawnKind.LabelCap,
                selectedRecipe.larva.LabelCap);
            command.icon = InitScannerIcon.Texture;
            command.action = () =>
            {
                selectedPawnKindDefName = selectedRecipe.pawnKind.defName;
                BeginProcessing();
            };
            command.activateSound = SoundDefOf.Tick_Tiny;
            yield return command;
        }
    }

    public override AcceptanceReport CanAcceptPawn(Pawn selPawn)
    {
        AcceptanceReport report = base.CanAcceptPawn(selPawn);
        if (report.Accepted || State != SubcoreScannerState.WaitingForIngredients || !base.AllRequiredIngredientsLoaded)
        {
            return report;
        }

        TransformationPodRecipe? recipe = SelectedRecipe;
        if (recipe?.larva == null)
        {
            return false;
        }

        return "TransformationPodMissingSelectedBossLarva".Translate(
            recipe.larva.LabelCap,
            innerContainer.TotalStackCountOfDef(recipe.larva));
    }

    public override void EjectContents()
    {
        base.EjectContents(dropAllContents: true);
    }

    protected override string PawnConfirmationKey => "ConfirmTransformationPodPawn";

    protected override string GetPawnConfirmationText(Pawn pawn) =>
        PawnConfirmationKey.Translate(
            pawn.Named("PAWN"),
            SelectedRecipe?.pawnKind?.LabelCap ?? string.Empty);

    protected override bool RequiresPawnConfirmation(Pawn pawn) => true;

    protected override bool RequiresCancelConfirmation => false;

    protected override void AppendIngredientsList(StringBuilder sb)
    {
        base.AppendIngredientsList(sb);
        TransformationPodRecipe? recipe = SelectedRecipe;
        if (recipe?.larva == null)
        {
            return;
        }

        sb.AppendInNewLine("TransformationPodSelectedBoss".Translate(recipe.pawnKind?.LabelCap ?? string.Empty));
        int loadedLarvaCount = innerContainer.TotalStackCountOfDef(recipe.larva);
        sb.AppendInNewLine("TransformationPodSelectedLarvaRequirement".Translate(
            recipe.larva.LabelCap,
            loadedLarvaCount));
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new StringBuilder(base.GetInspectString());
        PawnKindDef? selectedPawnKind = SelectedRecipe?.pawnKind;
        if (ScannerInitialized && State != SubcoreScannerState.WaitingForIngredients && selectedPawnKind != null)
        {
            sb.AppendLineIfNotEmpty();
            sb.Append("TransformationPodSelectedBoss".Translate(selectedPawnKind.LabelCap));
        }

        return sb.ToString();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref selectedPawnKindDefName, "selectedPawnKindDefName");
    }

    protected override void CompleteProcessing()
    {
        TransformationPodRecipe? recipe = SelectedRecipe;
        Pawn? occupant = Occupant;
        if (recipe?.pawnKind == null || occupant == null)
        {
            Log.Error($"{def.defName} reached completion without exactly one valid boss larva and an occupant; ejecting its contents.");
            EjectContents();
            return;
        }

        Pawn transformedTentacle = PawnGenerator.GeneratePawn(recipe.pawnKind, Faction.OfPlayer);
        occupant.Kill(null);
        base.EjectContents(
            dropAllContents: false,
            destroyRemainingContents: true,
            ejectPawnAndCorpse: false);
        GenSpawn.Spawn(transformedTentacle, InteractionCell, Map);
        Messages.Message(
            "MessageTransformationPodCompleted".Translate(
                occupant.Named("PAWN"),
                recipe.pawnKind.LabelCap),
            new LookTargets(transformedTentacle),
            MessageTypeDefOf.PositiveEvent);

        if (def.building.subcoreScannerComplete != null)
        {
            def.building.subcoreScannerComplete.PlayOneShot(this);
        }
    }
}
