// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using System;
using System.Linq;
using BuddyCron;
using BuddyCron.Inheritables;
using BuddyCron.Managers;
using DefaultCombat.Core;
using DefaultCombat.Helpers;
using Reborn.Behaviors.Treesharp;
using Targeting = DefaultCombat.Core.Targeting;

namespace DefaultCombat
{
    /// <summary>All-class combat routine: picks the rotation for the player's discipline and wires
    /// the pull/combat/rest behavior trees.</summary>
    public class DefaultCombat : CombatRoutine
    {
        /// <summary>True when the active rotation is a healing discipline.</summary>
        public static bool IsHealer { get; private set; }

        private static readonly InventoryManagerItem s_medPack = new InventoryManagerItem("Medpac", 90);
        private static readonly CharacterDiscipline[] s_allDisciplines = Enum.GetValues<CharacterDiscipline>()
            .Where(discipline => discipline != CharacterDiscipline.None &&
                                 discipline != CharacterDiscipline.CanNotBeDetermined)
            .ToArray();
        private Composite _combat;
        private Composite _ooc;
        private Composite _pull;

        /// <summary>True when the current botbase is not autonomous; the routine then skips
        /// self-movement and resting.</summary>
        public static bool MovementDisabled => !BotManager.Current.IsAutonomous;

        /// <summary>True when the current botbase runs autonomously (grinding).</summary>
        public static bool Grind => BotManager.Current.IsAutonomous;

        /// <summary>Routine name shown in logs and the UI.</summary>
        public override string Name => "DefaultCombat";

        /// <summary>All current combat-style disciplines; obsolete origin/basic classes are excluded.</summary>
        public override CharacterDiscipline[] Class => s_allDisciplines;

        /// <summary>Ranged pull distance (not yet user-configurable); melee rotations close the
        /// rest of the way themselves via <see cref="CombatMovement.CloseDistance"/>.</summary>
        public override float PullRange => 3f;

        /// <summary>Behavior used while pulling; runs the combat tree when allowed to act.</summary>
        public override Composite PullBehavior => _pull;

        /// <summary>Builds the rotation for the player's discipline/class, registers the combat
        /// hotkeys, and assembles the pull/combat/rest behavior trees.</summary>
        public override void Initialize()
        {
            var me = BuddyCron.Core.Player;

            Logger.Write("*** Default Combat v90***");
            Logger.Write("Level: " + me.Level);
            Logger.Write("Class: " + me.CharacterClass);
            Logger.Write("Discipline: " + me.CharacterDiscipline);

            var f = new RotationFactory();
            var b = f.Build(me.CharacterDiscipline);

            CombatHotkeys.Initialize();

            Logger.Write("Rotation Selected : " + b.Name);

            IsHealer = me.IsHealer();

            if (IsHealer)
                Logger.Write("Healing Enabled");

            _ooc = new Decorator(ret => !BuddyCron.Core.Player.IsDead && !BuddyCron.Core.Player.IsMounted && !CombatHotkeys.PauseRotation,
                new PrioritySelector(
                    Targeting.ScanTargets,
                    new Decorator(ret => IsHealer, b.AreaOfEffect),
                    Spell.Buff(BuddyCron.Core.Player.SelfBuffName()),
                    b.Buffs,
                    Rest.HandleRest,
                    Scavenge.ScavengeCorpse
                    ));

            _combat = new Decorator(ret => !CombatHotkeys.PauseRotation,
                new PrioritySelector(
                    Spell.WaitForCast(),
                    s_medPack.UseItem(ret => BuddyCron.Core.Player.HealthPercent <= 30),
                    Targeting.ScanTargets,
                    b.Cooldowns,
                    new Decorator(ret => IsHealer || CombatHotkeys.EnableAoe, b.AreaOfEffect),
                    b.SingleTarget));

            _pull = new Decorator(
                ret => !CombatHotkeys.PauseRotation && (!MovementDisabled || IsHealer && !Grind),
                _combat);

            // RoutineManager.ResetHooks wires these into the TreeHooks locations the brain runs.
            CombatBehavior = _combat;
            RestBehavior = _ooc;
        }
    }
}
