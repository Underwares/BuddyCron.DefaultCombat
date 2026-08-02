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
    ///     7.x Sniper Marksmanship (ranged burst DPS) rotation, fought from cover: Followthrough
    ///     and Penetrating Blasts on cooldown, Ambush on Zeroing Shots stacks.
    /// </summary>
    public class Marksmanship : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Marksmanship;

        public override string Name => "Sniper Marksmanship";

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
                    Spell.Cast("Sniper Volley", ret => Me.EnergyPercent <= 65),

                    //Offensive
                    Spell.Buff("Entrench", ret => Me.Target.StrongOrGreater() && Me.IsInCover()),
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

                    //Cover -- Snipe/Ambush/Followthrough/Penetrating Blasts all require it, and it is our energy regen
                    Spell.Buff("Crouch", ret => !Me.IsInCover() && !Me.IsMoving),

                    //Interrupt
                    Spell.Cast("Distraction", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Low Energy -- keep at/above the 45-50% high regen band. Followthrough is cheap and
                    //its proc window is short, so it is the one thing we still fire while regenerating.
                    new Decorator(ret => Me.EnergyPercent < 50,
                        new PrioritySelector(
                            Spell.Cast("Followthrough"),
                            Spell.Cast("Rifle Shot")
                            )),

                    //Rotation
                    Spell.Cast("Followthrough"),
                    Spell.Cast("Penetrating Blasts"),
                    //Zeroing Shots is a Marksmanship passive; low level chars simply hard cast Ambush
                    Spell.Cast("Ambush", ret => Me.BuffCount("Zeroing Shots") >= 2 || Me.Level < 30),
                    Spell.Cast("Takedown", ret => Me.Target.HealthPercent <= 30),
                    //Filler dot -- only worth the energy on things that live long enough
                    Spell.DoT("Corrosive Dart", "Corrosive Dart", 0,
                        ret => Me.Target.StrongOrGreater() && Me.EnergyPercent > 60),
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
                        //Ability tree choice (lvl 68) for Marksmanship -- skipped when not chosen
                        Spell.CastOnGround("Orbital Strike", ret => Me.IsInCover() && Me.EnergyPercent > 60),
                        Spell.Cast("Fragmentation Grenade", ret => Me.EnergyPercent > 50),
                        Spell.CastOnGround("Suppressive Fire", ret => Me.IsInCover() && Me.EnergyPercent > 30)
                        ));
            }
        }
    }
}
