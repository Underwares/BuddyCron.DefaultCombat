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
    ///     Juggernaut Rage (burst melee dps) rotation: Raging Burst on cooldown with the
    ///     Shockwave / Dominate procs, Furious Strike right behind it.
    /// </summary>
    public class Rage : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Rage;

        public override string Name => "Juggernaut Rage";

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
                    Spell.Buff("Saber Reflect", ret => Me.HealthPercent <= 90),
                    Spell.Buff("Saber Ward", ret => Me.HealthPercent <= 70),
                    Spell.Buff("Enraged Defense", ret => Me.HealthPercent < 70),
                    Spell.Buff("Endure Pain", ret => Me.HealthPercent <= 30),

                    //Enrage is an offensive cooldown for Rage: 6 Rage up front, +1/sec, and it grants
                    //Shockwave (next Smash/Raging Burst is free and hits 15% harder).
                    Spell.Cast("Enrage", ret => Me.ActionPoints <= 8),
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
                    Spell.Cast("Saber Throw", ret => !DefaultCombat.MovementDisabled && Me.Target.Distance > .4f && Me.Target.Distance <= 3f),

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Interrupts
                    Spell.Cast("Disruption", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Rotation - Raging Burst on cooldown (autocrits with Dominate from Force Charge /
                    //Obliterate, free + 15% harder with Shockwave from Enrage / Force Crush), then
                    //Furious Strike immediately after it.
                    Spell.Cast("Raging Burst"),
                    Spell.Cast("Furious Strike", ret => Me.ActionPoints >= 5 || Me.HasBuff("Fuming Rage")),
                    Spell.Cast("Obliterate"),
                    Spell.Cast("Force Crush"),

                    //Execute
                    Spell.Cast("Vicious Throw", ret => Me.Target.HealthPercent <= 30),

                    //Smash is the low-level stand-in before Raging Burst is trained, and is still worth
                    //pressing when a pack is stacked on the target.
                    Spell.Cast("Smash", ret => Me.Level < 25 || Targeting.ShouldPbaoe),

                    Spell.Cast("Force Scream"),
                    Spell.Cast("Ravage"),

                    //Fillers
                    Spell.Cast("Retaliation"),
                    Spell.Cast("Vicious Slash", ret => Me.ActionPoints >= 9),
                    Spell.Cast("Sundering Assault", ret => Me.ActionPoints <= 7),
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
                        Spell.Cast("Smash"),
                        Spell.Cast("Obliterate"),
                        Spell.Cast("Force Crush"),
                        Spell.Cast("Furious Strike", ret => Me.ActionPoints >= 5 || Me.HasBuff("Fuming Rage")),
                        Spell.Cast("Sweeping Slash", ret => Me.ActionPoints >= 6),
                        Spell.Cast("Saber Throw")
                        ));
            }
        }
    }
}
