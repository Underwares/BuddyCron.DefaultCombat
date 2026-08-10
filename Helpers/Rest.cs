// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using System.Threading.Tasks;
using BuddyCron;
using BuddyCron.Behaviors;
using BuddyCron.Managers;
using BuddyCron.Navigation;
using BuddyCron.Objects;
using Reborn.Behaviors.Coroutines;
using Reborn.Behaviors.Treesharp;

namespace DefaultCombat.Helpers
{
    /// <summary>Out-of-combat recovery: revives dead companions and channels the class rest ability
    /// until health/resource are restored.</summary>
    public static class Rest
    {
        private static HeroPlayer Me => BuddyCron.Core.Player;

        /// <summary>Composite that first revives a dead companion, then rests until health and
        /// resource are back to full.</summary>
        public static Composite HandleRest
        {
            get
            {
                return new PrioritySelector(
                    new ActionRunCoroutine(ctx => ReviveCompanion()),
                    new ActionRunCoroutine(ctx => Rejuvenate())
                    );
            }
        }

        /// <summary>Revives a dead companion: waits out any in-flight cast, closes to interact
        /// range, then channels Revive Companion. Runs one step per tick (Running while moving /
        /// casting); false when there is nothing to revive, so the selector falls through.</summary>
        private static async Task<bool> ReviveCompanion()
        {
            var companion = Me.Companion;
            if (companion == null || !companion.IsDead)
                return false;

            // don't clip whatever is already casting (the old Spell.WaitForCast guard)
            if (Me.IsCasting)
                return true;

            if (companion.Distance > 0.3f)
            {
                await CommonTasks.MoveAndStop(new MoveToParameters(companion.Location, "Companion"), 0.2f, true, "Companion");
                return true;
            }

            return AbilityManager.Cast("Revive Companion", companion).Success;
        }

        /// <summary>Channels the class rest ability until health/resource are back to full.
        /// Earlier implementations parked the bot thread in a while+Thread.Sleep loop and cancelled
        /// the finished channel by sending ESC to the game window; this yields to the tree between
        /// polls (the composite reports Running) and cancels through <see cref="AbilityManager.StopCasting"/>,
        /// so it needs neither the window handle nor focus.</summary>
        private static async Task<bool> Rejuvenate()
        {
            if (!NeedRest())
                return false;

            Logger.Write("Starting to rest!");

            if (Me.IsMoving)
            {
                Navigator.PlayerMover.MoveStop();
                await Coroutine.Wait(300, () => !Me.IsMoving);
            }


            await Coroutine.Wait(1000, () => AbilityManager.CanCast(Me.RejuvenateAbilityName(),Me).Success);

            while (KeepResting())
            {
                if (!Me.IsCasting && !AbilityManager.Cast(Me.RejuvenateAbilityName(), Me).Success)
                {
                    // channel refused (combat, unknown ability name, ...) — bail instead of spinning
                    return false;
                }

                await Coroutine.Sleep(100);
            }


            Logger.Write("Finished Resting");
            // fully rested (or interrupted via KeepResting going false) — stop the channel
            if (Me.IsCasting)
                AbilityManager.StopCasting();

            return true;
        }

        /// <summary>Player resource scaled so low values mean "needs rest"; rage/focus classes
        /// always report 100 (they never rest for resource).</summary>
        public static int NormalizedResource()
        {
            // ResourcePercent() returns remaining resource, 100 = full/rested — heat classes
            // count down on the current build, so no class needs inverting here.
            switch (Me.AdvancedClass)
            {
                case AdvancedClass.Juggernaut:
                case AdvancedClass.Marauder:
                case AdvancedClass.Guardian:
                case AdvancedClass.Sentinel:
                    return 100;
                default:
                    return (int)Me.ResourcePercent();
            }
        }

        /// <summary>True when out of combat and the player's health/resource (or the companion's
        /// health) are low enough to start resting.</summary>
        public static bool NeedRest()
        {
            var resource = NormalizedResource();
            return !RotationRuntime.MovementDisabled && !Me.InCombat && (resource < 50 || Me.HealthPercent < 90 || Me.Companion is { IsDead: false, HealthPercent: < 90 });
        }

        /// <summary>True while resting should continue: still out of combat and anything
        /// (health, resource, companion health) below 100%.</summary>
        public static bool KeepResting()
        {
            var resource = NormalizedResource();
            return !RotationRuntime.MovementDisabled && !Me.InCombat && (resource < 100 || Me.HealthPercent < 100 || Me.Companion is
            {
                IsDead: false, HealthPercent: < 100
            });
        }
    }
}
