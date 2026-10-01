using RimWorld;
using Verse;

namespace MultipleManXGene
{
    public class CompUseEffect_GiveMultipleManGene : CompUseEffect
    {
        public override void DoEffect(Pawn user)
        {
            base.DoEffect(user);
            ApplyGene(user);
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            var recipient = MarvelUnification.InjectorUtility.OrganicRecipient(p);
            if (!recipient.Accepted) return recipient;
            var result = base.CanBeUsedBy(p);
            if (!result.Accepted)
            {
                return result;
            }

            if (DupeSettingsManager.UseGeneMode && p.genes != null)
            {
                if (p.genes.HasActiveGene(MMDefOf.MM_MultipleManGene))
                {
                    return "Already has Multiple Man gene";
                }
            }
            else if (p.health?.hediffSet?.HasHediff(MMDefOf.MM_MultipleManGeneHediff) == true)
            {
                return "Already has Multiple Man power";
            }

            return result;
        }

        private static void ApplyGene(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            var addedGene = false;
            if (DupeSettingsManager.UseGeneMode && pawn.genes != null)
            {
                if (!pawn.genes.HasActiveGene(MMDefOf.MM_MultipleManGene))
                {
                    pawn.genes.AddGene(MMDefOf.MM_MultipleManGene, xenogene: true);
                    addedGene = true;
                }
            }

            if (!addedGene)
            {
                DupeUtility.EnsureGeneMarkerHediff(pawn);
            }
        }
    }
}
