// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using BuddyCron;
using BuddyCron.Inheritables;
using BuddyCron.Objects;
using DefaultCombat.Core;
using DefaultCombat.Helpers;
using Reborn.Behaviors.Treesharp;

namespace DefaultCombat.Routines
{
    /// <summary>
    /// Base combat routine for one discipline, exposing its buff, cooldown, single-target, and
    /// area-of-effect priorities through the standard BuddyCron behavior hooks.
    /// </summary>
    public abstract class RotationBase : CombatRoutine
    {
        private Composite _combat;
        private Composite _outOfCombat;
        private Composite _pull;

        /// <summary>Gets the local player.</summary>
        protected HeroPlayer Me => RotationRuntime.Player;

        /// <summary>Gets the character functioning as the tank.</summary>
        protected HeroCharacter Tank => RotationRuntime.Tank;

        /// <summary>Gets the current heal target.</summary>
        protected HeroCharacter HealTarget => RotationRuntime.HealTarget;

        /// <summary>True when the active discipline is a healing discipline.</summary>
        protected bool IsHealer => RotationRuntime.IsHealer;

        /// <summary>True when the current botbase is not autonomous.</summary>
        protected bool MovementDisabled => RotationRuntime.MovementDisabled;

        /// <summary>True when the current botbase runs autonomously.</summary>
        protected bool Grind => RotationRuntime.Grind;

        /// <summary>Gets the Legacy Heroic Moment priority shared by every discipline.</summary>
        protected Composite HeroicComposite => RotationRuntime.HeroicMoment;

        /// <inheritdoc />
        public abstract override string Name { get; }

        /// <summary>Gets the combat-style discipline implemented by this routine.</summary>
        public abstract CharacterDiscipline Discipline { get; }

        /// <inheritdoc />
        public sealed override CharacterDiscipline[] Class => new[] { Discipline };

        /// <inheritdoc />
        public sealed override float PullRange => 3f;

        /// <inheritdoc />
        public sealed override Composite PullBehavior => _pull;

        /// <summary>
        /// Builds this discipline's pull, combat, and out-of-combat behavior trees and registers
        /// the shared combat hotkeys.
        /// </summary>
        public sealed override void Initialize()
        {
            Logger.Write("*** Default Combat v90***");
            Logger.Write("Level: " + Me.Level);
            Logger.Write("Class: " + Me.CharacterClass);
            Logger.Write("Discipline: " + Me.CharacterDiscipline);

            CombatHotkeys.Initialize();

            Logger.Write("Rotation Selected : " + Name);
            if (IsHealer)
            {
                Logger.Write("Healing Enabled");
            }

            _outOfCombat = new Decorator(
                ret => !Me.IsDead && !Me.IsMounted && !CombatHotkeys.PauseRotation,
                new PrioritySelector(
                    Targeting.ScanTargets,
                    new Decorator(ret => IsHealer, AreaOfEffect),
                    Spell.Buff(Me.SelfBuffName()),
                    Buffs,
                    Rest.HandleRest,
                    Scavenge.ScavengeCorpse));

            _combat = new Decorator(
                ret => !CombatHotkeys.PauseRotation,
                new PrioritySelector(
                    Spell.WaitForCast(),
                    RotationRuntime.MedPack.UseItem(ret => Me.HealthPercent <= 30),
                    Targeting.ScanTargets,
                    Cooldowns,
                    new Decorator(ret => IsHealer || CombatHotkeys.EnableAoe, AreaOfEffect),
                    SingleTarget));

            _pull = new Decorator(
                ret => !CombatHotkeys.PauseRotation && (!MovementDisabled || IsHealer && !Grind),
                _combat);

            CombatBehavior = _combat;
            RestBehavior = _outOfCombat;
        }

        /// <summary>Gets the buff logic for this routine.</summary>
        public abstract Composite Buffs { get; }

        /// <summary>Gets the cooldown usage logic for this routine.</summary>
        public abstract Composite Cooldowns { get; }

        /// <summary>Gets the single-target logic for this routine.</summary>
        public abstract Composite SingleTarget { get; }

        /// <summary>Gets the area-of-effect logic for this routine.</summary>
        public abstract Composite AreaOfEffect { get; }
    }
}
