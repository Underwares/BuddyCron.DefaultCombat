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
    // 7.x Bodyguard Mercenary (healer).
    // Heat convention: Me.EnergyPercent is the REMAINING resource (100 = stone cold, 0 = fully
    // overheated), so "hot" == low EnergyPercent. Kolto Shot is the free heal / heat dump.
    // Healing lives in AreaOfEffect (the composite the framework runs before SingleTarget);
    // SingleTarget is the "nobody needs healing" dps filler.
    /// <summary>
    ///     Mercenary Bodyguard (healer) rotation: Kolto Shell upkeep and an Emergency Scan /
    ///     Healing Scan triage priority, with a dps filler for solo play.
    /// </summary>
    public class Bodyguard : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Bodyguard;

        public override string Name => "Mercenary Bodyguard";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Hunter's Boon")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Determination", ret => Me.IsStunned),

                    //Spend 10 stacks of Supercharge when the heal target actually needs it. Bodyguard's
                    //spender is "Supercharged Kolto Gas" (abl.bounty_hunter.skill.bodyguard.supercharged_gas_support);
                    //plain "Supercharged Gas" is the Innovative Ordnance one and never matches here.
                    Spell.Buff("Supercharged Kolto Gas",
                        ret => Me.InCombat && Me.BuffCount("Supercharge") >= 10
                               && HealTarget != null && HealTarget.HealthPercent <= 85),

                    //Heat
                    Spell.Buff("Vent Heat", ret => Me.InCombat && Me.EnergyPercent <= 40),

                    //Defensives -- these are what keep a leveling character alive
                    Spell.Buff("Energy Shield", ret => Me.HealthPercent <= 60),
                    Spell.Buff("Chaff Flare", ret => Me.HealthPercent <= 50),           //ability-tree choice (~43)
                    Spell.Buff("Kolto Overload", ret => Me.HealthPercent <= 35),
                    Spell.Buff("Responsive Safeguards", ret => Me.HealthPercent <= 25), //ability-tree choice (~68)
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
                    CombatMovement.CloseDistance(Distance.Ranged),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Interrupt
                    Spell.Cast("Disabling Shot", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Filler damage for solo/leveling -- only reached when nothing needs healing
                    Spell.Cast("Rail Shot", ret => Me.EnergyPercent >= 55),
                    Spell.Cast("Power Shot", ret => Me.EnergyPercent >= 70),

                    //Never stall: free basic attack
                    Spell.Cast("Rapid Shots")
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
                        //Spell.Cast("Cure", ret => HealTarget.ShouldDispel()), ((New Code Hold off for now))
                        Spell.Cleanse("Cure"),

                        //Keep the shield rolling on whoever we are healing
                        Spell.Heal("Kolto Shell", on => HealTarget, 100, ret => !HealTarget.HasBuff("Kolto Shell")),

                        //Emergency Scan is instant and cheap, so use it on cooldown
                        Spell.Heal("Emergency Scan", 90, ret => Me.InCombat),

                        //Procs worth reacting to: Emergency Scan makes the next Healing Scan instant,
                        //Supercharged Gas turns it into the burst heal
                        Spell.Heal("Healing Scan", 95, ret => Me.HasBuff("Emergency Response") && Me.InCombat),
                        //Bodyguard's spender is the ability "Supercharged Kolto Gas"; the aura it grants
                        //may be named either way, so accept both rather than silently miss the window.
                        Spell.Heal("Healing Scan", 95,
                            ret => (Me.HasBuff("Supercharged Kolto Gas") || Me.HasBuff("Supercharged Gas")) && Me.InCombat),

                        //AoE heal / Kolto Residue upkeep (Kolto Missile is cheap for what it heals).
                        //"Kolto Residue" is only the PASSIVE'S name (abl.bounty_hunter.skill.bodyguard.kolto_residue);
                        //the aura it actually lands on the ally is "Invigorated" (Buff, 45s) -- verified against the ability data.
                        new Decorator(ctx => HealTarget != null && Targeting.ShouldAoeHeal,
                            Spell.CastOnGround("Kolto Missile", on => HealTarget.Location, ret => Me.InCombat)),
                        new Decorator(ctx => HealTarget != null,
                            Spell.CastOnGround("Kolto Missile", on => HealTarget.Location,
                                ret => Me.InCombat && HealTarget.HealthPercent < 90 && !HealTarget.HasMyBuff("Invigorated"))),

                        //Single target priority
                        Spell.Heal("Progressive Scan", 90, ret => !Me.IsMoving),                //channelled, so stand still
                        Spell.Heal("Healing Scan", 85),                                         //Critical Efficiency makes this cheap
                        Spell.Heal("Rapid Scan", 65),                                           //expensive panic heal
                        Spell.Heal("Rapid Scan", 85, ret => Me.EnergyPercent >= 60),

                        //Filler -- free, instant, vents heat and builds Supercharge
                        Spell.Heal("Kolto Shot", 95)
                        );
            }
        }
    }
}
