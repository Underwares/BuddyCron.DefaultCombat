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
    ///     Operative Medicine (healer) rotation: two-stack Kolto Probe upkeep and a Surgical
    ///     Probe / Kolto Injection triage priority, with a dps filler for solo play.
    /// </summary>
    public class Medicine : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Medicine;

        public override string Name => "Operative Medicine";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Coordination")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Escape", ret => Core.Player.IsStunned),

                    //Raid buff - costs 1 Tactical Advantage
                    Spell.Buff("Tactical Superiority", ret => CombatHotkeys.EnableRaidBuffs && Core.Player.HasBuff("Tactical Advantage")),

                    //Energy / Tactical Advantage economy
                    Spell.Cast("Adrenaline Probe", ret => Core.Player.EnergyPercent <= 40),
                    Spell.Cast("Stim Boost", ret => Core.Player.InCombat && Core.Player.BuffCount("Tactical Advantage") < 2),

                    //Ability tree choice - resets Stim Boost / Recuperative Nanotech / Toxin Scan
                    Spell.Buff("Tactical Overdrive",
                        ret => Core.Player.InCombat && Targeting.HealTarget != null && Targeting.HealTarget.HealthPercent <= 50),

                    //Defensives
                    Spell.Buff("Shield Probe", ret => Core.Player.HealthPercent <= 75),
                    Spell.Buff("Evasion", ret => Core.Player.HealthPercent <= 50),
                    Spell.Buff("Unity", ret => Core.Player.Companion != null && Core.Player.HealthPercent <= 15)
                    );
            }
        }

        /// <summary>
        ///     Dps filler for solo/leveling; only reached when nothing needs healing.
        /// </summary>
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

                    //Filler damage - only ever reached when nothing needs healing (solo / leveling)
                    Spell.Cast("Corrosive Dart",
                        ret => Core.Player.EnergyPercent >= 60 &&
                               (!Core.Player.Target.HasMyDebuff("Corrosive Dart") || Core.Player.Target.DebuffTimeLeft("Corrosive Dart") <= 2)),
                    Spell.Cast("Shiv", ret => Core.Player.BuffCount("Tactical Advantage") < 2),
                    Spell.Cast("Overload Shot", ret => Core.Player.EnergyPercent >= 85),

                    //Never stall
                    Spell.Cast("Rifle Shot")
                    );
            }
        }

        /// <summary>
        ///     The healing priority. Runs from the AoE slot, which the framework evaluates before
        ///     <see cref="SingleTarget"/>, so healing always pre-empts the dps filler.
        /// </summary>
        public override Composite AreaOfEffect
        {
            get
            {
                return new PrioritySelector(

                    //Cleanse
                    //Spell.Cast("Toxin Scan", ret => Targeting.HealTarget.ShouldDispel()), ((New Code Hold off for now))
                    Spell.Cleanse("Toxin Scan"),

                    //Emergency - free below 30%
                    Spell.Heal("Surgical Probe", 30),

                    //Burst triage - preserve one Tactical Advantage outside the emergency range.
                    Spell.Heal("Kolto Infusion", 55,
                        ret => (Core.Player.BuffCount("Tactical Advantage") >= 2 || Targeting.HealTarget.HealthPercent <= 35) &&
                               Core.Player.EnergyPercent >= 45),

                    //Kolto Probe upkeep - two stacks on the tank and current triage target.
                    Spell.Heal("Kolto Probe", on => Targeting.Tank, 100,
                        ret => Targeting.Tank != null && Targeting.Tank.InCombat &&
                               (Targeting.Tank.BuffCount("Kolto Probe") < 2 || Targeting.Tank.BuffTimeLeft("Kolto Probe") < 6)),
                    Spell.Heal("Kolto Probe", 90,
                        ret => Targeting.HealTarget.BuffCount("Kolto Probe") < 2 || Targeting.HealTarget.BuffTimeLeft("Kolto Probe") < 6),

                    //Smart and ground AoE healing require a real injured cluster.
                    Spell.Heal("Recuperative Nanotech", on => Targeting.AoeHealTarget, 90,
                        ret => Targeting.ShouldAoeHeal),
                    Spell.HealGround("Kolto Waves"),

                    //Spend only surplus Tactical Advantage outside emergencies.
                    Spell.Heal("Surgical Probe", 80, ret => Core.Player.BuffCount("Tactical Advantage") >= 2),

                    //Kolto Injection supplies Tactical Advantage; preserve the high-regeneration band.
                    Spell.Heal("Kolto Injection", 75,
                        ret => Core.Player.EnergyPercent >= 60 || Targeting.HealTarget.HealthPercent <= 40),

                    //Free low-level and resource-recovery filler.
                    Spell.Heal("Diagnostic Scan", 95)
                    );
            }
        }
    }
}
