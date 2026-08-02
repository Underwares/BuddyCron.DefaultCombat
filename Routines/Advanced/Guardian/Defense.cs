// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using System.Windows.Input;
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
using Targeting = DefaultCombat.Core.Targeting;

namespace DefaultCombat.Routines
{
    /// <summary>
    ///     Guardian Defense (tank) rotation: off-GCD Riposte, Warding Strike buff upkeep,
    ///     Guardian Slash and Blade Storm on cooldown.
    /// </summary>
    public class Defense : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Defense;

        public override string Name => "Guardian Defense";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Force Might")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Resolute", ret => Me.IsStunned),

                    //Focus generation. Threatening Focus is a passive that upgrades Combat Focus
                    //(taunt cooldown reduction) — not a castable ability. Combat Focus is the button.
                    Spell.Cast("Combat Focus", ret => Me.ActionPoints <= 6),

                    //Defensives
                    Spell.Buff("Saber Reflect", ret => Me.HealthPercent <= 90),
                    Spell.Buff("Enure", ret => Me.HealthPercent <= 80),
                    Spell.Buff("Focused Defense", ret => Me.HealthPercent < 70),
                    Spell.Buff("Warding Call", ret => Me.HealthPercent <= 50),
                    Spell.Buff("Saber Ward", ret => Me.HealthPercent <= 30),
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

                    //Rotation - Riposte is off the GCD, so fire it whenever it is up (Blade Barricade)
                    Spell.Cast("Riposte"),

                    //Warding Strike is the focus builder and buffs the next Guardian Slash into an AoE
                    Spell.Cast("Warding Strike", ret => !Me.HasBuff("Warding Strike") || Me.ActionPoints <= 6),
                    Spell.Cast("Guardian Slash"),
                    Spell.Cast("Blade Storm"),
                    Spell.Cast("Dispatch", ret => Me.Target.HealthPercent <= 30),
                    Spell.Cast("Blade Barrage"),
                    Spell.Cast("Hilt Bash", ret => !Me.Target.IsStunned && !Me.Target.BossOrGreater() && Me.Target.Distance <= 0.4f),
                    Spell.Cast("Force Sweep", ret => Me.Target.Distance <= 0.5f),

                    //Fillers
                    Spell.Cast("Saber Throw", ret => Me.ActionPoints >= 3),
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
                        //Guardian Slash only cleaves while Warding Strike's buff is up
                        Spell.Cast("Warding Strike", ret => !Me.HasBuff("Warding Strike")),
                        Spell.Cast("Guardian Slash", ret => Me.HasBuff("Warding Strike") || Me.Level < 30),
                        Spell.Cast("Blade Storm"),
                        Spell.Cast("Force Sweep", ret => Me.Target.Distance <= 0.5f),
                        Spell.Cast("Cyclone Slash", ret => Me.Target.Distance <= 0.5f),
                        Spell.Cast("Blade Barrage")
                        ));
            }
        }
    }
}
