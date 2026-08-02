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
    ///     Guardian Vigilance (DoT melee dps) rotation: Plasma Brand / Blade Barrage / Overhead
    ///     Slash on cooldown keep the burns rolling; Blade Storm autocrits at 2 Force Rush.
    /// </summary>
    public class Vigilance : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Vigilance;

        public override string Name => "Guardian Vigilance";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Force Might")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Resolute", ret => Me.IsStunned),

                    //Offensive
                    Spell.Buff("Force Clarity", ret => Me.Target.BossOrGreater()),

                    //Focus generation. Burning Focus is a passive that upgrades Combat Focus to also
                    //detonate burns — not a castable ability. Combat Focus is the button.
                    Spell.Cast("Combat Focus", ret => Me.ActionPoints <= 6),

                    //Defensives
                    Spell.Buff("Saber Reflect", ret => Me.HealthPercent <= 90),
                    Spell.Buff("Saber Ward", ret => Me.HealthPercent <= 50),
                    Spell.Buff("Focused Defense", ret => Me.HealthPercent < 70),
                    Spell.Buff("Enure", ret => Me.HealthPercent <= 30),
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15)
                    );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    Spell.Cast("Force Leap", ret => CombatHotkeys.EnableCharge && Me.Target.Distance >= 1f),
                    Spell.Cast("Saber Throw", ret => Me.Target.Distance > .4f && Me.Target.Distance <= 3f),

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Interrupts
                    Spell.Cast("Force Kick", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Rotation - Plasma Brand / Overhead Slash / Blade Storm / Vigilant Thrust on cooldown.
                    //Plasma Brand finishes the cooldown on Blade Barrage, so it leads.
                    Spell.Cast("Plasma Brand"),
                    Spell.Cast("Blade Barrage"),
                    Spell.Cast("Overhead Slash"),

                    //Blade Storm autocrits at 2 stacks of Force Rush; fall through until then.
                    Spell.Cast("Blade Storm", ret => Me.BuffCount("Force Rush") >= 2 || Me.Level < 40),
                    Spell.Cast("Vigilant Thrust", ret => Me.Target.Distance <= 0.5f),

                    //Whirling Blade replaces Dispatch for Vigilance; Keening makes it free and usable
                    //at any health. Dispatch is the pre-replacement (low level) fallback.
                    Spell.Cast("Whirling Blade", ret => Me.HasBuff("Keening") || Me.Target.HealthPercent <= 30),
                    Spell.Cast("Dispatch", ret => Me.Target.HealthPercent <= 30),

                    //Safety net: never let Blade Storm rot if the Force Rush proc never lands.
                    Spell.Cast("Blade Storm"),

                    //Fillers
                    Spell.Cast("Sundering Strike", ret => Me.ActionPoints <= 5),
                    Spell.Cast("Riposte"),
                    Spell.Cast("Slash", ret => Me.ActionPoints >= 6),
                    Spell.Cast("Strike")
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new Decorator(ret => Targeting.ShouldPbaoe,
                    new PrioritySelector(
                        //Get the burns up first, then spread them with Vigilant Thrust
                        Spell.Cast("Plasma Brand"),
                        Spell.Cast("Overhead Slash"),
                        Spell.Cast("Vigilant Thrust", ret => Me.Target.Distance <= 0.5f),
                        Spell.Cast("Blade Storm"),
                        Spell.Cast("Force Sweep", ret => Me.Target.Distance <= 0.5f),
                        Spell.Cast("Cyclone Slash", ret => Me.Target.Distance <= 0.5f),
                        Spell.Cast("Blade Barrage")
                        ));
            }
        }
    }
}
