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
    ///     Assassin Darkness (tank) rotation: opens from stealth for Dark Protection stacks,
    ///     keeps Dark Ward up and spends 3 Harnessed Darkness on Depredating Volts.
    /// </summary>
    internal class Darkness : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Darkness;

        public override string Name => "Assassin Darkness";

        public override Composite Buffs
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Mark of Power"),
                    //Darkness should always open from Stealth: leaving stealth instantly grants
                    //4 stacks of Dark Protection (Conspirator's Cloak passive).
                    Spell.Buff("Stealth", ret => !Rest.KeepResting() && !DefaultCombat.MovementDisabled && !Me.IsMounted)
                    );
            }
        }

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    //Dark Ward: keep up 100% of the time, refresh when it drops or runs low on charges
                    Spell.Cast("Dark Ward", ret => Me,
                        ret => !Me.HasBuff("Dark Ward") || Me.BuffCount("Dark Ward") <= 3),
                    Spell.Buff("Unbreakable Will", ret => Me.IsStunned),
                    Spell.Buff("Overcharge Saber", ret => Me.HealthPercent <= 75),
                    Spell.Buff("Deflection", ret => Me.HealthPercent <= 60),
                    Spell.Buff("Force Shroud", ret => Me.HealthPercent <= 50),
                    Spell.Buff("Force Speed", ret => Me.HealthPercent <= 35),
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
                    //Gap closer. Phantom Stride also grants Conspirator's Cloak, so the opener
                    //naturally lands a free, full damage Maul right after the stride.
                    Spell.Cast("Phantom Stride", ret => CombatHotkeys.EnableCharge && Me.Target.Distance >= 1f),

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Interrupts
                    Spell.Cast("Jolt", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),
                    Spell.Cast("Electrocute", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Rotation
                    Spell.Cast("Assassinate", ret => Me.Target.HealthPercent <= 30 || Me.HasBuff("Reaper's Rush")),
                    Spell.Cast("Depredating Volts", ret => Me.BuffCount("Harnessed Darkness") >= 3),
                    Spell.Cast("Shock"),
                    Spell.Cast("Wither"),
                    Spell.Cast("Maul", ret => Me.HasBuff("Conspirator's Cloak")),
                    //Dark Charge's Discharge applies the accuracy debuff "Unsteady (Force)" (45s)
                    Spell.Cast("Discharge", ret => !Me.Target.HasMyDebuff("Unsteady (Force)")),
                    Spell.Cast("Thrash", ret => Me.ForcePercent >= 30),
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
                            //Depredating Volts is worth using in AoE even at 0 Harnessed Darkness
                            Spell.Cast("Depredating Volts"),
                            Spell.Cast("Wither"),
                            Spell.Cast("Discharge"),
                            Spell.Cast("Severing Slash"))),
                    new Decorator(ret => Targeting.ShouldPbaoe,
                        Spell.Cast("Lacerate", ret => Me.ForcePercent >= 40))
                    );
            }
        }
    }
}
