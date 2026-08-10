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
    ///     Marauder Fury (burst melee dps) rotation: Obliterate / Force Crush set up the
    ///     Dominate and Shockwave procs, cashed in with Raging Burst.
    /// </summary>
    public class Fury : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Fury;

        public override string Name => "Marauder Fury";

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

                    //Frenzy tops Fury back up so Berserk comes around again sooner
                    Spell.Cast("Frenzy", ret => !Me.HasBuff("Berserk") && Me.BuffCount("Fury") < 10),

                    //Berserk grants Shockwave -- feeds the next Raging Burst / Smash, so do not
                    //overwrite a Shockwave that has not been cashed in yet
                    Spell.Cast("Berserk", ret => !Me.HasBuff("Berserk") && !Me.HasBuff("Shockwave"))
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

                    //Set the burst window up: Obliterate grants Dominate (autocrit) + Battle Cry,
                    //Force Crush / Berserk grant Shockwave (free + 15% damage)
                    Spell.Cast("Obliterate",
                        ret => CombatHotkeys.EnableCharge && !Me.HasBuff("Dominate")),
                    Spell.Cast("Force Crush", ret => !Me.HasBuff("Shockwave")),

                    //Cash the procs in
                    Spell.Cast("Raging Burst", ret => Me.HasBuff("Shockwave") || Me.HasBuff("Dominate")),
                    Spell.Cast("Furious Strike", ret => Me.ActionPoints >= 5),
                    Spell.Cast("Vicious Throw", ret => Me.Target.HealthPercent <= 30),
                    Spell.Cast("Force Scream",
                        ret => (Me.HasBuff("Battle Cry") || !AbilityManager.HasAbility("Obliterate")) &&
                               Me.Target.Distance <= 1f),

                    //Raging Burst on cooldown even without a proc
                    Spell.Cast("Raging Burst"),

                    //Fillers -- Ravage is free and still builds Fury
                    Spell.Cast("Ravage"),
                    Spell.Cast("Battering Assault", ret => Me.ActionPoints <= 8),
                    Spell.Cast("Vicious Slash", ret => Me.ActionPoints >= 6),
                    Spell.Cast("Dual Saber Throw", ret => Me.Target.Distance <= 1f),

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
                        //Smash is the AoE stand-in for Raging Burst and eats the same procs
                        Spell.Cast("Force Crush", ret => !Me.HasBuff("Shockwave")),
                        Spell.Cast("Smash",
                            ret => Me.HasBuff("Shockwave") || Me.HasBuff("Dominate") ||
                                   !AbilityManager.HasAbility("Raging Burst")),
                        Spell.Cast("Obliterate", ret => CombatHotkeys.EnableCharge),
                        Spell.Cast("Smash"),
                        Spell.Cast("Dual Saber Throw", ret => Me.Target.Distance <= 1f),
                        Spell.Cast("Sweeping Slash", ret => Me.ActionPoints >= 5),
                        Spell.Cast("Ravage"),
                        Spell.Cast("Battering Assault", ret => Me.ActionPoints <= 8)
                        ));
            }
        }
    }
}
