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
    ///     Marauder Carnage (burst melee dps) rotation: opens the Ferocity armour-pen window
    ///     and spends it on Devastating Blast / Gore / Vicious Throw.
    /// </summary>
    public class Carnage : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Carnage;

        public override string Name => "Marauder Carnage";

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
                    Spell.Cast("Berserk", ret => !Me.HasBuff("Berserk"))
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

                    //Ferocity opens the armour-pen window (next 2-3 attacks ignore armour)
                    Spell.Cast("Ferocity", ret => Me.Target.Distance <= Distance.Melee),

                    //Spend the window on the hardest hitters, in guide order
                    Spell.Cast("Devastating Blast", ret => Me.HasBuff("Execute") || Me.Level < 30),
                    Spell.Cast("Vicious Throw",
                        ret => Me.HasBuff("Slaughter") || Me.Target.HealthPercent <= 30),
                    Spell.Cast("Gore"),
                    Spell.Cast("Devastating Blast"),

                    //Dual Saber Throw on cooldown -- damage plus rage
                    Spell.Cast("Dual Saber Throw", ret => Me.Target.Distance <= 1f),

                    //Ravage is free, so it is the better filler while Berserk is up or when rage-starved
                    Spell.Cast("Ravage", ret => Me.HasBuff("Berserk") || Me.ActionPoints < 3),

                    //Primary filler
                    Spell.Cast("Massacre", ret => Me.ActionPoints >= 3),
                    Spell.Cast("Battering Assault", ret => Me.ActionPoints <= 8),
                    Spell.Cast("Ravage"),

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
                        //These out-damage Sweeping Slash at any target count, so they stay on top
                        Spell.Cast("Dual Saber Throw", ret => Me.Target.Distance <= 1f),
                        Spell.Cast("Ferocity", ret => Me.Target.Distance <= Distance.MeleeAoE),
                        Spell.Cast("Battering Assault", ret => Me.ActionPoints <= 8),

                        //Sweeping Slash beats the single-target fillers from 2 targets up
                        Spell.Cast("Sweeping Slash", ret => Me.ActionPoints >= 5),

                        Spell.Cast("Devastating Blast", ret => Me.HasBuff("Execute") || Me.Level < 30),
                        Spell.Cast("Vicious Throw",
                            ret => Me.HasBuff("Slaughter") || Me.Target.HealthPercent <= 30),
                        Spell.Cast("Ravage")
                        ));
            }
        }
    }
}
