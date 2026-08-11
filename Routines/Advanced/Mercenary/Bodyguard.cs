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
                    Spell.Buff("Determination", ret => Core.Player.IsStunned),

                    //Spend 10 stacks of Supercharge during sustained healing. Current guide text uses
                    //Supercharged Gas, while the discipline-specific player record and aura use the
                    //Supercharged Kolto Gas name; exact lookup safely falls through to whichever is known.
                    Spell.Buff("Supercharged Gas",
                        ret => Core.Player.InCombat && Core.Player.BuffCount("Supercharge") >= 10
                               && Targeting.HealTarget != null && Targeting.HealTarget.HealthPercent <= 85),
                    Spell.Buff("Supercharged Kolto Gas",
                        ret => Core.Player.InCombat && Core.Player.BuffCount("Supercharge") >= 10
                               && Targeting.HealTarget != null && Targeting.HealTarget.HealthPercent <= 85),

                    //Heat
                    Spell.Buff("Vent Heat", ret => Core.Player.InCombat && Core.Player.EnergyPercent <= 40),

                    //Defensives -- these are what keep a leveling character alive
                    Spell.Buff("Energy Shield", ret => Core.Player.HealthPercent <= 60),
                    Spell.Buff("Chaff Flare", ret => Core.Player.HealthPercent <= 50),           //ability-tree choice (~43)
                    Spell.Buff("Kolto Overload", ret => Core.Player.HealthPercent <= 35),
                    Spell.Buff("Responsive Safeguards", ret => Core.Player.HealthPercent <= 25), //ability-tree choice (~68)
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
                    CombatMovement.CloseDistance(Distance.Ranged),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    RotationRuntime.HeroicMoment,

                    //Interrupt
                    Spell.Cast("Disabling Shot", ret => Core.Player.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Filler damage for solo/leveling -- only reached when nothing needs healing
                    Spell.Cast("Rail Shot", ret => Core.Player.EnergyPercent >= 55),
                    Spell.Cast("Power Shot", ret => Core.Player.EnergyPercent >= 70),

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

                        //Maintain the charge-based shell on the active tank and current triage target.
                        Spell.Heal("Kolto Shell", on => Targeting.Tank, 100,
                            ret => Targeting.Tank != null && Targeting.Tank.InCombat && !Targeting.Tank.HasMyBuff("Kolto Shell")),
                        Spell.Heal("Kolto Shell", 85, ret => !Targeting.HealTarget.HasMyBuff("Kolto Shell")),

                        //Emergency Scan is free and makes the following Healing Scan instant.
                        Spell.Heal("Emergency Scan", 80, ret => Core.Player.InCombat),
                        Spell.Heal("Healing Scan", 80,
                            ret => Core.Player.InCombat && (Core.Player.HasBuff("Emergency Response") ||
                                                   Core.Player.HasBuff("Supercharged Gas") ||
                                                   Core.Player.HasBuff("Supercharged Kolto Gas"))),

                        //Channels and clustered healing precede ordinary fillers.
                        Spell.Heal("Progressive Scan", 75, ret => !Core.Player.IsMoving),
                        Spell.HealGround("Kolto Missile", ret => Core.Player.InCombat),
                        new Decorator(ctx => Targeting.HealTarget != null,
                            Spell.CastOnGround("Kolto Missile", on => Targeting.HealTarget.Location,
                                ret => Core.Player.InCombat && Targeting.HealTarget.HealthPercent < 85 &&
                                       !Targeting.HealTarget.HasMyBuff("Invigorated"))),

                        //Rapid Scan builds Critical Efficiency. Spend capped stacks before another
                        //Rapid Scan can win the priority; before the passive is learned, use Healing
                        //Scan as an ordinary cooldown heal.
                        Spell.Heal("Healing Scan", 75,
                            ret => Core.Player.BuffCount("Critical Efficiency") >= 3 ||
                                   !AbilityManager.HasAbility("Critical Efficiency")),
                        Spell.Heal("Rapid Scan", 75, ret => Core.Player.EnergyPercent >= 60),
                        Spell.Heal("Rapid Scan", 40, ret => Core.Player.EnergyPercent >= 45),

                        //Free, instant recovery filler and the only heal available at the earliest levels.
                        Spell.Heal("Kolto Shot", 95)
                        );
            }
        }
    }
}
