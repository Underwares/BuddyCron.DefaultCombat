// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using System.Collections.Generic;
using System.Linq;
using BuddyCron;
using BuddyCron.Behaviors;
using BuddyCron.Helpers;
using BuddyCron.Managers;
using BuddyCron.Navigation;
using BuddyCron.Objects;
using Reborn.Utilities;
using Reborn.Utilities.Math;
using Reborn.Behaviors.Treesharp;
using System.Numerics;
using DefaultCombat.Helpers;
using Action = Reborn.Behaviors.Treesharp.Action;

namespace DefaultCombat.Core
{
    /// <summary>Per-pulse scan that computes the shared heal/dispel/tank targets and AoE decisions
    /// consumed by the rotations.</summary>
    public static class Targeting
    {
        private const int AoedpsCountNeeded = 3;
        private const int AoeHealCountNeeded = 2;

        //Settings for making target queries
        private const int MaxHealth = Health.Max;
        private const float HealingDistance = Distance.Ranged;
        private const float AoeHealDist = Distance.HealAoe;
        private const int AoeHealHp = Health.High;
        //Collections
        /// <summary>Group members considered for healing this scan.</summary>
        public static List<HeroCharacter> HealCandidates;
        /// <summary>Group members identified as tanks.</summary>
        public static List<HeroCharacter> Tanks;
        /// <summary>Positions of the heal candidates, for AoE-heal placement.</summary>
        public static List<Vector3> HealCandidatePoints;
        /// <summary>Hostile units in range this scan.</summary>
        public static List<HeroCharacter> Enemies = new List<HeroCharacter>();
        /// <summary>Positions of the enemies, for AoE placement.</summary>
        public static List<Vector3> EnemyPoints = new List<Vector3>();

        //Static Points and People
        /// <summary>Explicit tank name the user configured; empty for auto-detection.</summary>
        public static string TankName = "";
        /// <summary>Working values used while resolving <see cref="Tank"/> from <see cref="TankName"/>.</summary>
        public static string TankNameStart;
        /// <inheritdoc cref="TankNameStart"/>
        public static string TankNameCheck;
        /// <summary>The resolved tank, if any.</summary>
        public static HeroCharacter Tank;
        /// <summary>Best single-target heal recipient this scan.</summary>
        public static HeroCharacter HealTarget;
        /// <summary>Best AoE-heal anchor this scan.</summary>
        public static HeroCharacter AoeHealTarget;
        /// <summary>Best AoE-damage anchor this scan.</summary>
        public static HeroCharacter AoeDpsTarget;
        /// <summary>Group member carrying a cleansable debuff, if any.</summary>
        public static HeroCharacter DispelTarget;

        /// <summary>Ground point for targeted AoE damage.</summary>
        public static Vector3 AoeDpsPoint = Vector3.Zero;

        //Counts
        /// <summary>Heal candidates inside the AoE-heal radius.</summary>
        public static int AoeHealCount;
        /// <summary>Enemies clustered around <see cref="AoeDpsTarget"/>.</summary>
        public static int AoeDpsCount;
        /// <summary>Enemies inside point-blank AoE range of the player.</summary>
        public static int AoePeanutButterCount;
        private static readonly int s_aoepbCountNeeded = 3;
        /// <summary>True when enough hurt allies cluster to justify AoE healing.</summary>
        public static bool ShouldAoeHeal;
        /// <summary>True when enough enemies cluster to justify targeted AoE.</summary>
        public static bool ShouldAoe;
        /// <summary>True when enough enemies surround the player to justify point-blank AoE.</summary>
        public static bool ShouldPbaoe;
        /// <summary>Ground point for targeted AoE heals.</summary>
        public static Vector3 AoeHealPoint = Vector3.Zero;


        //Caching shit
        /// <summary>Scans since the object cache was rebuilt (starts expired).</summary>
        public static int cacheCount = 75;
        /// <summary>Scans a cached object list stays valid for.</summary>
        public static int maxCacheCount = 2;
        /// <summary>Cached characters used between full rescans.</summary>
        public static List<HeroCharacter> Objects;
        /// <summary>Working list for the in-progress rescan.</summary>
        public static List<HeroCharacter> objects;

        private static HeroPlayer Me => BuddyCron.Core.Player;

        //Determine if we should use the tank's target.
        private static bool UseTankTarget => Me.Target == null && Tank != null && Tank.NodeId != Me.NodeId && Tank.InCombat &&
                                             Tank.Target != null;

        /// <summary>Composite that refreshes the heal/dispel/tank targets and AoE counts each pulse;
        /// always fails so the enclosing selector continues.</summary>
        public static Composite ScanTargets
        {
            get
            {
                return new Action(delegate
                {
                    //increment shit!
                    cacheCount++;

                    //Reset counts
                    AoeHealCount = 0;
                    AoeDpsCount = 0;
                    AoePeanutButterCount = 0;
                    //Reset Targets
                    //     Tank = null;
                    HealTarget = null;
                    AoeHealTarget = null;
                    AoeHealPoint = Vector3.Zero;
                    DispelTarget = null;


                    //Reset Lists and shit
                    HealCandidates = new List<HeroCharacter>();
                    HealCandidatePoints = new List<Vector3>();
                    EnemyPoints = new List<Vector3>();
                    Tanks = new List<HeroCharacter>();
                    ShouldAoeHeal = false;
                    var objects = GetHeroCharacters();


                    //update the cache when we feel like it
                    if (cacheCount >= maxCacheCount)
                        updateObjects();

                    if (DefaultCombat.IsHealer)
                    {
                        foreach (var p in Objects)
                        {
                            //Doing this shit early

                            //    Logger.Write(p.Name);
                            if (Tank != null && Tank == p)
                                Tank = p;

                            if (Tank == null && p.Name == TankName && !p.IsDead)
                                Tank = p;

                            // Got a Focus Tank?
                            if (Tank == null && Me.FocusTargetIsActive && p.NodeId == Me.FocusTargetId && !p.IsDead)
                                Tank = p;

                            if (p.IsPartyRoleTank())
                                Tanks.Add(p);

                            // Damn couldnt find a tank ima be the boss!

                            if (Tank == null && Me.Companion != null)
                                Tank = Me.Companion;

                            if (Tank == null && Me.Companion == null)
                                Tank = Me;

                            //Check for HealTarget
                            //if (p.Health <= p.statHealth && !p.IsDead)
							if (p.HealthPercent <= MaxHealth && !p.IsDead)
                            {
                                //if (HealTarget == null || p.Health < HealTarget.statHealth)
								if (HealTarget == null || p.HealthPercent < HealTarget.HealthPercent)
                                    HealTarget = p;

                                //Add to candidtates list
                                HealCandidates.Add(p);
                                HealCandidatePoints.Add(p.Location);

                                //increment our AOEHealCount
                                //if (p.Health <= AoeHealHp)
								if (p.HealthPercent <= AoeHealHp)
                                    AoeHealCount++;
                            }

                            if (p.NeedsCleanse())
                            {
                                //if (DispelTarget != null && p.Health < DispelTarget.statHealth)
								if (DispelTarget != null && p.HealthPercent < DispelTarget.HealthPercent)
                                    DispelTarget = p;

                                if (DispelTarget == null)
                                    DispelTarget = p;
                            }
                        }


                        //We have checked everyone out, lets set AOE stuff
                        if (AoeHealCount >= AoeHealCountNeeded)
                        {
                            ShouldAoeHeal = true;
                            //AOEHealTarget
                            AoeHealTarget = AoeHealLocation(AoeHealDist);

                            //AOEHealPoint
                            if (AoeHealTarget != null)
                                AoeHealPoint = AoeHealLocation(AoeHealTarget);
                        }
                    }

                    foreach (var c in objects)
                    {
                        //Dps
                        if (c.IsValidTarget())
                        {
                            //Enemies.Add(c);
                            EnemyPoints.Add(c.Location);
                        }

                        if (Me.Target != null)
                            ShouldAoe = CheckDpsAoe(AoedpsCountNeeded, Distance.MeleeAoE, Me.Target.Location);

                        ShouldPbaoe = CheckDpsAoe(AoedpsCountNeeded, Distance.MeleeAoE, Me.Location);
                    }


                    return RunStatus.Failure;
                });
            }
        }

        /// <summary>Assigns the current friendly target as the tank (by name), or clears the
        /// assignment when re-invoked on the same character or with no friendly target.</summary>
        public static void SetTank()
        {
            TankNameStart = Me.Target.ToString();
            TankNameCheck = TankNameStart.Substring(0, TankNameStart.IndexOf(','));

            if (Me.Target != null && Me.Target.IsFriendly && !TankName.Equals(TankNameCheck))
            {
                TankName = TankNameStart.Substring(0, TankNameStart.IndexOf(','));
                Logger.Write("Tank set to : " + TankName);
                Tank = null;
            }
            else
            {
                TankName = "";
                Tank = null;
                Logger.Write("Cleared Tank");
            }
        }

        private static void updateObjects()
        {
            if (DefaultCombat.IsHealer)
            {
                Objects = Me.PartyMembers(false).ToList().FindAll(p =>
                    !p.IsDead
                    && p.DistanceSqr < HealingDistance * HealingDistance
                    && p.InLineOfSight);

                if (!Objects.Contains(Me))
                    Objects.Add(Me);

                if (Me.Companion != null && !Objects.Contains(Me.Companion))
                    Objects.Add(Me.Companion);
            }

            //Reset dat count
            cacheCount = 0;
        }

        /// <summary>Snapshot of all NPCs in the object manager as <see cref="HeroCharacter"/>
        /// (source list for the enemy scan).</summary>
        public static List<HeroCharacter> GetHeroCharacters()
        {
            //List<HeroCharacter> objects = HeroObjectManager.GetObjectsOfType<HeroCharacter>().ToList();

            /*
            if (Me.Companion != null)
                objects.Add(Me.Companion);
            */
            var npcs = HeroObjectManager.GetObjectsOfType<HeroNPC>();
            var objects = npcs.Cast<HeroCharacter>().ToList();

            return objects;
        }

        private static Vector3 AoeHealLocation(HeroCharacter p)
        {
            return p != null ? p.Location : Vector3.Zero;
        }

        /// <summary>Heal candidate with the most other candidates within <paramref name="dist"/>
        /// (tank or self as the baseline), or null when too few cluster to justify an AoE heal.</summary>
        private static HeroCharacter AoeHealLocation(float dist)
        {
            HeroCharacter pt = Me;

            if (Tank != null)
                pt = Tank;

            var currentPtCount = PointsAroundPoint(pt.Location, HealCandidatePoints, dist);
            var tempCount = 0;
            foreach (var p in HealCandidates)
            {
                tempCount = PointsAroundPoint(p.Location, HealCandidatePoints, dist);
                if (p.NodeId != Me.NodeId && tempCount > currentPtCount)
                {
                    pt = p;
                    currentPtCount = tempCount;
                }
            }

            return tempCount >= AoeHealCountNeeded ? pt : null;
        }

        /// <summary>True when at least <paramref name="minMobs"/> tracked enemies are within
        /// <paramref name="distance"/> of <paramref name="center"/>.</summary>
        public static bool CheckDpsAoe(int minMobs, float distance, Vector3 center)
        {
            return PointsAroundPoint(center, EnemyPoints, distance) >= minMobs;
        }

        private static int PointsAroundPoint(Vector3 pt, List<Vector3> l, float dist)
        {
            var maxDistance = dist * dist;
            return l.Count(p => p.DistanceSqr(pt) <= maxDistance);
        }
    }
}
