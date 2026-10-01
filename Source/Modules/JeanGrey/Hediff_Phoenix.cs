using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace JeanGreyMod
{
    [StaticConstructorOnStartup]
    public class Hediff_Phoenix : HediffWithComps
    {
        private static readonly Texture2D IconTelekinesis = ContentFinder<Texture2D>.Get("UI/Abilities/Telekinesis", true);
        private static readonly Texture2D IconMindBlast = ContentFinder<Texture2D>.Get("UI/Abilities/MindBlast", true);

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }

            if (pawn == null || !pawn.Drafted || !pawn.IsColonistPlayerControlled) 
                yield break;

            // Ability 1: Telekinetic Push (Available from Stage 1)
            yield return new Command_Target
            {
                defaultLabel = "JG_TelekineticPush_Label".Translate(),
                defaultDesc = "JG_TelekineticPush_Desc".Translate(),
                icon = IconTelekinesis,
                // range = 24.9f, // Command_Target does not support range drawing natively
                targetingParams = TargetingParameters.ForAttackAny(),
                action = delegate(LocalTargetInfo target)
                {
                    PsionicMechanics.DoTelekineticPush(this.pawn, target);
                }
            };

            // Ability 2: Molecular Deconstruction (Available immediately)
            {
                yield return new Command_Target
                {
                    defaultLabel = "JG_MolecularDeconstruction_Label".Translate(),
                    defaultDesc = "JG_MolecularDeconstruction_Desc".Translate(),
                    icon = IconMindBlast,
                    // range = 30f,
                    targetingParams = TargetingParameters.ForAttackAny(),
                    action = delegate(LocalTargetInfo target)
                    {
                        PsionicMechanics.DoMindBlast(this.pawn, target);
                    }
                };
            }
        }
    }
}
