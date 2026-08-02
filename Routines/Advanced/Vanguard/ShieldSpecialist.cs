// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using BuddyCron;
using BuddyCron.Behaviors;
using BuddyCron.Helpers;
using BuddyCron.Managers;
using BuddyCron.Navigation;
using BuddyCron.Objects;
using Reborn.Utilities;
using Reborn.Behaviors.Treesharp;
using DefaultCombat.Core;
using DefaultCombat.Helpers;

namespace DefaultCombat.Routines
{
    /// <summary>
    ///     7.x Vanguard Shield Specialist (tank) rotation, Republic mirror of Shield Tech.
    ///     Cell convention: Me.EnergyPercent is the REMAINING resource (100 = full energy cells,
    ///     0 = empty), so a low value means conserve.
    /// </summary>
    public class ShieldSpecialist : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.ShieldSpecialist;

        public override string Name => "Vanguard Shield Specialist";

        public override Composite Buffs
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Fortification"),
                    Spell.Cast("Guard", on => Me.Companion, ret => Me.Companion != null && !Me.Companion.IsDead && !Me.Companion.HasBuff("Guard"))
                    );
            }
        }

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Tenacity", ret => Me.IsStunned),

                    //Interrupt lives here so a cell-starved rotation can never swallow it
                    Spell.Cast("Riot Strike", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Energy Blast is off-GCD: dumps 3 Power Screens for damage, an absorb buff and cells
                    Spell.Cast("Energy Blast", ret => Me.BuffCount("Power Screen") >= 3 || Me.Level < 50),

                    //Defensives
                    Spell.Buff("Reactive Shield", ret => Me.HealthPercent <= 60),
                    //(Power Yield is a Powertech/Advanced Prototype ability -- no Vanguard discipline
                    // grants it. Shield Specialist's "special"-slot pick is Infused Kolto Packs.)
                    Spell.Cast("Riot Gas", ret => Me.HealthPercent <= 75),
                    Spell.Buff("Adrenaline Rush", ret => Me.HealthPercent <= 30),
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15),

                    //Cells: 7.0 folded Reserve Powercell into Recharge Cells
                    Spell.Cast("Recharge Cells", ret => Me.EnergyPercent <= 40),

                    //Offensive cooldowns
                    Spell.Cast("Battle Focus", ret => Me.InCombat && Me.Target.StrongOrGreater()),
                    Spell.Cast("Shoulder Cannon", ret => Me.InCombat && !Me.HasBuff("Shoulder Cannon"))
                    );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    //(No charge here: Storm is a Tactics / Plasmatech ability-tree pick. Shield
                    // Specialist's tier-2 slot is Extraction Plan instead, so it has no gap-closer.)

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Low cells -- free / procced casts only until energy regenerates
                    new Decorator(ret => Me.EnergyPercent <= 40,
                        new PrioritySelector(
                            Spell.Cast("Ion Storm", ret => Me.HasBuff("Pulse Engine") && Me.Target.Distance <= 1f),
                            Spell.Cast("Ion Pulse", ret => Me.HasBuff("Static Surge")),
                            Spell.Cast("Hammer Shot")
                            )),

                    //Rotation: Stockstrike and High Impact Bolt are the Power Screen generators,
                    //so they lead -- Energy Blast is fired off-GCD from Cooldowns
                    Spell.Cast("Stockstrike"),
                    Spell.Cast("High Impact Bolt"),
                    Spell.Cast("Ion Storm",
                        ret => (Me.HasBuff("Pulse Engine") || Me.Level < 50) && Me.Target.Distance <= 1f),
                    Spell.Cast("Shoulder Cannon", ret => Me.HasBuff("Shoulder Cannon") && Me.Target.StrongOrGreater()),

                    //Fillers
                    Spell.Cast("Ion Pulse", ret => Me.HasBuff("Static Surge")),
                    Spell.Cast("Ion Pulse", ret => Me.EnergyPercent >= 55),
                    Spell.Cast("Hammer Shot")
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new PrioritySelector(
                    new Decorator(ret => Targeting.ShouldAoe,
                        new PrioritySelector(
                            Spell.CastOnGround("Artillery Blitz")
                            )),
                    new Decorator(ret => Targeting.ShouldPbaoe,
                        new PrioritySelector(
                            Spell.Cast("Ion Storm"),
                            Spell.Cast("Flak Shell"),
                            Spell.Cast("Explosive Surge", ret => Me.EnergyPercent >= 50)
                            )));
            }
        }
    }
}
