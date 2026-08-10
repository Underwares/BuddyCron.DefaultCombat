// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using BuddyCron;
using BuddyCron.Objects;
using DefaultCombat.Core;
using Reborn.Behaviors.Treesharp;

namespace DefaultCombat.Routines
{
    /// <summary>
    ///     Base class for a combat-style discipline rotation, exposed as buff, cooldown,
    ///     single-target, and area-of-effect behavior-tree composites.
    /// </summary>
    public abstract class RotationBase
    {
        /// <summary>
        ///     Gets the local player.
        /// </summary>
        protected static HeroPlayer Me => BuddyCron.Core.Player;

        /// <summary>
        ///     Gets the character functioning as the tank.
        /// </summary>
        public static HeroCharacter Tank => Targeting.Tank;

        /// <summary>
        ///     Gets the current heal target.
        /// </summary>
        public static HeroCharacter HealTarget => Targeting.HealTarget;

        /// <summary>
        ///     Gets the rotation's name.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        ///     Gets the combat-style discipline this rotation implements.
        /// </summary>
        public virtual CharacterDiscipline Discipline => CharacterDiscipline.None;

        /// <summary>
        ///     Shared priority for the Legacy Heroic Moment abilities. Gated on the Heroic Moment
        ///     buff, so it only acts after the user activates Heroic Moment.
        /// </summary>
        public static Composite HeroicComposite = new Decorator(r => Me.HasBuff("Heroic Moment"), new PrioritySelector(

                                        //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                                        Spell.Cast("Legacy Force Sweep", ret => Me.Target.Distance < .6f),
                                        Spell.CastOnGround("Legacy Orbital Strike"),
                                        Spell.Cast("Legacy Project"),
                                        Spell.Cast("Legacy Dirty Kick", ret => Me.Target.Distance < .5f),
                                        Spell.Cast("Legacy Sticky Plasma Grenade"),
                                        Spell.Cast("Legacy Flame Thrower"),
                                        Spell.Cast("Legacy Force Lightning"),
                                        Spell.Cast("Legacy Force Choke")

        ));



        /// <summary>
        ///     Gets the buff logic for this routine.
        /// </summary>
        public abstract Composite Buffs { get; }

        /// <summary>
        ///     Gets the cooldown usage logic for this routine.
        /// </summary>
        public abstract Composite Cooldowns { get; }

        /// <summary>
        ///     Gets the single target logic for this routine.
        /// </summary>
        public abstract Composite SingleTarget { get; }

        /// <summary>
        ///     Gets the area of effect logic for this routine.
        /// </summary>
        public abstract Composite AreaOfEffect { get; }
    }
}
