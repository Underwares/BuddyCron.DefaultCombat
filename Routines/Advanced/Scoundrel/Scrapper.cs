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
using Targeting = DefaultCombat.Core.Targeting;

namespace DefaultCombat.Routines
{
    /// <summary>
    ///     7.x Scoundrel Scrapper (melee burst DPS) rotation. Holds the priority while stealthed
    ///     so the opener is always the stealth Back Blast.
    /// </summary>
    public class Scrapper : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Scrapper;

        public override string Name => "Scoundrel Scrapper";

        /// <summary>
        ///     While stealthed with Back Blast available we hold everything else so the opener is
        ///     always the (auto-crit, Upper-Hand-generating, Flechette Round applying) stealth Back
        ///     Blast. If Back Blast is on cooldown we drop the hold rather than stall out of stealth.
        /// </summary>
        private static bool HoldForOpener => Me.IsStealthed && Me.Target != null &&
                                             AbilityManager.CanCast("Back Blast", Me.Target).Success;

        public override Composite Buffs
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Lucky Shots"),
					Spell.Buff("Stealth", ret => !Rest.KeepResting() && !DefaultCombat.MovementDisabled && !Me.IsMounted)
                    );
            }
        }

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Escape", ret => Me.IsStunned),

                    //Raid buff - costs 1 Upper Hand
                    Spell.Buff("Stack the Deck", ret => CombatHotkeys.EnableRaidBuffs && Me.HasBuff("Upper Hand")),

                    //Energy / Upper Hand economy
                    Spell.Cast("Cool Head", ret => Me.EnergyPercent <= 45),
                    Spell.Cast("Pugnacity", ret => Me.InCombat && Me.BuffCount("Upper Hand") < 2),

                    //Ability tree choice (lvl 43) - resets Pugnacity / off-heals, save it for real fights
                    Spell.Buff("Hot Streak",
                        ret => Me.InCombat && Me.Target != null && Me.Target.StrongOrGreater()),

                    //Defensives
                    Spell.Buff("Defense Screen", ret => Me.HealthPercent <= 75),
                    Spell.Buff("Dodge", ret => Me.HealthPercent <= 50),

                    //Off-heals (Scoundrels keep these in every spec)
                    Spell.Cast("Kolto Pack", on => Me, ret => Me.HealthPercent <= 40 && Me.HasBuff("Upper Hand")),
                    Spell.HoT("Slow-release Medpac", on => Me, 60),

                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15)
                    );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    //Gap closer (ability tree choice, lvl 73)
                    Spell.Cast("Trick Move", ret => CombatHotkeys.EnableCharge && Me.Target.Distance > .4f),

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Interrupt
                    Spell.Cast("Distraction", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Energy floor - free filler while Cool Head is down
                    Spell.Cast("Flurry of Bolts",
                        ret => Me.EnergyPercent < 60 && !AbilityManager.CanCast("Cool Head", Me).Success),

                    //Guarantee the stealth Back Blast opener before entering the repeatable mini-cycle.
                    Spell.Cast("Back Blast", ret => Me.IsStealthed),

                    //Bleed upkeep gives Blood Boiler a detonator and remains the low-level fallback.
                    Spell.Cast("Vital Shot",
                        ret => !HoldForOpener &&
                               (!Me.Target.HasMyDebuff("Vital Shot") || Me.Target.DebuffTimeLeft("Vital Shot") <= 2)),

                    //Blood Boiler arms for three seconds. Shank Shot occupies the intervening GCD
                    //before Back Blast detonates it; missing optional abilities simply fall through.
                    Spell.Cast("Blood Boiler", ret => !HoldForOpener),
                    Spell.Cast("Shank Shot",
                        ret => !HoldForOpener && Me.Target.HasMyDebuff("Blood Boiler")),
                    Spell.Cast("Back Blast",
                        ret => !HoldForOpener && Me.Target.HasMyDebuff("Blood Boiler")),

                    //Spend Upper Hand, then use Back Blast normally outside the mini-cycle.
                    Spell.Cast("Sucker Punch", ret => !HoldForOpener && Me.HasBuff("Upper Hand")),
                    Spell.Cast("Back Blast", ret => !HoldForOpener),

                    //Build Upper Hand (Blaster Whip is the pre-Bludgeon low level generator)
                    Spell.Cast("Bludgeon", ret => !HoldForOpener && Me.BuffCount("Upper Hand") < 2),
                    Spell.Cast("Blaster Whip", ret => !HoldForOpener && Me.BuffCount("Upper Hand") < 2),

                    //Cheap fillers
                    Spell.Cast("Shank Shot", ret => !HoldForOpener),
                    Spell.Cast("Quick Shot", ret => !HoldForOpener && Me.EnergyPercent >= 85 && !Me.HasBuff("Upper Hand")),

                    //Never stall
                    Spell.Cast("Flurry of Bolts", ret => !HoldForOpener)
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new Decorator(ret => Targeting.ShouldAoe && !HoldForOpener,
                    new PrioritySelector(
                        Spell.Cast("Bushwhack", ret => Me.HasBuff("Upper Hand")),
                        Spell.Cast("Lacerating Blast"),
                        Spell.Cast("Thermal Grenade", ret => Me.EnergyPercent >= 60)
                    ));
            }
        }
    }
}
