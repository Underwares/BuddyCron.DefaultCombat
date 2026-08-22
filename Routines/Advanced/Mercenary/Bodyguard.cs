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
    // 7.x Bodyguard Mercenary (healer).
    // Heat convention: Core.Player.EnergyPercent is the REMAINING resource (100 = stone cold, 0 = fully
    // overheated), so "hot" == low EnergyPercent. Kolto Shot is the free heal / heat dump.
    // Healing lives in AreaOfEffect (the composite the framework runs before SingleTarget);
    // SingleTarget is the "nobody needs healing" dps filler.
    //
    // Rotation per Xam Xam's 7.4 guide:
    //   - Emergency Scan -> Healing Scan combo off cooldown (core mechanic)
    //   - Progressive Scan off cooldown, ideally towards end of Supercharge
    //   - Supercharged Gas at 10 stacks; Emergency Scan right after for Concentrated Fire crit
    //   - Rapid Scan builds Critical Efficiency (3 stacks = free Healing Scan) and Power Barrier (DR)
    //   - Kolto Shell maintained on tank, refreshed on all during downtime
    //   - Kolto Shot woven in as free filler / heat dump / Supercharge builder
    //   - Healing Scan ONLY when proc'd (Emergency Response) or during Supercharged Gas or with
    //     3 Critical Efficiency stacks — never hardcast otherwise
    /// <summary>
    ///     Mercenary Bodyguard (healer) rotation: Kolto Shell upkeep, Emergency Scan / Healing Scan
    ///     combo triage, Progressive Scan off cooldown, with a dps filler for solo play.
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
                    Spell.Buff("Determination", ret => Core.Player.IsStunned),

                    //Spend 10 stacks of Supercharge during sustained healing. Current guide text uses
                    //Supercharged Gas, while the discipline-specific player record and aura use the
                    //Supercharged Kolto Gas name; exact lookup safely falls through to whichever is known.
                    //Emergency Scan right after this benefits from Concentrated Fire (crit) if equipped.
                    Spell.Buff("Supercharged Gas",
                        ret => Core.Player.InCombat && Core.Player.BuffCount("Supercharge") >= 10
                               && Targeting.HealTarget != null && Targeting.HealTarget.HealthPercent <= 85),
                    Spell.Buff("Supercharged Kolto Gas",
                        ret => Core.Player.InCombat && Core.Player.BuffCount("Supercharge") >= 10
                               && Targeting.HealTarget != null && Targeting.HealTarget.HealthPercent <= 85),

                    //Heat — Vent Heat dumps 50 heat over 3s and makes the next ability free
                    Spell.Buff("Vent Heat", ret => Core.Player.InCombat && Core.Player.EnergyPercent <= 40),

                    //Defensives — these are what keep a leveling character alive
                    Spell.Buff("Energy Shield", ret => Core.Player.HealthPercent <= 60),
                    Spell.Buff("Chaff Flare", ret => Core.Player.HealthPercent <= 50),           //ability-tree choice (~43)
                    Spell.Buff("Kolto Overload", ret => Core.Player.HealthPercent <= 35),
                    Spell.Buff("Responsive Safeguards", ret => Core.Player.HealthPercent <= 25), //ability-tree choice (~68)

                    //Power Surge: instant-cast next Rapid Scan when moving and hurt (6s interrupt immunity)
                    Spell.Buff("Power Surge", ret => Core.Player.IsMoving && Core.Player.HealthPercent <= 50),

                    Spell.Buff("Unity", ret => Core.Player.Companion != null && Core.Player.HealthPercent <= 15)
                    );
            }
        }

        /// <summary>
        ///     Dps filler for solo/leveling; only reached when nothing needs healing.
        ///     Off-DPS priority per guide: Electro Net -> Rail Shot -> Unload -> Power Shot ->
        ///     Fusion Missile (low heat only), with Rapid Shots as the free fallback.
        /// </summary>
        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    //Movement
                    CombatMovement.CloseDistance(Distance.Ranged),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    RotationRuntime.HeroicMoment,

                    //Interrupt
                    Spell.Cast("Disabling Shot", ret => Core.Player.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Electro Net — strong utility, ideally on enemy healers / ball carriers / melee
                    Spell.Cast("Electro Net", ret => Core.Player.Target != null && Core.Player.Target.StrongOrGreater()),

                    //Rail Shot / Mag Shot — Mag Shot is the Bodyguard alternative name
                    Spell.Cast("Rail Shot", ret => Core.Player.EnergyPercent >= 55),
                    Spell.Cast("Mag Shot", ret => Core.Player.EnergyPercent >= 55),

                    //Unload — channel, cannot be used while moving unless Thrill of the Hunt is taken
                    Spell.Cast("Unload", ret => !Core.Player.IsMoving && Core.Player.EnergyPercent >= 60),

                    //Power Shot — builds 1 stack of Supercharge
                    Spell.Cast("Power Shot", ret => Core.Player.EnergyPercent >= 70),

                    //Fusion Missile — low heat only
                    Spell.Cast("Fusion Missile", ret => Core.Player.EnergyPercent >= 80),

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
                        //Spell.Cast("Cure", ret => Targeting.HealTarget.ShouldDispel()), ((New Code Hold off for now))
                        Spell.Cleanse("Cure"),

                        //Maintain Kolto Shell on the active tank at all times (in combat and downtime).
                        //The guide: "Always refresh Shells on everyone during downtimes."
                        Spell.Heal("Kolto Shell", on => Targeting.Tank, 100,
                            ret => Targeting.Tank != null && !Targeting.Tank.HasMyBuff("Kolto Shell")),

                        //Kolto Shell on the current triage target when hurt
                        Spell.Heal("Kolto Shell", 85, ret => !Targeting.HealTarget.HasMyBuff("Kolto Shell")),

                        //Refresh Kolto Shell on self during downtime
                        Spell.Heal("Kolto Shell", on => Core.Player, 100,
                            ret => !Core.Player.InCombat && !Core.Player.HasMyBuff("Kolto Shell")),

                        //Emergency Scan is free (Emergency Response passive) and makes the next
                        //Healing Scan instant. Use off cooldown for the core combo.
                        Spell.Heal("Emergency Scan", 80, ret => Core.Player.InCombat),

                        //Healing Scan with Emergency Response proc (instant) or during Supercharged Gas
                        //(no cooldown, reduced heat). This is the primary burst heal combo.
                        Spell.Heal("Healing Scan", 80,
                            ret => Core.Player.InCombat && (Core.Player.HasBuff("Emergency Response") ||
                                                   Core.Player.HasBuff("Supercharged Gas") ||
                                                   Core.Player.HasBuff("Supercharged Kolto Gas"))),

                        //Progressive Scan off cooldown — spreads heals to nearby allies.
                        //Guide: "Use it off cooldown ideally towards the end of Supercharge."
                        Spell.Heal("Progressive Scan", 75, ret => !Core.Player.IsMoving),

                        //Kolto Missile for clustered AoE healing (HealGround checks ShouldAoeHeal).
                        //Also slows enemies in the area — useful in Huttball.
                        Spell.HealGround("Kolto Missile", ret => Core.Player.InCombat),

                        //Healing Scan with 3 Critical Efficiency stacks (free) or during Supercharged
                        //Gas (no cooldown, reduced heat). Never hardcast otherwise — the guide is
                        //explicit: "The only time you should be using Healing Scan is when it's proc'ed
                        //to instantly cast by Emergency Scan." At very low levels before the Critical
                        //Efficiency passive is learned, Healing Scan is an ordinary cooldown heal.
                        Spell.Heal("Healing Scan", 75,
                            ret => Core.Player.BuffCount("Critical Efficiency") >= 3 ||
                                   Core.Player.HasBuff("Supercharged Gas") ||
                                   Core.Player.HasBuff("Supercharged Kolto Gas") ||
                                   !AbilityManager.HasAbility("Critical Efficiency")),

                        //Rapid Scan builds Critical Efficiency (3 stacks = free Healing Scan) and
                        //Power Barrier (2% DR per stack, up to 3). Primary channel filler.
                        Spell.Heal("Rapid Scan", 75, ret => Core.Player.EnergyPercent >= 60),
                        Spell.Heal("Rapid Scan", 40, ret => Core.Player.EnergyPercent >= 45),

                        //Kolto Shot: free, instant, builds Supercharge, vents heat (Kolto Boosters).
                        //The only heal available at the earliest levels. Weave in as filler.
                        Spell.Heal("Kolto Shot", 95)
                        );
            }
        }
    }
}
