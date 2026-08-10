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
    ///     Operative Concealment (burst melee dps) rotation: stealth Backstab opener, poison
    ///     upkeep detonated by Volatile Substance, Tactical Advantage spent on Laceration.
    /// </summary>
    public class Concealment : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Concealment;

        public override string Name => "Operative Concealment";

        /// <summary>
        ///     While stealthed with Backstab available we hold everything else so the opener is
        ///     always the (guaranteed-crit, TA-generating) stealth Backstab. If Backstab is on
        ///     cooldown we drop the hold rather than stall out of stealth.
        /// </summary>
        private static bool HoldForOpener => Me.IsStealthed && Me.Target != null &&
                                             AbilityManager.CanCast("Backstab", Me.Target).Success;

        public override Composite Buffs
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Coordination"),
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

                    //Raid buff - costs 1 Tactical Advantage
                    Spell.Buff("Tactical Superiority", ret => CombatHotkeys.EnableRaidBuffs && Me.HasBuff("Tactical Advantage")),

                    //Energy / Tactical Advantage economy
                    Spell.Cast("Adrenaline Probe", ret => Me.EnergyPercent <= 45),
                    Spell.Cast("Stim Boost", ret => Me.InCombat && Me.BuffCount("Tactical Advantage") < 2),

                    //Defensives
                    Spell.Buff("Shield Probe", ret => Me.HealthPercent <= 75),
                    Spell.Buff("Evasion", ret => Me.HealthPercent <= 50),

                    //Off-heals (Operatives keep these in every spec)
                    Spell.Cast("Kolto Infusion", on => Me, ret => Me.HealthPercent <= 40 && Me.HasBuff("Tactical Advantage")),
                    Spell.HoT("Kolto Probe", on => Me, 60),

                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15)
                    );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    Spell.Cast("Holotraverse", ret => CombatHotkeys.EnableCharge && Me.Target.Distance > .4f),

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Interrupt
                    Spell.Cast("Distraction", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Energy floor - free filler while Adrenaline Probe is down
                    Spell.Cast("Rifle Shot",
                        ret => Me.EnergyPercent < 45 && !AbilityManager.CanCast("Adrenaline Probe", Me).Success),

                    //Guarantee the stealth Backstab opener before entering the repeatable mini-cycle.
                    Spell.Cast("Backstab", ret => Me.IsStealthed),

                    //Poison upkeep gives Volatile Substance a detonator.
                    Spell.Cast("Corrosive Dart",
                        ret => !HoldForOpener &&
                               (!Me.Target.HasMyDebuff("Corrosive Dart") || Me.Target.DebuffTimeLeft("Corrosive Dart") <= 2)),

                    //Volatile Substance arms for three seconds. Crippling Slice occupies the intervening
                    //GCD before Backstab detonates it; missing optional abilities simply fall through.
                    Spell.Cast("Volatile Substance", ret => !HoldForOpener),
                    Spell.Cast("Crippling Slice",
                        ret => !HoldForOpener && Me.Target.HasMyDebuff("Volatile Substance")),
                    Spell.Cast("Backstab",
                        ret => !HoldForOpener && Me.Target.HasMyDebuff("Volatile Substance")),

                    //Spend Tactical Advantage, then use Backstab normally outside the mini-cycle.
                    Spell.Cast("Laceration", ret => !HoldForOpener && Me.HasBuff("Tactical Advantage")),
                    Spell.Cast("Backstab", ret => !HoldForOpener),

                    //Build Tactical Advantage (Shiv is the pre-Veiled Strike low level generator)
                    Spell.Cast("Veiled Strike", ret => !HoldForOpener && Me.BuffCount("Tactical Advantage") < 2),
                    Spell.Cast("Shiv", ret => !HoldForOpener && Me.BuffCount("Tactical Advantage") < 2),

                    //Cheap fillers
                    Spell.Cast("Crippling Slice", ret => !HoldForOpener),
                    Spell.Cast("Overload Shot", ret => !HoldForOpener && Me.EnergyPercent >= 85 && !Me.HasBuff("Tactical Advantage")),

                    //Never stall
                    Spell.Cast("Rifle Shot", ret => !HoldForOpener)
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new Decorator(ret => Targeting.ShouldAoe && !HoldForOpener,
                    new PrioritySelector(
                        Spell.Cast("Toxic Haze", ret => Me.HasBuff("Tactical Advantage")),
                        Spell.Cast("Noxious Knives"),
                        Spell.Cast("Fragmentation Grenade", ret => Me.EnergyPercent >= 60)
                    ));
            }
        }
    }
}
