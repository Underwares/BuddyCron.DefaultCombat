// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BuddyCron;
using BuddyCron.Behaviors;
using BuddyCron.Helpers;
using BuddyCron.Managers;
using BuddyCron.Navigation;
using BuddyCron.Objects;
using DefaultCombat.Behaviors;
using Reborn.Utilities;
using Reborn.Behaviors.Treesharp;
using Action = Reborn.Behaviors.Treesharp.Action;

namespace DefaultCombat.Helpers
{
    //This shit comes from Joe's
    /// <summary>Finds nearby harvestable corpses (Bioanalysis/Scavenging) and gathers them
    /// after combat.</summary>
    internal class Scavenge
    {
        private static Stopwatch _throttleTimer = Stopwatch.StartNew();
        private static TimeSpan _pauseDuration = TimeSpan.FromSeconds(3);
        private static HeroNPC _scavengeTargetCached;

        // Harvest ranks are snapshotted once per rescan: ProfessionInfos is a GOM LookupList walk
        // (unwrap + field read per entry), and CanHarvestCorpse runs against every cached NPC.
        private static bool _ranksInitialized;
        private static ulong _bioanalysisRank;
        private static ulong _scavengingRank;

        private static void RefreshProfessionRanks()
        {
            _bioanalysisRank = 0;
            _scavengingRank = 0;
            foreach (var profession in Core.Player.ProfessionInfos)
            {
                if (profession.Type == prfProfessionType.Bioanalysis)
                    _bioanalysisRank = (ulong)profession.CurrentLevel;
                else if (profession.Type == prfProfessionType.Scavenging)
                    _scavengingRank = (ulong)profession.CurrentLevel;
            }
            _ranksInitialized = true;
        }

        /// <summary>A harvestable corpse near the player, or null. The scan (and the profession-rank
        /// snapshot it uses) reruns at most every 3 seconds; the result is cached in between.</summary>
        public static HeroCharacter ScavUnit
        {
            get
            {
                if (_throttleTimer.Elapsed < _pauseDuration && _scavengeTargetCached != null)
                    return _scavengeTargetCached;

                RefreshProfessionRanks();
                _scavengeTargetCached = HeroObjectManager.GetObjectsOfType<HeroNPC>().FirstOrDefault(CanHarvestCorpse);

                _throttleTimer.Restart();
                return _scavengeTargetCached;
            }
        }

        /// <summary>Composite that moves to and interacts with <see cref="ScavUnit"/> when one is
        /// available (waiting out any in-flight cast first).</summary>
        public static Composite ScavengeCorpse
        {
            get
            {
                return new PrioritySelector(
                    new Decorator(ret => ScavUnit != null,
                        new PrioritySelector(
                            Spell.WaitForCast(),
                            CommonBehaviors.MoveAndStop(location => ScavUnit.Location, 0.2f, true),
                            new Action(ret => ScavUnit.Interact())
                            ))
                    );
            }
        }


        // RE: guards run cheapest-first — pure reads (distance, toughness, creature type, rank,
        // blacklist) gate the two members that can hit the engine: IsDead's GetHealth fallback
        // (the cached health field reads 0 on corpses) and IsFriendly's GetDemeanor.
        /// <summary>True when <paramref name="unit"/> is a dead, non-friendly, strong-or-tougher
        /// corpse within harvest range whose type the player's Bioanalysis/Scavenging rank can
        /// gather, and that is not blacklisted.</summary>
        public static bool CanHarvestCorpse(HeroNPC unit)
        {
            if (unit == null || !unit.IsValid || unit.Distance > 4.0f || !StrongOrGreater(unit))
                return false;

            if (!_ranksInitialized)
                RefreshProfessionRanks();

            // The corpse type picks the harvest skill; rank 0 = not trained.
            ulong rank = unit.CreatureType switch
            {
                cbtCreatureType.Creature => _bioanalysisRank,
                cbtCreatureType.Droid => _scavengingRank,
                _ => 0,
            };
            if (rank == 0 || rank < (ulong)(Math.Max(unit.Level - 3, 0) * 8))
                return false;

            if (Blacklist.Contains(unit.NodeId))
                return false;

            return unit.IsDead && !unit.IsFriendly;
        }

        /// <summary>Current level of the first crew skill whose name contains
        /// <paramref name="Skill"/>, or 0 when untrained.</summary>
        public static ulong PISkill(string Skill)
        {
            foreach (var pi in Core.Player.ProfessionInfos) if (pi.Name.Contains(Skill)) return (ulong)pi.CurrentLevel;
            return 0;
        }



        private static HashSet<cbtToughnessEnum> validEnums = [cbtToughnessEnum.strong, cbtToughnessEnum.boss_1 ,cbtToughnessEnum.boss_2,cbtToughnessEnum.player];
        /// <summary>True when the unit's toughness is strong, boss, or player tier (null-safe).</summary>
        public static bool StrongOrGreater(HeroCharacter unit)
        {
            if (unit != null)
            {
                return validEnums.Contains(unit.Toughness);
            }
            return false;
        }
    }
}
