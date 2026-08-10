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
    ///     7.x Sage Seer (healing) rotation. Healing lives in AreaOfEffect; SingleTarget is
    ///     Force-gated filler damage so a solo Seer can still kill things.
    /// </summary>
    public class Seer : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Seer;

        public override string Name => "Sage Seer";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Force Valor")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Force of Will", ret => Me.IsStunned),

                    //Defensives
                    Spell.Buff("Force Barrier", ret => Me.HealthPercent <= 20),
                    Spell.Buff("Force Mend", ret => Me.HealthPercent <= 75),
                    Spell.Buff("Force Armor", ret => Me.InCombat && !Me.HasDebuff("Force-imbalanced")),

                    //Throughput cooldowns -- saved for when the group is actually taking damage
                    Spell.Buff("Force Empowerment", ret => CombatHotkeys.EnableRaidBuffs),
                    Spell.Cast("Force Potency", ret => Targeting.ShouldAoeHeal),
                    Spell.Cast("Mental Alacrity", ret => Targeting.ShouldAoeHeal),

                    //Force management -- Vindicate costs health, so only when we can afford it.
                    //Resplendence (Healing Trance crits) makes it cheaper and stronger.
                    Spell.Cast("Vindicate", ret => NeedForce()),

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

                    //Interrupt
                    Spell.Cast("Mind Snap", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Damage -- a Seer still has to kill things while levelling/farming. Everything that
                    //costs Force is gated so the heal budget is never spent down to nothing.
                    new Decorator(ret => Me.ForcePercent >= 50,
                        new PrioritySelector(
                            Spell.DoT("Weaken Mind", "Weaken Mind"),

                            //Mind Crush and Disturbance proc Altruism (free instant Benevolence)
                            Spell.Cast("Mind Crush"),

                            //Telekinetic Blitz is an ability-tree choice (lvl 68) -- skipped if not taken
                            Spell.Cast("Telekinetic Blitz"),

                            Spell.Cast("Project", ret => Me.Target.HasMyDebuff("Crushed (Force)") || Me.Level < 30),
                            Spell.Cast("Telekinetic Throw"),
                            Spell.Cast("Disturbance")
                            )),

                    //Free filler so the rotation can never stall (and never starves the heals)
                    Spell.Cast("Saber Strike", ret => Me.Target.Distance <= Distance.Melee)
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new PrioritySelector(

                    //Cleanse
                    //Spell.Cast("Restoration", ret => HealTarget.ShouldDispel()), ((New Code Hold off for now))
                    Spell.Cleanse("Restoration"),

                    //Use the instant, free Altruism heal for urgent triage.
                    Spell.Heal("Benevolence", 80, ret => Me.HasBuff("Altruism")),

                    //Prevent predictable damage, then build Conveyance with Rejuvenate.
                    Spell.HoT("Force Armor", 90, ret => !HealTarget.HasDebuff("Force-imbalanced")),
                    Spell.HoT("Rejuvenate", 95),

                    //Spend Conveyance on the efficient channel before other consumers.
                    new Decorator(ret => Me.HasBuff("Conveyance"),
                        new PrioritySelector(
                            Spell.Heal("Healing Trance", 90),
                            Spell.Heal("Wandering Mend", 90,
                                ret => !Me.HasBuff("Wandering Mend Charges")),
                            Spell.Heal("Deliverance", 60)
                            )),

                    //Healing Trance builds Resplendence; Mend is strongest when several allies are hurt.
                    Spell.Heal("Healing Trance", 85),
                    Spell.Heal("Wandering Mend", 85,
                        ret => !Me.HasBuff("Wandering Mend Charges") &&
                               (Targeting.ShouldAoeHeal || HealTarget.HealthPercent <= 60)),

                    //Use the ground heal only for a sustained, tightly grouped raid-healing check.
                    Spell.HealGround("Salvation", ret => Targeting.AoeHealCount >= 4),

                    //Maintain preventative effects on the active tank after immediate triage.
                    Spell.HoT("Force Armor", on => Tank, 100,
                        ret => Tank != null && Tank.InCombat && !Tank.HasDebuff("Force-imbalanced")),
                    Spell.HoT("Rejuvenate", on => Tank, 100, ret => Tank != null && Tank.InCombat),

                    //Deliverance is the efficient direct filler. At the earliest levels, Benevolence
                    //must fill that role because the character has not learned Deliverance yet.
                    Spell.Heal("Benevolence", 80, ret => Me.Level < 15),
                    Spell.Heal("Benevolence", 35),
                    Spell.Heal("Deliverance", 80)
                    );
            }
        }

        /// <summary>
        ///     True when Vindicate should be used: below 40% Force, or below 80% Force with
        ///     Resplendence up (which offsets its health cost) - never below 50% health.
        /// </summary>
        private bool NeedForce()
        {
            if (Me.HealthPercent < 50)
                return false;
            if (Me.HasBuff("Resplendence") && Me.ForcePercent < 80)
                return true;
            return Me.ForcePercent < 40;
        }
    }
}
