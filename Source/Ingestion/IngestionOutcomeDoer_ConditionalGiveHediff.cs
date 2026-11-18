using RimWorld;
using rjw.MainTab.Icon;
using Verse;

namespace AmorTentaculum
{

    // Started life as a copy of the GiveHediff IngestionOutcomeDoer. Stripped out drug stuff (since this is for
    // other things) and bolted on a bunch of conditional logic to help determine what to do with an ingested
    // item based on what hediffs the pawn already has.
    //
    // Primarily intended for transformation eggs, but could certainly have other applications.
    public class IngestionOutcomeDoer_ConditionalGiveHediff : IngestionOutcomeDoer
    {
        public List<HediffSetConditional> conditionalSets;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {

            foreach (HediffSetConditional conditionalSet in conditionalSets)
            {
                int hediffMatched = 0;
                int hediffMatches = conditionalSet.hediffConditionals.Count();
                int sexMatched = 0;
                int sexMatches = conditionalSet.genderConditionals.Count();

                Log.Message("Checking gender set of size "+sexMatches);

                // Check for sex matches.
                String pawnSex = rjw.GenderHelper.GetSex(pawn).ToString();
                foreach (String checkSex in conditionalSet.genderConditionals)
                {
                    if (checkSex == pawnSex)
                    {
                        // Log.Message("Match found!");
                        sexMatched++;
                    }
                }

                bool sexBreakout = false;
                switch (conditionalSet.genderComparator)
                {
                    case "any":
                        if (sexMatched < 1)
                        {
                            sexBreakout = true;
                        }
                        break;
                    case "none":
                        if (sexMatched > 0)
                        {
                            sexBreakout = true;
                        }
                        break;
                    default:
                        break;
                }
                if (sexBreakout)
                {
                    // Log.Message("Gender flags tripped; moving to next conditional set...");
                    continue;
                } else
                {
                    // Log.Message("Gender flags passed, continuing with this conditional set.");
                }

                // Log.Message("Checking hediff set of size " + hediffMatches);

                foreach (HediffDef checkHediff in conditionalSet.hediffConditionals)
                {
                    Hediff matchTest = pawn.health.hediffSet.GetFirstHediffOfDef(checkHediff);
                    if (matchTest != null)
                    {
                        // Log.Message("Match found!");
                        hediffMatched++;
                    }
                }

                Log.Message(hediffMatched + " of " + hediffMatches + " hediffs matched.");
                
                bool doApply = false;
                switch (conditionalSet.hediffComparator)
                {
                    case "all":
                        if (hediffMatched == hediffMatches)
                        {
                            doApply = true;
                        }
                        break;
                    case "none":
                        if (hediffMatched == 0)
                        {
                            doApply = true;
                        }
                        break;
                    case "atLeastOne":
                        if (hediffMatched >= 1)
                        {
                            doApply = true;
                        }
                        break;
                    case "atMostOne":
                        if (hediffMatched <= 1)
                        {
                            doApply = true;
                        }
                        break;
                    default:
                        break;
                }

                // Log.Message("Preparing hediff...");

                Hediff hediffToApply = HediffMaker.MakeHediff(conditionalSet.hediff, pawn);
                float effect = ((!(conditionalSet.severity > 0f)) ? conditionalSet.hediff.initialSeverity : conditionalSet.severity);
                hediffToApply.Severity = effect;

                if (doApply)
                {
                    // Log.Message("Applying hediff to pawn...");
                    pawn.health.AddHediff(hediffToApply);
                } else
                {
                    // Log.Message("NOT applying hediff to pawn.");
                }

            }
            
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(ThingDef parentDef)
        {
            if (!parentDef.IsDrug || !(chance >= 1f))
            {
                yield break;
            }
        }
    }
}
