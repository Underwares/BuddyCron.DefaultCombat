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
    ///     7.x Sentinel Combat (melee burst DPS) rotation: opens armour-pen windows with
    ///     Precision and spends them on Clashing Blast / Dispatch / Blade Storm.
    /// </summary>
    public class Combat : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Combat;

        public override string Name => "Sentinel Combat";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Force Might")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Resolute", ret => Me.IsStunned),

                    //Defensives -- keep these first, they are what keeps a leveling character alive
                    Spell.Buff("Rebuke", ret => Me.HealthPercent <= 75),
                    Spell.Buff("Saber Ward", ret => Me.HealthPercent <= 50),
                    Spell.Cast("Force Camouflage", ret => Me.HealthPercent <= 35),
                    Spell.Buff("Guarded by the Force", ret => Me.HealthPercent <= 20),
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15),

                    //Offensive cooldowns
                    Spell.Buff("Force Clarity", ret => Me.Target != null && Me.Target.StrongOrGreater()),
                    Spell.Buff("Inspiration", ret => CombatHotkeys.EnableRaidBuffs),

                    //Valorous Call tops Centering back up so Zen comes around again sooner
                    Spell.Cast("Valorous Call", ret => !Me.HasBuff("Zen") && Me.BuffCount("Centering") < 10),

                    //Zen gives alacrity plus a third Precision charge -- just use it on cooldown
                    Spell.Cast("Zen", ret => !Me.HasBuff("Zen"))
                    );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    Spell.Cast("Force Leap", ret => CombatHotkeys.EnableCharge && Me.Target.Distance >= 1f),

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Rotation
                    Spell.Cast("Force Kick", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Enter the burst window with enough focus and a live Blade Rush Ataru buff.
                    Spell.Cast("Zealous Strike", ret => Me.ActionPoints <= 5),
                    Spell.Cast("Blade Rush",
                        ret => Me.ActionPoints >= 3 &&
                               (!Me.HasBuff("Blade Rush") || Me.BuffTimeLeft("Blade Rush") <= 2)),
                    Spell.Cast("Precision",
                        ret => Me.Target.Distance <= Distance.Melee &&
                               (Me.HasBuff("Blade Rush") || Me.Level < 30)),

                    //Spend the window on the hardest hitters. Opportune Attack autocrits Clashing Blast,
                    //Hand of Justice makes Dispatch free and usable at any health.
                    Spell.Cast("Clashing Blast", ret => Me.HasBuff("Opportune Attack") || Me.Level < 30),
                    Spell.Cast("Dispatch",
                        ret => Me.HasBuff("Hand of Justice") || Me.Target.HealthPercent <= 30),
                    Spell.Cast("Lance"),
                    Spell.Cast("Clashing Blast"),
                    Spell.Cast("Blade Storm", ret => Me.HasBuff("Precision") && Me.ActionPoints >= 6),

                    //Twin Saber Throw on cooldown -- free damage
                    Spell.Cast("Twin Saber Throw", ret => Me.Target.Distance <= 1f),

                    //Blade Barrage is free, so it is the better filler while Zen is up or focus-starved
                    Spell.Cast("Blade Barrage", ret => Me.HasBuff("Zen") || Me.ActionPoints < 3),

                    //Primary filler
                    Spell.Cast("Blade Rush", ret => Me.ActionPoints >= 3),
                    Spell.Cast("Zealous Strike", ret => Me.ActionPoints <= 8),
                    Spell.Cast("Blade Barrage"),

                    //Never stall -- free basic attack that builds focus
                    Spell.Cast("Strike")
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new Decorator(ret => Targeting.ShouldPbaoe,
                    new PrioritySelector(
                        //These out-damage Cyclone Slash at any target count, so they stay on top
                        Spell.Cast("Twin Saber Throw", ret => Me.Target.Distance <= 1f),
                        Spell.Cast("Precision", ret => Me.Target.Distance <= Distance.MeleeAoE),
                        Spell.Cast("Force Sweep"),
                        Spell.Cast("Zealous Strike", ret => Me.ActionPoints <= 8),

                        //Cyclone Slash beats the single-target fillers from 2 targets up
                        Spell.Cast("Cyclone Slash", ret => Me.ActionPoints >= 5),

                        Spell.Cast("Clashing Blast", ret => Me.HasBuff("Opportune Attack") || Me.Level < 30),
                        Spell.Cast("Dispatch",
                            ret => Me.HasBuff("Hand of Justice") || Me.Target.HealthPercent <= 30),
                        Spell.Cast("Blade Barrage")
                        ));
            }
        }
    }
}
