using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class VerbPropertiesCE : VerbProperties
    {
        public RecoilPattern recoilPattern = RecoilPattern.None;
        public int ammoConsumedPerShotCount = 1;
        public float recoilAmount = 0;
        public float indirectFirePenalty = 0;
        public float circularError = 0;
        public float meleeArmorPenetration = 0;
        public bool ejectsCasings = true;
        public bool ignorePartialLoSBlocker = false;
        public float speedMult = 1f;
        // not a true floatrange, just takes the min for barrel length velocity mult and max for what distance it translates to
        public FloatRange barrelLength;

        public float AdjustedArmorPenetrationCE(Verb ownerVerb, Pawn attacker)
        {
            var toolCE = (ToolCE)ownerVerb.tool;
            if (ownerVerb.verbProps != this)
            {
                Log.ErrorOnce("Tried to calculate armor penetration for a verb with different verb props. verb=" + ownerVerb, 9865767);
                return 0f;
            }
            if (ownerVerb.EquipmentSource != null && ownerVerb.EquipmentSource.def.IsWeapon)
            {
                return toolCE.armorPenetration * ownerVerb.EquipmentSource.GetStatValue(CE_StatDefOf.MeleePenetrationFactor);
            }
            else
            {
                return toolCE.armorPenetration;
            }
        }
    }
}
