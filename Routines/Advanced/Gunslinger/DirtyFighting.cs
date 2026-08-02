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
    ///     Gunslinger Dirty Fighting (DoT ranged dps) rotation: keeps Vital Shot and Shrap Bomb
    ///     at full uptime and channels Wounding Shots while both are ticking.
    /// </summary>
    public class DirtyFighting : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.DirtyFighting;

        public override string Name => "Gunslinger Dirty Fighting";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Lucky Shots")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    //Survival
                    Spell.Buff("Escape", ret => Me.IsStunned),
                    Spell.Buff("Defense Screen", ret => Me.HealthPercent <= 70),
                    Spell.Buff("Dodge", ret => Me.HealthPercent <= 40),
                    Spell.Buff("Hunker Down", ret => Me.IsInCover() && Me.HealthPercent <= 60),
                    //Ability tree choice (lvl 68) -- silently skipped when not chosen
                    Spell.Buff("Scrambling Field", ret => Me.HealthPercent <= 50),
                    //Resets Defense Screen/Dodge/Hunker Down/Hightail It when things go bad
                    Spell.Buff("Bag of Tricks", ret => Me.HealthPercent <= 25),

                    //Energy -- Cool Head at 45-50% keeps us in the top regen band
                    Spell.Cast("Cool Head", ret => Me.EnergyPercent <= 50),

                    //Offensive
                    Spell.Cast("Smuggler's Luck", ret => Me.Target.StrongOrGreater()),
                    //Ability tree choice (lvl 47) -- silently skipped when not chosen
                    Spell.Cast("Illegal Mods", ret => Me.Target.StrongOrGreater()),

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

                    //Cover -- Dirty Blast/Wounding Shots/Speed Shot want it and it is our energy regen
                    Spell.Buff("Crouch", ret => !Me.IsInCover() && !Me.IsMoving),

                    //Interrupt
                    Spell.Cast("Distraction", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Rotation -- the dots are the whole spec, keep them at 100% uptime
                    Spell.DoT("Vital Shot", "Vital Shot"),
                    Spell.DoT("Shrap Bomb", "Shrap Bomb"),
                    //Hemorrhaging Blast makes every dot tick hit harder -- pair it with Wounding Shots
                    Spell.Cast("Hemorrhaging Blast",
                        ret => Me.Target.HasMyDebuff("Vital Shot") || Me.Target.HasMyDebuff("Shrap Bomb")),
                    //Wounding Shots only pays off while the dots are actually ticking. Shrap Bomb is a
                    //later pickup, so do not let its absence lock the channel out on a low level character.
                    Spell.Cast("Wounding Shots",
                        ret => Me.Target.DebuffTimeLeft("Vital Shot") > 3
                               && (Me.Target.DebuffTimeLeft("Shrap Bomb") > 3 || Me.Level < 40)),
                    Spell.Cast("Quickdraw", ret => Me.Target.HealthPercent <= 30 || Me.HasBuff("Dirty Shot")),

                    //Low Energy -- Speed Shot is our cheap channel, use it before falling to Flurry of Bolts
                    Spell.Cast("Speed Shot", ret => Me.EnergyPercent < 65),
                    Spell.Cast("Flurry of Bolts", ret => Me.EnergyPercent < 50),

                    //Filler
                    Spell.Cast("Dirty Blast"),

                    //Never stall
                    Spell.Cast("Flurry of Bolts")
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new Decorator(ret => Targeting.ShouldAoe,
                    new PrioritySelector(
                        Spell.Buff("Crouch", ret => !Me.IsInCover() && !Me.IsMoving),
                        //Shrap Bomb is our AoE dot and spreads via Extra Shrapnel -- land the dots first
                        Spell.DoT("Vital Shot", "Vital Shot"),
                        Spell.DoT("Shrap Bomb", "Shrap Bomb"),
                        //Bombing Run (7.0 rename of XS Freighter Flyby) is the lvl 68 tree choice -- skipped when not chosen
                        Spell.CastOnGround("Bombing Run", ret => Me.IsInCover() && Me.EnergyPercent > 60),
                        Spell.Cast("Thermal Grenade", ret => Me.EnergyPercent > 50),
                        Spell.CastOnGround("Sweeping Gunfire", ret => Me.IsInCover() && Me.EnergyPercent > 30)
                        ));
            }
        }
    }
}
