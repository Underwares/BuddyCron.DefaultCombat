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
using Action = Reborn.Behaviors.Treesharp.Action;

namespace DefaultCombat.Core
{
    /// <summary>Movement composites shared by rotations.</summary>
    public class CombatMovement
    {
        //public static Composite CloseDistance(float range)
        //{
        //    return new Decorator(ret => !DefaultCombat.MovementDisabled && BuddyCron.Core.Player.Target != null,
        //        new PrioritySelector(
        //            new Decorator(ret => BuddyCron.Core.Player.Target.Distance < range,
        //                new Action(delegate
        //                {
        //                    Navigator.Stop();
        //                    return RunStatus.Failure;
        //                })),
        //            new Decorator(ret => BuddyCron.Core.Player.Target.Distance >= range,
        //                CommonBehaviors.MoveAndStop(location => BuddyCron.Core.Player.Target.Location, range, true)),
        //            new Action(delegate { return RunStatus.Failure; })));
        //}



        /// <summary>Composite that moves toward the current target and stops once within
        /// <paramref name="range"/>.</summary>
        public static Composite CloseDistance(float range)
        {
            return CommonBehaviors.MoveAndStop(location => BuddyCron.Core.Player.Target.Location, r=>range, true, r=> $"Closing to {range} on {BuddyCron.Core.Player.Target.Name}" );
        }
    }
}
