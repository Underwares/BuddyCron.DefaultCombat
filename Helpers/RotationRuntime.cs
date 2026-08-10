// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using BuddyCron.Managers;
using BuddyCron.Objects;
using DefaultCombat.Core;
using Reborn.Behaviors.Treesharp;

namespace DefaultCombat.Helpers
{
    /// <summary>Provides shared player, targeting, bot-mode, item, and behavior state to the
    /// discipline routines.</summary>
    internal static class RotationRuntime
    {
        /// <summary>The shared medpac wrapper and its reuse timer.</summary>
        internal static readonly InventoryManagerItem MedPack = new InventoryManagerItem("Medpac", 90);

        /// <summary>Legacy Heroic Moment priority shared by every discipline.</summary>
        internal static readonly Composite HeroicMoment = new Decorator(
            ret => Player.HasBuff("Heroic Moment"),
            new PrioritySelector(
                Spell.Cast("Legacy Force Sweep", ret => Player.Target.Distance < .6f),
                Spell.CastOnGround("Legacy Orbital Strike"),
                Spell.Cast("Legacy Project"),
                Spell.Cast("Legacy Dirty Kick", ret => Player.Target.Distance < .5f),
                Spell.Cast("Legacy Sticky Plasma Grenade"),
                Spell.Cast("Legacy Flame Thrower"),
                Spell.Cast("Legacy Force Lightning"),
                Spell.Cast("Legacy Force Choke")));

        /// <summary>Gets the local player.</summary>
        internal static HeroPlayer Player => BuddyCron.Core.Player;

        /// <summary>Gets the character functioning as the tank.</summary>
        internal static HeroCharacter Tank => Targeting.Tank;

        /// <summary>Gets the current heal target.</summary>
        internal static HeroCharacter HealTarget => Targeting.HealTarget;

        /// <summary>True when the active discipline is a healing discipline.</summary>
        internal static bool IsHealer => Player.IsHealer();

        /// <summary>True when the current botbase is not autonomous.</summary>
        internal static bool MovementDisabled => !BotManager.Current.IsAutonomous;

        /// <summary>True when the current botbase runs autonomously.</summary>
        internal static bool Grind => BotManager.Current.IsAutonomous;
    }
}
