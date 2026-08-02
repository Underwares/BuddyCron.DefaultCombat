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
    ///     7.x Sentinel Watchman (melee DoT DPS) rotation: keeps the Cauterize / Overload Saber /
    ///     Force Melt burns rolling and lines Zen autocrits up with them.
    /// </summary>
    public class Watchman : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Watchman;

        public override string Name => "Sentinel Watchman";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Force Might")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Resolute", ret => Me.IsStunned),

                    //Defensives -- keep these first, they are what keeps a leveling character alive
                    Spell.Buff("Rebuke", ret => Me.HealthPercent <= 75),
                    Spell.Buff("Saber Ward", ret => Me.HealthPercent <= 50),
                    Spell.Cast("Force Camouflage", ret => Me.HealthPercent <= 35),
                    Spell.Buff("Guarded by the Force", ret => Me.HealthPercent <= 20),
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15),

                    //Offensive cooldowns
                    Spell.Buff("Force Clarity", ret => Me.Target != null && Me.Target.StrongOrGreater()),
                    Spell.Buff("Inspiration", ret => CombatHotkeys.EnableRaidBuffs),

                    //Overload Saber is off-GCD -- keep the burn charge rolling at all times
                    Spell.Cast("Overload Saber", ret => !Me.HasBuff("Overload Saber")),

                    //Valorous Call tops Centering back up so Zen comes around again sooner
                    Spell.Cast("Valorous Call", ret => !Me.HasBuff("Zen") && Me.BuffCount("Centering") < 10),

                    //Zen makes the burns autocrit -- only worth it once the burns are actually rolling
                    Spell.Cast("Zen",
                        ret => !Me.HasBuff("Zen") && Me.Target != null &&
                               (Me.Target.HasDebuff("Burning (Cauterize)") ||
                                Me.Target.HasDebuff("Burning (Overload Saber)") ||
                                Me.Level < 30))
                    );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    Spell.Cast("Force Leap", ret => CombatHotkeys.EnableCharge && Me.Target.Distance >= 1f),

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Rotation
                    Spell.Cast("Force Kick", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Burn upkeep -- Cauterize is the only burn that can fall off (9s duration), so it is
                    //refreshed as it expires. Force Melt outlasts nothing and is simply used on cooldown.
                    Spell.Cast("Cauterize",
                        ret => !Me.Target.HasMyDebuff("Burning (Cauterize)") ||
                               Me.Target.DebuffTimeLeft("Burning (Cauterize)") <= 2),
                    Spell.Cast("Force Melt"),

                    //Core damage -- Merciless Slash on cooldown, its cooldown shrinks with Merciless stacks
                    Spell.Cast("Merciless Slash"),
                    Spell.Cast("Dispatch", ret => Me.Target.HealthPercent <= 30),

                    //Mind Sear makes the next Twin Saber Throw hit twice as hard and resets its cooldown
                    Spell.Cast("Twin Saber Throw", ret => Me.HasBuff("Mind Sear") && Me.Target.Distance <= 1f),

                    //Fillers -- Blade Barrage is free, so it comes before the focus spenders
                    Spell.Cast("Blade Barrage"),
                    Spell.Cast("Slash", ret => Me.ActionPoints >= 6),
                    Spell.Cast("Zealous Strike", ret => Me.ActionPoints <= 8),
                    Spell.Cast("Twin Saber Throw", ret => Me.Target.Distance <= 1f),

                    //Never stall -- free basic attack that builds focus
                    Spell.Cast("Strike")
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new Decorator(ret => Targeting.ShouldPbaoe,
                    new PrioritySelector(
                        //Burns first -- Force Sweep spreads them to everything around the target
                        Spell.Cast("Cauterize", ret => !Me.Target.HasMyDebuff("Burning (Cauterize)")),
                        Spell.Cast("Force Melt"),
                        Spell.Cast("Twin Saber Throw", ret => Me.Target.Distance <= 1f),
                        Spell.Cast("Force Sweep"),
                        Spell.Cast("Merciless Slash"),
                        Spell.Cast("Cyclone Slash", ret => Me.ActionPoints >= 5),
                        Spell.Cast("Blade Barrage"),
                        Spell.Cast("Zealous Strike", ret => Me.ActionPoints <= 8)
                        ));
            }
        }
    }
}
