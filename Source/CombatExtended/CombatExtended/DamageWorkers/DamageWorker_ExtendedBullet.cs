using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;
using RimWorld;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class ExtendedProjectilesPatcher
    {
        static ExtendedProjectilesPatcher()
        {
            foreach (var ammo in DefDatabase<ThingDef>.AllDefs.Where(x => x.HasModExtension<ExtendedBulletProps>()))
            {
                if (ammo.statBases == null)
                {
                    ammo.statBases = new List<StatModifier>();
                }
                ammo.statBases.Add(new StatModifier { stat = CE_StatDefOf.ExtendedStats, value = 1f });
            }

            foreach (var race in DefDatabase<ThingDef>.AllDefs.Where(x => x.race != null))
            {
                if (race.comps == null)
                {
                    race.comps = new List<CompProperties>();
                }

                race.comps.Add(new CompProperties { compClass = typeof(ProjComp) });
            }

            DamageDefOf.Bullet.workerClass = typeof(DamageWorker_ExtendedBullet);
        }
    }

    public class ProjComp : ThingComp
    {
        public BulletCE bullet;
    }
    public class ExtendedBulletProps : DefModExtension
    {
        public float fragmentationChance = 0f;

        public SimpleCurve distanceFragMultCurve;

        public IntRange fragmentCountRange = new IntRange(0, 0);

        public FloatRange fragmentDamageRange = new FloatRange(0, 0);

        public float yawChance = 0f;

        public FloatRange yawDamage = new FloatRange(0, 0);

        public float tumbleChance = 0f;

        public FloatRange tumbleDamage = new FloatRange(0, 0);

        //WiP
        /*
        public float overpenChance = 0f;
        */
    }
    public class DamageWorker_ExtendedBullet : DamageWorker_AddInjury
    {
        public bool fragged = false;

        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            var result = base.Apply(dinfo, thing);

            if (thing is Pawn p)
            {
                if (!p.RaceProps?.IsFlesh ?? true)
                {
                    return result;
                }
                #region tumbling
                var proj = (p.TryGetComp<ProjComp>()?.bullet ?? null);

                if (proj == null)
                {
                    return result;
                }

                if (proj != null && proj.def.HasModExtension<ExtendedBulletProps>())
                {
                    var ext = proj.def.GetModExtension<ExtendedBulletProps>();
                    if (Rand.Chance(ext.tumbleChance))
                    {
                        dinfo.SetAmount(dinfo.Amount * ext.tumbleDamage.RandomInRange);
                    }
                }
                #endregion
            }

            return result;
        }

        public override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
        {
            var result = base.ChooseHitPart(dinfo, pawn);

            if (!pawn.RaceProps.IsFlesh)
            {
                return result;
            }

            if ((result?.depth ?? BodyPartDepth.Undefined) == BodyPartDepth.Inside)
            {
                var proj = (pawn.TryGetComp<ProjComp>()?.bullet ?? null);

                if (proj == null)
                {
                    return result;
                }

                List<BodyPartRecord> bodyparts = result.parts?.ListFullCopy() ?? new List<BodyPartRecord>();

                bodyparts.Add(result);

                if (result.parent.depth == BodyPartDepth.Inside)
                {
                    bodyparts.Add(result.parent);
                }

                bodyparts.AddRange(result.parent.parts.Where(x => x.depth == BodyPartDepth.Inside));

                float mult = 1f;

                if (result.def.IsSolid(result, pawn.health.hediffSet.hediffs))
                {
                    mult = 2f;
                }

                if (!bodyparts.NullOrEmpty())
                {
                    BodyPartRecord outerMostPart = bodyparts.MaxBy(x => x.depth == BodyPartDepth.Outside) ?? null;

                    #region Likely to be slow for not much effect
                    /*if (outerMostPart != null)
                    {
                        float SharpAP = proj.def.projectileCE()?.armorPenetrationSharp ?? 0f;

                        float SharpDefense = 0f;

                        if (pawn?.apparel?.WornApparel?.FindAll(x => x.def?.apparel?.CoversBodyPart(outerMostPart) ?? false).Any() ?? false)
                        {
                            SharpDefense = pawn?.apparel?.WornApparel?.FindAll(x => x.def?.apparel?.CoversBodyPart(outerMostPart) ?? false).Max(x => x.GetStatValue(StatDefOf.ArmorRating_Sharp)) ?? 0f;
                        }

                        float Val1 = Math.Max((SharpDefense - (SharpAP * 0.85f)), 0f);

                        float finalVal = Mathf.Lerp(1f, 0.1f, Val1);

                        mult *= finalVal;
                    }*/
                    #endregion (the commented code)

                    if (proj != null && proj.def.HasModExtension<ExtendedBulletProps>())
                    {
                        var ext = proj.def.GetModExtension<ExtendedBulletProps>();
                        if (Rand.Chance(ext.distanceFragMultCurve.Evaluate( ((proj.OriginIV3.DistanceTo(pawn.Position)) ) * mult) + proj.verbProps.barrelLength.max))
                        {
                            for (int i = (int)ext.fragmentCountRange.RandomInRange; i >= 0; i--)
                            {
                                var bp = bodyparts.RandomElementByWeight(x => x.coverage);

                                var hediff = HediffMaker.MakeHediff(CE_HediffDefOf.ProjCut, pawn, bp);

                                hediff.Severity = ext.fragmentDamageRange.RandomInRange * dinfo.Amount;

                                pawn.health.AddHediff(hediff, bp);
                            }

                            fragged = true;
                        }
                        else
                        {
                            if (Rand.Chance(ext.yawChance * mult))
                            {
                                var bp = bodyparts.RandomElementByWeight(x => x.coverage);

                                var hediff = HediffMaker.MakeHediff(dinfo.Def.hediff, pawn, bp);

                                hediff.Severity = dinfo.Amount * ext.yawDamage.RandomInRange;

                                pawn.health.AddHediff(hediff, bp);
                            }
                        }
                    }
                }
            }


            return result;
        }
    }

    public class ExtendedCaliberStatWorker : StatWorker
    {
        public override string ValueToString(float val, bool finalized, ToStringNumberSense numberSense = ToStringNumberSense.Absolute)
        {
            return "hover over";
        }

        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            ExtendedBulletProps correctProjExtProps = null;

            ThingDef correctProj = ((AmmoDef)req.Thing?.def)?.AmmoSetDefs[0].ammoTypes.Find(x => x.ammo == req.Thing.def).projectile;

            if (req.Thing != null)
            {
                correctProjExtProps = correctProj.GetModExtension<ExtendedBulletProps>();
            }
            else if (req.Def != null)
            {
                correctProj = ((AmmoDef)req.Def)?.AmmoSetDefs.FirstOrFallback()?.ammoTypes.Find(x => x.ammo == req.Def).projectile ?? null;

                correctProjExtProps = correctProj.GetModExtension<ExtendedBulletProps>();
            }


            string text = "Fragmentation chance " + (correctProjExtProps.fragmentationChance * 100f).ToString() + "%";

            text += "\n";

            text += "\n";

            text += "Fragment amount range: " + " min. " + correctProjExtProps.fragmentCountRange.min.ToString() + " max. " + correctProjExtProps.fragmentCountRange.max.ToString();

            text += "\n";

            text += "\n";

            text += "Fragment damage range: " + " min. " + (correctProjExtProps.fragmentDamageRange.min * correctProj.projectile.GetDamageAmount(1)).ToString() + " max. " + (correctProjExtProps.fragmentDamageRange.max * correctProj.projectile.GetDamageAmount(1)).ToString();

            text += "\n";

            text += "\n";

            text += "\n";

            text += "Yaw chance " + Math.Round((correctProjExtProps.yawChance * 100f), 1).ToString() + "% ";

            text += "\n";

            text += "\n";

            text += "Yaw damage range " + " min. " + Math.Round((correctProjExtProps.yawDamage.min * correctProj.projectile.GetDamageAmount(1))).ToString() + " max. " + (correctProjExtProps.yawDamage.max * correctProj.projectile.GetDamageAmount(1)).ToString();

            text += "\n";

            text += "\n";

            text += "\n";

            text += "Tumbling chance " + Math.Round((correctProjExtProps.tumbleChance * 100f), 1).ToString() + "% ";

            text += "\n";

            text += "\n";

            text += "Tumbling damage range " + " min. " + Math.Round((correctProjExtProps.tumbleDamage.min * correctProj.projectile.GetDamageAmount(1))).ToString() + " max. " + (correctProjExtProps.tumbleDamage.max * correctProj.projectile.GetDamageAmount(1)).ToString();

            return text;
        }

        public override bool ShouldShowFor(StatRequest req)
        {
            var result = (req.Thing?.def ?? ThingDefOf.ActivatorProximity) is AmmoDef;

            if (result)
            {
                result = (((AmmoDef)req.Thing?.def).AmmoSetDefs?.FirstOrFallback()?.ammoTypes.Find(y => y.ammo == req.Thing.def)?.projectile?.HasModExtension<ExtendedBulletProps>() ?? false);
            }

            if (req.Def is AmmoDef def
                &&
                (def.AmmoSetDefs?.FirstOrFallback()?.ammoTypes.Find(y => y.ammo == req.Def)?.projectile?.HasModExtension<ExtendedBulletProps>() ?? false)
                )
            {
                return true;
            }

            if (result)
            {
                result = req.Thing.TryGetComp<CompFragments>() == null;
            }

            return result;
        }
    }
}
