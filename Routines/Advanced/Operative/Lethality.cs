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
    ///     Operative Lethality (DoT melee dps) rotation: stealth Lethal Strike opener, keeps the
    ///     Corrosive DoTs at full uptime and spends Tactical Advantage on Corrosive Assault.
    /// </summary>
    public class Lethality : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Lethality;

        public override string Name => "Operative Lethality";

        /// <summary>
        ///     While stealthed with Lethal Strike available we hold everything else so the opener
        ///     is always the stealth Lethal Strike (grants Tactical Advantage + Augmented Toxins).
        ///     If Lethal Strike is on cooldown we drop the hold rather than stall out of stealth.
        /// </summary>
        private static bool HoldForOpener => Me.IsStealthed && Me.Target != null &&
                                             AbilityManager.CanCast("Lethal Strike", Me.Target).Success;

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

                    //Ability tree choice - resets cooldowns, save it for real fights
                    Spell.Buff("Tactical Overdrive",
                        ret => Me.InCombat && Me.Target != null && Me.Target.StrongOrGreater()),

                    //Defensives
                    Spell.Buff("Shield Probe", ret => Me.HealthPercent <= 80),
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
                        ret => Me.EnergyPercent < 35 && !AbilityManager.CanCast("Adrenaline Probe", Me).Success),

                    //Stealth opener / on cooldown
                    Spell.Cast("Lethal Strike"),

                    //DoT upkeep - 100% uptime is the whole spec. Kept above Toxic Blast / Corrosive
                    //Assault so those never need a debuff-name gate that could stall the priority.
                    Spell.Cast("Corrosive Dart",
                        ret => !HoldForOpener &&
                               (!Me.Target.HasMyDebuff("Corrosive Dart") || Me.Target.DebuffTimeLeft("Corrosive Dart") <= 3)),
                    Spell.Cast("Corrosive Grenade",
                        ret => !HoldForOpener &&
                               (!Me.Target.HasMyDebuff("Corrosive Grenade") || Me.Target.DebuffTimeLeft("Corrosive Grenade") <= 3)),

                    //Detonator - also generates Tactical Advantage
                    Spell.Cast("Toxic Blast", ret => !HoldForOpener),

                    //Spend Tactical Advantage
                    Spell.Cast("Corrosive Assault", ret => !HoldForOpener && Me.HasBuff("Tactical Advantage")),

                    //Build Tactical Advantage
                    Spell.Cast("Shiv", ret => !HoldForOpener && Me.BuffCount("Tactical Advantage") < 2),

                    //Cheap filler
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
                        Spell.DoT("Corrosive Grenade", "Corrosive Grenade"),
                        Spell.Cast("Toxic Haze", ret => Me.HasBuff("Tactical Advantage")),
                        Spell.Cast("Noxious Knives"),
                        Spell.Cast("Fragmentation Grenade", ret => Me.EnergyPercent >= 60)
                        ));
            }
        }
    }
}
