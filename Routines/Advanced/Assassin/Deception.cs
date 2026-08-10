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
    ///     Assassin Deception (burst melee dps) rotation: stealth opener, Discharge at
    ///     3 Static Charge, Ball Lightning on cooldown and Maul on Duplicity procs.
    /// </summary>
    internal class Deception : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Deception;

        public override string Name => "Assassin Deception";

        public override Composite Buffs
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Mark of Power"),
                    //Always re-stealth out of combat so we can open with Phantom Stride + Reaping Strike
					Spell.Buff("Stealth", ret => !Rest.KeepResting() && !DefaultCombat.MovementDisabled && !Me.IsMounted)
                    );
            }
        }

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Unbreakable Will", ret => Me.IsStunned),
                    Spell.Buff("Overcharge Saber", ret => Me.InCombat),
                    Spell.Buff("Deflection", ret => Me.HealthPercent <= 60),
                    Spell.Buff("Force Shroud", ret => Me.HealthPercent <= 50),
                    Spell.Buff("Recklessness", ret => Me.InCombat),
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15)
                    );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    //Stealth opener / gap closer
                    Spell.Cast("Phantom Stride", ret => CombatHotkeys.EnableCharge && Me.Target.Distance >= 1f),
                    Spell.Cast("Force Speed", ret => CombatHotkeys.EnableCharge && Me.IsMoving && Me.Target.Distance > 1f),

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Interrupts
                    Spell.Cast("Jolt", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),
                    Spell.Cast("Electrocute", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Refresh Voltage before spending charges so Ball Lightning retains its bonuses.
                    Spell.Cast("Voltaic Slash",
                        ret => Me.ForcePercent >= 25 &&
                               (!Me.HasBuff("Voltage") || Me.BuffTimeLeft("Voltage") <= 3)),

                    //Spend three Static Charges when that mechanic is active. The zero-stack CanCast
                    //fallback lets the pre-upgrade low-level version fire without guessing an unlock level.
                    Spell.Cast("Discharge",
                        ret => Me.BuffCount("Static Charge") >= 3 ||
                               (Me.BuffCount("Static Charge") == 0 &&
                                AbilityManager.CanCast("Discharge", Me.Target).Success)),
                    Spell.Cast("Assassinate", ret => Me.Target.HealthPercent <= 30 || Me.HasBuff("Reaper's Rush")),
                    //Reaping Strike is only usable from stealth or inside the crit window - CanCast gates it
                    Spell.Cast("Reaping Strike"),
                    //Ball Lightning goes on cooldown regardless of Induction stacks
                    Spell.Cast("Ball Lightning"),
                    Spell.Cast("Maul", ret => Me.HasBuff("Duplicity")),
                    Spell.Cast("Voltaic Slash", ret => Me.ForcePercent >= 25),
                    //Filler for characters that have not trained Voltaic Slash yet
                    Spell.Cast("Thrash", ret => Me.ForcePercent >= 25),
                    Spell.Cast("Saber Strike")

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
                            Spell.Cast("Discharge",
                                ret => Me.BuffCount("Static Charge") >= 3 ||
                                       (Me.BuffCount("Static Charge") == 0 &&
                                        AbilityManager.CanCast("Discharge", Me.Target).Success)),
							Spell.Cast("Severing Slash"))),
                    new Decorator(ret => Targeting.ShouldPbaoe,
                        //Lacerate is the AoE filler and also builds Voltage for Ball Lightning
                        Spell.Cast("Lacerate", ret => Me.ForcePercent >= 40))
                    );
            }
        }
    }
}
