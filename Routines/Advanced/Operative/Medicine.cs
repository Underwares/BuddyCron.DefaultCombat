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
                    Spell.Buff("Escape", ret => Me.IsStunned),

                    //Raid buff - costs 1 Tactical Advantage
                    Spell.Buff("Tactical Superiority", ret => CombatHotkeys.EnableRaidBuffs && Me.HasBuff("Tactical Advantage")),

                    //Energy / Tactical Advantage economy
                    Spell.Cast("Adrenaline Probe", ret => Me.EnergyPercent <= 40),
                    Spell.Cast("Stim Boost", ret => Me.InCombat && Me.BuffCount("Tactical Advantage") < 2),

                    //Ability tree choice - resets Stim Boost / Recuperative Nanotech / Toxin Scan
                    Spell.Buff("Tactical Overdrive",
                        ret => Me.InCombat && HealTarget != null && HealTarget.HealthPercent <= 50),

                    //Defensives
                    Spell.Buff("Shield Probe", ret => Me.HealthPercent <= 75),
                    Spell.Buff("Evasion", ret => Me.HealthPercent <= 50),
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15)
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
                    HeroicComposite,

                    //Interrupt
                    Spell.Cast("Distraction", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Filler damage - only ever reached when nothing needs healing (solo / leveling)
                    Spell.Cast("Corrosive Dart",
                        ret => Me.EnergyPercent >= 60 &&
                               (!Me.Target.HasMyDebuff("Corrosive Dart") || Me.Target.DebuffTimeLeft("Corrosive Dart") <= 2)),
                    Spell.Cast("Shiv", ret => Me.BuffCount("Tactical Advantage") < 2),
                    Spell.Cast("Overload Shot", ret => Me.EnergyPercent >= 85),

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
                    //Spell.Cast("Toxin Scan", ret => HealTarget.ShouldDispel()), ((New Code Hold off for now))
                    Spell.Cleanse("Toxin Scan"),

                    //Emergency - free below 30%
                    Spell.Heal("Surgical Probe", 30),

                    //Burst triage - costs 1 Tactical Advantage
                    Spell.Heal("Kolto Infusion", 55, ret => Me.HasBuff("Tactical Advantage") && Me.EnergyPercent >= 45),

                    //AoE healing
                    Spell.Heal("Recuperative Nanotech", on => Tank ?? HealTarget, 90, ret => Targeting.ShouldAoeHeal),
                    Spell.HealGround("Kolto Waves"),

                    //Kolto Probe upkeep - two stacks is the core of the spec
                    Spell.Heal("Kolto Probe", on => Tank, 100,
                        ret => Tank != null && (Tank.BuffCount("Kolto Probe") < 2 || Tank.BuffTimeLeft("Kolto Probe") < 6)),
                    Spell.Heal("Kolto Probe", 95,
                        ret => HealTarget != null && (HealTarget.BuffCount("Kolto Probe") < 2 || HealTarget.BuffTimeLeft("Kolto Probe") < 6)),

                    //Spend surplus Tactical Advantage
                    Spell.Heal("Surgical Probe", 80, ret => Me.BuffCount("Tactical Advantage") >= 2),

                    //Big single target heal - generates Tactical Advantage
                    Spell.Heal("Kolto Injection", 75),

                    //Energy regen filler
                    Spell.Heal("Diagnostic Scan", 95)
                    );
            }
        }
    }
}
