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

                    //Emergency Heal (Insta-cast) -- Altruism makes Benevolence instant and free
                    Spell.Heal("Benevolence", 80, ret => Me.HasBuff("Altruism")),

                    //AoE Healing
                    new Decorator(ctx => Targeting.ShouldAoeHeal && Tank != null,
                        Spell.CastOnGround("Salvation", on => Tank.Location)),
                    Spell.Heal("Wandering Mend", 90, ret => Targeting.ShouldAoeHeal),

                    //Single Target Healing
                    Spell.HoT("Force Armor", 90, ret => HealTarget != null && !HealTarget.HasDebuff("Force-imbalanced")),

                    //Build Conveyance (Rejuvenate) -- it buffs the next Healing Trance / Deliverance
                    Spell.HoT("Rejuvenate", 90),

                    //Use Conveyance
                    new Decorator(ret => Me.HasBuff("Conveyance"),
                        new PrioritySelector(
                            Spell.Heal("Healing Trance", 90),
                            Spell.Heal("Deliverance", 70)
                            )),

                    //Healing Trance on cooldown -- crits build Resplendence for Vindicate
                    Spell.Heal("Healing Trance", 85),

                    //Buff Tank
                    Spell.HoT("Force Armor", on => Tank, 100, ret => Tank != null && Tank.InCombat && !Tank.HasDebuff("Force-imbalanced")),
                    Spell.HoT("Rejuvenate", onUnit => Tank, 100, ret => Tank != null && Tank.InCombat),

                    //Single Target Healing
                    Spell.Heal("Wandering Mend", 85),
                    Spell.Heal("Benevolence", 40),
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
