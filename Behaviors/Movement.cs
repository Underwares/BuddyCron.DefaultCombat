// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using BuddyCron;
using BuddyCron.Behaviors;
using BuddyCron.Helpers;
using BuddyCron.Objects;
using DefaultCombat.Helpers;
using Reborn.Behaviors.Treesharp;

namespace DefaultCombat.Behaviors
{
    /// <summary>Movement composites shared by rotations.</summary>
    public class CombatMovement
    {
        private const float FacingToleranceDegrees = 10f;

        //public static Composite CloseDistance(float range)
        //{
        //    return new Decorator(ret => !RotationRuntime.MovementDisabled && Core.Player.Target != null,
        //        new PrioritySelector(
        //            new Decorator(ret => Core.Player.Target.Distance < range,
        //                new Action(delegate
        //                {
        //                    Navigator.Stop();
        //                    return RunStatus.Failure;
        //                })),
        //            new Decorator(ret => Core.Player.Target.Distance >= range,
        //                CommonBehaviors.MoveAndStop(location => Core.Player.Target.Location, range, true)),
        //            new Action(delegate { return RunStatus.Failure; })));
        //}



        /// <summary>Creates a composite that closes to <paramref name="range"/> while the botbase
        /// is autonomous, blocking later rotation actions until the current target is in range.</summary>
        public static Composite CloseDistance(float range)
        {
            return new Decorator(
                ret => !RotationRuntime.MovementDisabled && Core.Player.Target != null &&
                       Core.Player.Target.IsHostile && !Core.Player.Target.IsDead,
                CommonBehaviors.MoveAndStop(
                    location => Core.Player.Target.Location,
                    ret => range,
                    true,
                    ret => $"Closing to {range} on {Core.Player.Target.Name}"));
        }

        /// <summary>Faces a live hostile target while stationary and in range. The action returns
        /// failure so the offensive priority can immediately continue into its casts.</summary>
        public static Composite FaceTarget(float maxRange)
        {
            HeroCharacter selectedTarget = null;
            return new Decorator(
                ret =>
                {
                    selectedTarget = Core.Player.Target;
                    return selectedTarget != null && selectedTarget.IsHostile && !selectedTarget.IsDead &&
                           selectedTarget.Distance <= maxRange && !Core.Player.IsMoving && !Core.Player.IsCasting &&
                           NeedsFacing(selectedTarget);
                },
                new Action(delegate
                {
                    selectedTarget.Face();
                    return RunStatus.Failure;
                }));
        }

        private static bool NeedsFacing(HeroCharacter target)
        {
            var neededHeading = HeroMath.CalculateNeededHeading(Core.Player.Location, target.Location);
            var difference = (float)System.Math.Abs(neededHeading - Core.Player.Heading);
            if (difference > 180f)
                difference = 360f - difference;

            return difference > FacingToleranceDegrees;
        }
    }
}
