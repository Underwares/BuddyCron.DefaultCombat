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
    ///     7.x Sorcerer Corruption (healing) rotation. Healing lives in AreaOfEffect;
    ///     SingleTarget is Force-gated filler damage so a solo healer can still kill things.
    /// </summary>
    public class Corruption : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Corruption;

        public override string Name => "Sorcerer Corruption";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Mark of Power")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(

                    //Break CC
                    Spell.Buff("Unbreakable Will", ret => Me.IsStunned),

                    //Defensives
                    Spell.Buff("Force Barrier", ret => Me.HealthPercent <= 15),
                    Spell.Buff("Unnatural Preservation", ret => Me.HealthPercent <= 60),
                    Spell.HoT("Static Barrier", on => Me, 100, ret => Me.InCombat && !Me.HasDebuff("Deionized")),

                    //Force management: dump Force Surge stacks with Consuming Darkness (no Weary when stacked)
                    Spell.Buff("Consuming Darkness", ret => NeedForce()),

                    //Healing cooldowns
                    Spell.Cast("Recklessness", ret => Targeting.ShouldAoeHeal),
                    Spell.Cast("Polarity Shift", ret => Targeting.ShouldAoeHeal),
                    Spell.Buff("Unlimited Power", ret => CombatHotkeys.EnableRaidBuffs),

                    //Companion
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15)
                );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    //Movement
                    CombatMovement.CloseDistance(Distance.Ranged),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Filler damage so a solo/leveling healer can actually kill things.
                    //Only runs when nothing above (heals live in AreaOfEffect) wanted the GCD.
                    new Decorator(ret => Me.Target != null && Me.Target.IsHostile && !Me.Target.IsDead,
                        new PrioritySelector(
                            Spell.Cast("Jolt", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),
                            Spell.DoT("Affliction", "Affliction", 0, ret => Me.ForcePercent >= 50),
                            Spell.Cast("Volt Rush", ret => Me.ForcePercent >= 50),   // lvl 68 choice, skipped if untrained
                            Spell.Cast("Shock", ret => Me.ForcePercent >= 60),
                            Spell.Cast("Lightning Strike", ret => Me.ForcePercent >= 70),
                            Spell.Cast("Saber Strike")
                            ))
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new PrioritySelector(

                    //Cleanse (Purge was renamed Expunge)
                    Spell.Cleanse("Expunge"),

                    //Emergency Heal (Insta-cast, free, guaranteed crit)
                    Spell.Heal("Dark Heal", 80, ret => Me.HasBuff("Dark Concentration")),

                    //AoE Healing
                    Spell.HealGround("Revivification"),

                    //Spend Force Bending: Roaming Mend > Innervate > Dark Infusion
                    new Decorator(ret => Me.HasBuff("Force Bending"),
                        new PrioritySelector(
                            Spell.Heal("Roaming Mend", 95),
                            Spell.Heal("Innervate", 90),
                            Spell.Heal("Dark Infusion", 60)
                            )),

                    //Build Force Bending (Resurgence on cooldown)
                    Spell.HoT("Resurgence", 95),
                    Spell.HoT("Resurgence", on => Tank, 100, ret => Tank != null && Tank.InCombat),

                    //Bubble the tank / heal target, Static Barrier is the cheapest heal per GCD
                    Spell.HoT("Static Barrier", on => Tank, 100,
                        ret => Tank != null && Tank.InCombat && !Tank.HasDebuff("Deionized")),
                    Spell.HoT("Static Barrier", 99, ret => HealTarget != null && !HealTarget.HasDebuff("Deionized")),

                    //Innervate on cooldown: cheap, and it builds Force Surge
                    Spell.Heal("Innervate", 85),

                    //Roaming Mend on cooldown, prefer the tank
                    Spell.Heal("Roaming Mend", onUnit => Tank, 100, ret => Tank != null && Tank.InCombat),
                    Spell.Heal("Roaming Mend", 95),

                    //Single Target Healing
                    Spell.Heal("Dark Heal", 35),
                    Spell.Heal("Dark Infusion", 80));
            }
        }

        /// <summary>
        ///     True when Consuming Darkness should be used: with Force Surge stacks below 80%
        ///     Force (no Weary penalty), or starved at 20% Force or less without Weary already up.
        /// </summary>
        private bool NeedForce()
        {
            //Force Surge stacks (from Innervate crits) make Consuming Darkness free of the Weary penalty.
            if (Me.HasBuff("Force Surge") && Me.ForcePercent < 80)
                return true;

            //Starved: take the Weary hit rather than stall out with no Force.
            if (Me.ForcePercent <= 20 && !Me.HasDebuff("Weary"))
                return true;

            return false;
        }
    }
}
