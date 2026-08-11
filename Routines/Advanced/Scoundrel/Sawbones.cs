// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using BuddyCron;
using BuddyCron.Behaviors;
using BuddyCron.Helpers;
using BuddyCron.Managers;
using BuddyCron.Navigation;
using BuddyCron.Objects;
using DefaultCombat.Behaviors;
using Reborn.Utilities;
using Reborn.Behaviors.Treesharp;
using DefaultCombat.Helpers;
//using DefaultCombat.Extensions; ((Hold off for now))

namespace DefaultCombat.Routines
{
    /// <summary>
    ///     7.x Scoundrel Sawbones (healing) rotation. Healing lives in AreaOfEffect;
    ///     SingleTarget is filler damage only reached when nothing needs healing.
    /// </summary>
    public class Sawbones : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Sawbones;

        public override string Name => "Scoundrel Sawbones";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Lucky Shots")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Escape", ret => Core.Player.IsStunned),

                    //Raid buff - costs 1 Upper Hand
                    Spell.Buff("Stack the Deck", ret => CombatHotkeys.EnableRaidBuffs && Core.Player.HasBuff("Upper Hand")),

                    //Energy / Upper Hand economy
                    Spell.Cast("Cool Head", ret => Core.Player.EnergyPercent <= 40),
                    Spell.Cast("Pugnacity", ret => Core.Player.InCombat && Core.Player.BuffCount("Upper Hand") < 2),

                    //Ability tree choice (lvl 43) - resets Pugnacity / Kolto Cloud / Triage
                    Spell.Buff("Hot Streak",
                        ret => Core.Player.InCombat && Targeting.HealTarget != null && Targeting.HealTarget.HealthPercent <= 50),

                    //Defensives
                    Spell.Buff("Defense Screen", ret => Core.Player.HealthPercent <= 75),
                    Spell.Buff("Dodge", ret => Core.Player.HealthPercent <= 50),
                    Spell.Buff("Unity", ret => Core.Player.Companion != null && Core.Player.HealthPercent <= 15)
                    );
            }
        }

        //DPS - only ever reached when nothing needs healing (solo / leveling)
        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    RotationRuntime.HeroicMoment,

                    //Interrupt
                    Spell.Cast("Distraction", ret => Core.Player.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Filler damage
                    Spell.Cast("Vital Shot",
                        ret => Core.Player.EnergyPercent >= 60 &&
                               (!Core.Player.Target.HasMyDebuff("Vital Shot") || Core.Player.Target.DebuffTimeLeft("Vital Shot") <= 2)),
                    Spell.Cast("Blaster Whip", ret => Core.Player.BuffCount("Upper Hand") < 2),
                    Spell.Cast("Quick Shot", ret => Core.Player.EnergyPercent >= 85),

                    //Never stall
                    Spell.Cast("Flurry of Bolts")
                    );
            }
        }

        //Healing
        public override Composite AreaOfEffect
        {
            get
            {
                return new PrioritySelector(

                    //Cleanse
                    //Spell.Cast("Triage", ret => Targeting.HealTarget.ShouldDispel()), ((New Code Hold off for now))
                    Spell.Cleanse("Triage"),

                    //Emergency - free below 30%
                    Spell.Heal("Emergency Medpac", 30),

                    //Burst triage - preserve one Upper Hand outside the emergency range.
                    Spell.Heal("Kolto Pack", 55,
                        ret => (Core.Player.BuffCount("Upper Hand") >= 2 || Targeting.HealTarget.HealthPercent <= 35) &&
                               Core.Player.EnergyPercent >= 45),

                    //Slow-release Medpac upkeep - two stacks on the tank and current triage target.
                    Spell.Heal("Slow-release Medpac", on => Targeting.Tank, 100,
                        ret => Targeting.Tank != null && Targeting.Tank.InCombat &&
                               (Targeting.Tank.BuffCount("Slow-release Medpac") < 2 ||
                                Targeting.Tank.BuffTimeLeft("Slow-release Medpac") < 6)),
                    Spell.Heal("Slow-release Medpac", 90,
                        ret => Targeting.HealTarget.BuffCount("Slow-release Medpac") < 2 ||
                               Targeting.HealTarget.BuffTimeLeft("Slow-release Medpac") < 6),

                    //Smart and ground AoE healing require a real injured cluster.
                    Spell.Heal("Kolto Cloud", on => Targeting.AoeHealTarget, 90,
                        ret => Targeting.ShouldAoeHeal),
                    Spell.HealGround("Kolto Waves"),

                    //Spend only surplus Upper Hand outside emergencies.
                    Spell.Heal("Emergency Medpac", 80, ret => Core.Player.BuffCount("Upper Hand") >= 2),

                    //Underworld Medicine supplies Upper Hand; preserve the high-regeneration band.
                    Spell.Heal("Underworld Medicine", 75,
                        ret => Core.Player.EnergyPercent >= 60 || Targeting.HealTarget.HealthPercent <= 40),

                    //Free low-level and resource-recovery filler.
                    Spell.Heal("Diagnostic Scan", 95)
                    );
            }
        }
    }
}
