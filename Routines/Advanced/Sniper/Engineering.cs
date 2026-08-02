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
    ///     7.x Sniper Engineering (ranged DoT DPS) rotation, fought from cover and built around
    ///     Explosive Probe, Interrogation Probe and Plasma Probe. The most energy-hungry sniper
    ///     spec, so it bails to Rifle Shot early.
    /// </summary>
    public class Engineering : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Engineering;

        public override string Name => "Sniper Engineering";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Coordination")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    //Survival
                    Spell.Buff("Escape", ret => Me.IsStunned),
                    Spell.Buff("Shield Probe", ret => Me.HealthPercent <= 70),
                    Spell.Buff("Evasion", ret => Me.HealthPercent <= 40),
                    Spell.Buff("Entrench", ret => Me.IsInCover() && Me.HealthPercent <= 60),
                    //Ability tree choice (lvl 68) -- silently skipped when not chosen
                    Spell.Buff("Ballistic Shield", ret => Me.HealthPercent <= 50),
                    //Resets defensive cooldowns when things go bad
                    Spell.Buff("Meticulous Preparation", ret => Me.HealthPercent <= 25),

                    //Energy
                    Spell.Cast("Adrenaline Probe", ret => Me.EnergyPercent <= 45),

                    //Offensive -- Laze Target is the autocrit cooldown; every Sniper discipline keeps it
                    Spell.Cast("Laze Target"),
                    //Ability tree choice (lvl 43) -- silently skipped when not chosen
                    Spell.Cast("Target Acquired", ret => Me.Target.StrongOrGreater()),

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

                    //Cover -- Snipe/Series of Shots require it and it is our energy regen
                    Spell.Buff("Crouch", ret => !Me.IsInCover() && !Me.IsMoving),

                    //Interrupt
                    Spell.Cast("Distraction", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Low Energy -- Engineering is the hungriest sniper spec, bail out early
                    new Decorator(ret => Me.EnergyPercent < 45,
                        new PrioritySelector(
                            Spell.Cast("Rifle Shot")
                            )),

                    //Rotation
                    Spell.Cast("Explosive Probe"),
                    Spell.Cast("Series of Shots"),
                    Spell.DoT("Interrogation Probe", "Interrogation Probe"),
                    Spell.Cast("EMP Discharge",
                        ret => Me.Target.HasMyDebuff("Interrogation Probe") || Me.Level < 40),
                    Spell.DoTGround("Plasma Probe", 9000),
                    Spell.Cast("Takedown", ret => Me.Target.HealthPercent <= 30),
                    Spell.Cast("Fragmentation Grenade", ret => Me.EnergyPercent > 60),
                    Spell.DoT("Corrosive Dart", "Corrosive Dart", 0, ret => Me.EnergyPercent > 60),
                    Spell.CastOnGround("Orbital Strike", ret => Me.EnergyPercent > 75),
                    Spell.Cast("Snipe"),

                    //Never stall
                    Spell.Cast("Rifle Shot")
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
                        Spell.DoTGround("Plasma Probe", 9000),
                        Spell.CastOnGround("Orbital Strike", ret => Me.EnergyPercent > 50),
                        Spell.Cast("Fragmentation Grenade", ret => Me.EnergyPercent > 50),
                        Spell.CastOnGround("Suppressive Fire", ret => Me.IsInCover() && Me.EnergyPercent > 30)
                        ));
            }
        }
    }
}
