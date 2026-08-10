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
    ///     Juggernaut Vengeance (DoT melee dps) rotation: Shatter / Impale / Vengeful Slam /
    ///     Force Scream on cooldown keep the bleeds at full uptime.
    /// </summary>
    public class Vengeance : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Vengeance;

        public override string Name => "Juggernaut Vengeance";

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
                    Spell.Buff("Enraged Defense", ret => Me.HealthPercent <= 70),
                    Spell.Buff("Saber Reflect", ret => Me.HealthPercent <= 60),
                    Spell.Buff("Saber Ward", ret => Me.HealthPercent <= 50),
                    Spell.Buff("Endure Pain", ret => Me.HealthPercent <= 30),

                    //Bloodrage is a passive that upgrades Enrage (it detonates bleeds); there is no
                    //ability by that name to cast. Enrage is the button.
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

                    //Alternate the repeating Impale / Force Scream / Vengeful Slam core with priority
                    //slots. Shatter takes the first open slot and resets Ravage; Ravage is then consumed
                    //in a later slot while multiple bleeds are active.
                    Spell.Cast("Impale"),
                    Spell.Cast("Shatter"),
                    Spell.Cast("Force Scream",
                        ret => Me.BuffCount("Savagery") >= 2 || Me.Level < 40 || !Me.Target.BossOrGreater()),
                    Spell.Cast("Ravage"),
                    Spell.Cast("Vengeful Slam", ret => Me.Target.Distance <= 0.5f),

                    //Execute: free/anytime with the Destroyer proc, otherwise sub-30%.
                    Spell.Cast("Hew", ret => Me.HasBuff("Destroyer") || Me.Target.HealthPercent <= 30),

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
                        Spell.Cast("Impale"),
                        Spell.Cast("Shatter"),
                        Spell.Cast("Force Scream"),
                        Spell.Cast("Vengeful Slam", ret => Me.Target.Distance <= 0.5f),
                        Spell.Cast("Sweeping Slash", ret => Me.ActionPoints >= 6)
                        ));
            }
        }
    }
}
