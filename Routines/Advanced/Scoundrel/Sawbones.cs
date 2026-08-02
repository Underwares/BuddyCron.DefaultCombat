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
                    Spell.Buff("Escape", ret => Me.IsStunned),

                    //Raid buff - costs 1 Upper Hand
                    Spell.Buff("Stack the Deck", ret => CombatHotkeys.EnableRaidBuffs && Me.HasBuff("Upper Hand")),

                    //Energy / Upper Hand economy
                    Spell.Cast("Cool Head", ret => Me.EnergyPercent <= 40),
                    Spell.Cast("Pugnacity", ret => Me.InCombat && Me.BuffCount("Upper Hand") < 2),

                    //Ability tree choice (lvl 43) - resets Pugnacity / Kolto Cloud / Triage
                    Spell.Buff("Hot Streak",
                        ret => Me.InCombat && HealTarget != null && HealTarget.HealthPercent <= 50),

                    //Defensives
                    Spell.Buff("Defense Screen", ret => Me.HealthPercent <= 75),
                    Spell.Buff("Dodge", ret => Me.HealthPercent <= 50),
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15)
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
                    HeroicComposite,

                    //Interrupt
                    Spell.Cast("Distraction", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Filler damage
                    Spell.Cast("Vital Shot",
                        ret => Me.EnergyPercent >= 60 &&
                               (!Me.Target.HasMyDebuff("Vital Shot") || Me.Target.DebuffTimeLeft("Vital Shot") <= 2)),
                    Spell.Cast("Blaster Whip", ret => Me.BuffCount("Upper Hand") < 2),
                    Spell.Cast("Quick Shot", ret => Me.EnergyPercent >= 85),

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
                    //Spell.Cast("Triage", ret => HealTarget.ShouldDispel()), ((New Code Hold off for now))
                    Spell.Cleanse("Triage"),

                    //Emergency - free below 30%
                    Spell.Heal("Emergency Medpac", 30),

                    //Burst triage - costs 1 Upper Hand
                    Spell.Heal("Kolto Pack", 55, ret => Me.HasBuff("Upper Hand") && Me.EnergyPercent >= 45),

                    //AoE healing
                    Spell.Heal("Kolto Cloud", on => Tank ?? HealTarget, 90, ret => Targeting.ShouldAoeHeal),
                    Spell.HealGround("Kolto Waves"),

                    //Slow-release Medpac upkeep - two stacks is the core of the spec
                    Spell.Heal("Slow-release Medpac", on => Tank, 100,
                        ret => Tank != null && (Tank.BuffCount("Slow-release Medpac") < 2 ||
                                                Tank.BuffTimeLeft("Slow-release Medpac") < 6)),
                    Spell.Heal("Slow-release Medpac", 95,
                        ret => HealTarget != null && (HealTarget.BuffCount("Slow-release Medpac") < 2 ||
                                                      HealTarget.BuffTimeLeft("Slow-release Medpac") < 6)),

                    //Spend surplus Upper Hand
                    Spell.Heal("Emergency Medpac", 80, ret => Me.BuffCount("Upper Hand") >= 2),

                    //Big single target heal - generates Upper Hand
                    Spell.Heal("Underworld Medicine", 75),

                    //Energy regen filler
                    Spell.Heal("Diagnostic Scan", 95)
                    );
            }
        }
    }
}
