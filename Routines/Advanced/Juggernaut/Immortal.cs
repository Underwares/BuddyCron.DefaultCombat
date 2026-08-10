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
    ///     Juggernaut Immortal (tank) rotation: Crushing Blow and Force Scream lead, with
    ///     Aegis Assault building Rage and keeping its defensive buff up.
    /// </summary>
    public class Immortal : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Immortal;

        public override string Name => "Juggernaut Immortal";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Unnatural Might")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Unleash", ret => Me.IsStunned),
					Spell.Buff("Furious Power", ret => Me.Target.BossOrGreater()),

                    //Defensives, strongest last. Saber Reflect is an ability-tree choice (may be
                    //Mad Dash / Intimidating Roar instead) - it simply gets skipped if untrained.
                    Spell.Buff("Saber Reflect", ret => Me.HealthPercent <= 80),
                    Spell.Buff("Enraged Defense", ret => Me.HealthPercent <= 70),
                    Spell.Buff("Invincible", ret => Me.HealthPercent <= 60),
                    Spell.Buff("Saber Ward", ret => Me.HealthPercent <= 45),
                    Spell.Buff("Endure Pain", ret => Me.HealthPercent <= 25),
                    Spell.Cast("Enrage", ret => Me.ActionPoints <= 6),
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15)
                    );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    Spell.Cast("Force Charge", ret => CombatHotkeys.EnableCharge && Me.Target.Distance >= 1f),
                    Spell.Cast("Saber Throw", ret => !RotationRuntime.MovementDisabled && Me.Target.Distance > .4f && Me.Target.Distance <= 3f),

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Interrupts
                    Spell.Cast("Disruption", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Rotation - Crushing Blow and Force Scream are the hard hitters (Force Scream also
                    //grants the Sonic Barrier absorb shield). Aegis Assault is the Rage builder and
                    //keeps the damage-reduction/absorb buff up, which also makes Crushing Blow cleave.
                    Spell.Cast("Crushing Blow"),
                    Spell.Cast("Force Scream"),
                    Spell.Cast("Aegis Assault", ret => Me.ActionPoints <= 8 || !Me.HasBuff("Aegis Assault")),
                    Spell.Cast("Ravage"),
                    Spell.Cast("Backhand", ret => !Me.Target.IsStunned),
                    Spell.Cast("Vicious Throw", ret => Me.Target.HealthPercent <= 30),
                    Spell.Cast("Smash", ret => Targeting.ShouldPbaoe),

                    //Fillers
                    Spell.Cast("Retaliation"),
                    Spell.Cast("Vicious Slash", ret => Me.ActionPoints >= 9),
                    Spell.Cast("Assault")
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new Decorator(ret => Targeting.ShouldPbaoe,
                    new PrioritySelector(
                        Spell.Cast("Crushing Blow"),
                        Spell.Cast("Smash"),
                        Spell.Cast("Force Scream"),
                        Spell.Cast("Aegis Assault", ret => Me.ActionPoints <= 8 || !Me.HasBuff("Aegis Assault")),
                        Spell.Cast("Retaliation"),
                        Spell.Cast("Sweeping Slash", ret => Me.ActionPoints >= 6)
                        ));
            }
        }
    }
}
