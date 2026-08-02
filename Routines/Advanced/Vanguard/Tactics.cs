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
    ///     7.x Vanguard Tactics (burst DPS) rotation, Republic mirror of Advanced Prototype.
    ///     Cell convention: Me.EnergyPercent is the REMAINING resource (100 = full energy cells,
    ///     0 = empty), so a low value means conserve and fall back to Hammer Shot.
    /// </summary>
    public class Tactics : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Tactics;

        public override string Name => "Vanguard Tactics";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Fortification")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Tenacity", ret => Me.IsStunned),

                    //Interrupt lives here so a cell-starved rotation can never swallow it
                    Spell.Cast("Riot Strike", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Defensives
                    Spell.Buff("Reactive Shield", ret => Me.HealthPercent <= 60),
                    Spell.Buff("Adrenaline Rush", ret => Me.HealthPercent <= 30),
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15),

                    //Cells: 7.0 folded Reserve Powercell into Recharge Cells
                    Spell.Cast("Recharge Cells", ret => Me.EnergyPercent <= 40),

                    //(Power Yield is a Powertech/Advanced Prototype ability -- no Vanguard discipline
                    // grants it. Tactics' "special"-slot pick is Balmorran Advanced Weaponry.)

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
                    Spell.Cast("Storm", ret => CombatHotkeys.EnableCharge && Me.Target.Distance >= 1f),

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Low cells -- High Impact Bolt is free off Tactical Accelerator
                    new Decorator(ret => Me.EnergyPercent <= 40,
                        new PrioritySelector(
                            Spell.Cast("High Impact Bolt", ret => Me.HasBuff("Tactical Accelerator")),
                            Spell.Cast("Hammer Shot")
                            )),

                    //Rotation: never delay High Impact Bolt (it is the Energy Lode generator),
                    //dump 4 Energy Lodes with Cell Burst, keep the Gut bleed rolling
                    Spell.Cast("High Impact Bolt", ret => Me.HasBuff("Tactical Accelerator")),
                    Spell.Cast("Cell Burst", ret => Me.BuffCount("Energy Lode") >= 4 || Me.Level < 50),
                    Spell.Cast("High Impact Bolt"),
                    //Gut's bleed is named just "Bleeding" (the Imperial mirror, Retractable Blade, uses
                    //"Bleeding (Retractable Blade)" -- the two are NOT named symmetrically). Gut has no
                    //cooldown, so a wrong name here re-casts it every GCD.
                    Spell.DoT("Gut", "Bleeding"),
                    Spell.Cast("Assault Plastique"),
                    Spell.Cast("Stockstrike"),
                    Spell.Cast("Shoulder Cannon", ret => Me.HasBuff("Shoulder Cannon") && Me.Target.StrongOrGreater()),

                    //Fillers
                    Spell.Cast("Tactical Surge", ret => Me.EnergyPercent >= 55),
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
                            //Flak Shell spreads the Gut bleed to everything it hits
                            Spell.Cast("Flak Shell"),
                            Spell.Cast("Cell Burst", ret => Me.BuffCount("Energy Lode") >= 4),
                            Spell.Cast("Explosive Surge", ret => Me.EnergyPercent >= 50)
                        )));
            }
        }
    }
}
