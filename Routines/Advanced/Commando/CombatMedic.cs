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
    // 7.x Combat Medic Commando (healer, Republic mirror of Bodyguard Mercenary).
    // Cell convention: Core.Player.EnergyPercent is the REMAINING resource (100 = full Energy Cells,
    // 0 = empty), so "starved" == low EnergyPercent. Med Shot is the free filler heal that lets
    // cells regenerate and builds Supercharge.
    // Healing lives in AreaOfEffect (the composite the framework runs before SingleTarget);
    // SingleTarget is the "nobody needs healing" dps filler.
    /// <summary>
    ///     Commando Combat Medic (healer) rotation: Trauma Probe upkeep and a Bacta Infusion /
    ///     Advanced Medical Probe triage priority, with a dps filler for solo play.
    /// </summary>
    public class CombatMedic : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.CombatMedic;

        public override string Name => "Commando Combat Medic";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Fortification")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Tenacity", ret => Core.Player.IsStunned),

                    //Spend 10 stacks of Supercharge during sustained healing. Current guide text uses
                    //Supercharged Cell, while the discipline-specific player record and aura use the
                    //Supercharged Kolto Cell name; exact lookup safely falls through to whichever is known.
                    Spell.Buff("Supercharged Cell",
                        ret => Core.Player.InCombat && Core.Player.BuffCount("Supercharge") >= 10
                               && Targeting.HealTarget != null && Targeting.HealTarget.HealthPercent <= 85),
                    Spell.Buff("Supercharged Kolto Cell",
                        ret => Core.Player.InCombat && Core.Player.BuffCount("Supercharge") >= 10
                               && Targeting.HealTarget != null && Targeting.HealTarget.HealthPercent <= 85),

                    //Cells
                    Spell.Buff("Recharge Cells", ret => Core.Player.InCombat && Core.Player.EnergyPercent <= 40),

                    //Defensives -- these are what keep a leveling character alive
                    Spell.Buff("Reactive Shield", ret => Core.Player.HealthPercent <= 60),
                    Spell.Buff("Diversion", ret => Core.Player.HealthPercent <= 50),            //ability-tree choice
                    Spell.Buff("Adrenaline Rush", ret => Core.Player.HealthPercent <= 35),
                    Spell.Buff("Echoing Deterrence", ret => Core.Player.HealthPercent <= 25),   //ability-tree choice
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
                    Spell.Cast("High Impact Bolt", ret => Core.Player.EnergyPercent >= 55),
                    Spell.Cast("Charged Bolts", ret => Core.Player.EnergyPercent >= 70),

                    //Never stall: free basic attack
                    Spell.Cast("Hammer Shot")
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
                        //Spell.Cast("Field Aid", ret => Targeting.HealTarget.ShouldDispel()), ((New Code Hold off for now))
                        Spell.Cleanse("Field Aid"),

                        //Maintain the charge-based probe on the active tank and current triage target.
                        Spell.Heal("Trauma Probe", on => Targeting.Tank, 100,
                            ret => Targeting.Tank != null && Targeting.Tank.InCombat && !Targeting.Tank.HasMyBuff("Trauma Probe")),
                        Spell.Heal("Trauma Probe", 85, ret => !Targeting.HealTarget.HasMyBuff("Trauma Probe")),

                        //Bacta Infusion is free and makes the following Advanced Medical Probe instant.
                        Spell.Heal("Bacta Infusion", 80, ret => Core.Player.InCombat),
                        Spell.Heal("Advanced Medical Probe", 80,
                            ret => Core.Player.InCombat && (Core.Player.HasBuff("Emergency Response") ||
                                                   Core.Player.HasBuff("Supercharged Cell") ||
                                                   Core.Player.HasBuff("Supercharged Kolto Cell"))),

                        //Channels and clustered healing precede ordinary fillers.
                        Spell.Heal("Successive Treatment", 75, ret => !Core.Player.IsMoving),
                        Spell.HealGround("Kolto Bomb", ret => Core.Player.InCombat),
                        new Decorator(ctx => Targeting.HealTarget != null,
                            Spell.CastOnGround("Kolto Bomb", on => Targeting.HealTarget.Location,
                                ret => Core.Player.InCombat && Targeting.HealTarget.HealthPercent < 85 &&
                                       !Targeting.HealTarget.HasMyBuff("Invigorated"))),

                        //Medical Probe builds Field Triage. Spend capped stacks before another
                        //Medical Probe can win the priority; before the passive is learned, use
                        //Advanced Medical Probe as an ordinary cooldown heal.
                        Spell.Heal("Advanced Medical Probe", 75,
                            ret => Core.Player.BuffCount("Field Triage") >= 3 ||
                                   !AbilityManager.HasAbility("Field Triage")),
                        Spell.Heal("Medical Probe", 75, ret => Core.Player.EnergyPercent >= 60),
                        Spell.Heal("Medical Probe", 40, ret => Core.Player.EnergyPercent >= 45),

                        //Free, instant recovery filler and the only heal available at the earliest levels.
                        Spell.Heal("Med Shot", 95)
                        );
            }
        }
    }
}
