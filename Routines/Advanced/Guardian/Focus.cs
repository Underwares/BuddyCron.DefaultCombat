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
    ///     Guardian Focus (burst melee dps) rotation: builds Singularity and Felling Blow,
    ///     then spends them on Focused Burst / Force Sweep.
    /// </summary>
    public class Focus : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Focus;

        public override string Name => "Guardian Focus";

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

                    //Combat Focus grants Singularity (next Focused Burst / Force Sweep is free
                    //and hits 15% harder) and refills focus. Alternated with Force Exhaustion.
                    Spell.Cast("Combat Focus", ret => !Me.HasBuff("Singularity") || Me.ActionPoints <= 4),

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

                    //Rotation - Zealous Leap grants Felling Blow (autocrit Focused Burst / Force Sweep),
                    //Force Exhaustion / Combat Focus grant Singularity. Never spend the procs on anything else.
                    Spell.Cast("Zealous Leap"),
                    Spell.Cast("Force Exhaustion", ret => !Me.HasBuff("Singularity")),
                    Spell.Cast("Focused Burst", ret => Me.HasBuff("Felling Blow") || Me.Level < 30),
                    Spell.Cast("Force Sweep", ret => (Me.HasBuff("Felling Blow") || Me.Level < 30) && Me.Target.Distance <= 0.5f),

                    //Force Lash / Focused Vision windows
                    Spell.Cast("Concentrated Slice"),
                    Spell.Cast("Riposte"),

                    //Momentum makes the next Blade Storm hit harder after a leap
                    Spell.Cast("Blade Storm"),
                    Spell.Cast("Blade Barrage"),
                    Spell.Cast("Dispatch", ret => Me.Target.HealthPercent <= 30),

                    //Safety net: never let Focused Burst rot if the Felling Blow proc never lands
                    Spell.Cast("Focused Burst"),

                    //Fillers
                    Spell.Cast("Sundering Strike", ret => Me.ActionPoints <= 5),
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
                        //Force Sweep is the AoE payoff - only spend it with the procs up
                        Spell.Cast("Force Exhaustion", ret => !Me.HasBuff("Singularity")),
                        Spell.Cast("Zealous Leap"),
                        Spell.Cast("Force Sweep", ret => Me.Target.Distance <= 0.5f),
                        Spell.Cast("Blade Storm"),
                        Spell.Cast("Blade Barrage"),
                        Spell.Cast("Cyclone Slash", ret => Me.Target.Distance <= 0.5f),
                        Spell.Cast("Sundering Strike", ret => Me.ActionPoints <= 5)
                        ));
            }
        }
    }
}
