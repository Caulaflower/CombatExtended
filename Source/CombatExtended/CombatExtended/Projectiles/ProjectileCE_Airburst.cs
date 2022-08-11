﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using CombatExtended.Utilities;

namespace CombatExtended
{
    public class ProjectileCE_Airburst : ProjectileCE
    {
        public int updateRate => props.updateRate;

        public int timer = -10;
        
        public bool detonated;

        public ModExtension_Airburst props => this.def.GetModExtension<ModExtension_Airburst>();
        public override void Tick()
        {
            if (timer >= updateRate)
            {
                 if (!detonated)
                {
                    if (timer >= updateRate)
                    {
                        UpdateDistance();
                        timer = 0;
                    }
                    timer++;
                    base.Tick();
                }
            }
        }

        public virtual void UpdateDistance()
        {
             if (
                detonated || 
                this.Position.DistanceTo(this.intendedTarget.Cell) <= props.distToExplode
                ||
                this.Position.PawnsInRange(this.Map, props.distToExplode).Any()
                )
            {
                detonated = true;
                base.ImpactSomething();
            }
        }
    }
}
