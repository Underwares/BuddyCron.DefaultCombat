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

namespace DefaultCombat.Routines
{
    /// <summary>
    ///     Marauder Annihilation (DoT melee dps) rotation: keeps the Force Rend and Rupture
    ///     bleeds rolling and autocrits them with Berserk.
    /// </summary>
    public class Annihilation : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Annihilation;

        public override string Name => "Marauder Annihilation";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Unnatural Might")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Unleash", ret => Me.IsStunned),

                    //Defensives -- keep these first, they are what keeps a leveling character alive
                    Spell.Buff("Cloak of Pain", ret => Me.HealthPercent <= 75),
                    Spell.Buff("Saber Ward", ret => Me.HealthPercent <= 50),
                    Spell.Cast("Force Camouflage", ret => Me.HealthPercent <= 35),
                    Spell.Buff("Undying Rage", ret => Me.HealthPercent <= 20),
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15),

                    //Offensive cooldowns
                    Spell.Buff("Furious Power", ret => Me.Target.StrongOrGreater()),
                    Spell.Buff("Bloodthirst", ret => CombatHotkeys.EnableRaidBuffs),

                    //Deadly Saber is off-GCD -- keep the poison charge rolling at all times
                    Spell.Cast("Deadly Saber", ret => !Me.HasBuff("Deadly Saber")),

                    //Frenzy tops Fury back up so Berserk comes around again sooner
                    Spell.Cast("Frenzy", ret => !Me.HasBuff("Berserk") && Me.BuffCount("Fury") < 10),

                    //Berserk makes bleeds autocrit -- only worth it once bleeds are actually rolling
                    Spell.Cast("Berserk",
                        ret => !Me.HasBuff("Berserk") && Me.Target != null &&
                               (Me.Target.HasDebuff("Bleeding (Deadly Saber)") ||
                                Me.Target.HasDebuff("Bleeding (Rupture)") ||
                                Me.Level < 30))
                    );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    Spell.Cast("Force Charge", ret => CombatHotkeys.EnableCharge && Me.Target.Distance >= 1f),

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Rotation
                    Spell.Cast("Disruption", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Bleed upkeep -- Force Rend and Rupture want ~100% uptime, everything else feeds them
                    Spell.Cast("Force Rend",
                        ret => !Me.Target.HasMyDebuff("Force Rend") || Me.Target.DebuffTimeLeft("Force Rend") <= 2),
                    Spell.Cast("Rupture",
                        ret => !Me.Target.HasMyDebuff("Bleeding (Rupture)") ||
                               Me.Target.DebuffTimeLeft("Bleeding (Rupture)") <= 2),

                    //Pulverize makes Dual Saber Throw free + resets its cooldown
                    Spell.Cast("Dual Saber Throw", ret => Me.HasBuff("Pulverize") && Me.Target.Distance <= 1f),

                    //Core damage
                    Spell.Cast("Annihilate"),
                    Spell.Cast("Vicious Throw", ret => Me.Target.HealthPercent <= 30),
                    Spell.Cast("Ravage", ret => Me.HasBuff("Berserk")),

                    //Fillers
                    Spell.Cast("Vicious Slash", ret => Me.ActionPoints >= 6),
                    Spell.Cast("Ravage"),
                    Spell.Cast("Battering Assault", ret => Me.ActionPoints <= 8),

                    //Low level: no Pulverize passive yet, so Dual Saber Throw is just a rage builder
                    Spell.Cast("Dual Saber Throw", ret => Me.Level < 40 && Me.Target.Distance <= 1f),
                    Spell.Cast("Force Scream",
                        ret => Me.Target.Distance > Distance.Melee && Me.Target.Distance <= 1f),
                    Spell.Cast("Force Charge", ret => CombatHotkeys.EnableCharge && Me.ActionPoints <= 8),

                    //Never stall -- free basic attack
                    Spell.Cast("Assault")
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new Decorator(ret => Targeting.ShouldPbaoe,
                    new PrioritySelector(
                        //Bleeds first -- Smash/Rupture spread them (Blood Wave) and Smash bounces Force Rend
                        Spell.Cast("Force Rend", ret => !Me.Target.HasMyDebuff("Force Rend")),
                        Spell.Cast("Rupture", ret => !Me.Target.HasMyDebuff("Bleeding (Rupture)")),
                        Spell.Cast("Dual Saber Throw", ret => Me.Target.Distance <= 1f),
                        Spell.Cast("Smash"),
                        Spell.Cast("Annihilate"),
                        Spell.Cast("Sweeping Slash", ret => Me.ActionPoints >= 5),
                        Spell.Cast("Ravage"),
                        Spell.Cast("Battering Assault", ret => Me.ActionPoints <= 8)
                        ));
            }
        }
    }
}
