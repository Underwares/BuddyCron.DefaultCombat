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
    ///     7.x Sorcerer Lightning (ranged burst DPS) rotation, built around the Affliction
    ///     auto-crit setup for Thundering Blast and the Lightning Storm / Force Flash procs.
    /// </summary>
    public class Lightning : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Lightning;

        public override string Name => "Sorcerer Lightning";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Mark of Power")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    //Break CC
                    Spell.Buff("Unbreakable Will", ret => Me.IsStunned),

                    //Defensives
                    Spell.Buff("Force Barrier", ret => Me.HealthPercent <= 15),
                    Spell.Buff("Unnatural Preservation", ret => Me.HealthPercent <= 60),
                    Spell.HoT("Static Barrier", on => Me, 100, ret => Me.InCombat && !Me.HasDebuff("Deionized")),

                    //Force management (Consuming Darkness applies Weary without Force Surge, so only use it starved)
                    Spell.Buff("Consuming Darkness", ret => Me.ForcePercent <= 25 && !Me.HasDebuff("Weary")),

                    //Start Polarity Shift before applying the long-lived DoT. Hold the charge-based
                    //cooldowns for Lightning Flash's Force Flash window instead of wasting them on setup.
                    Spell.Cast("Polarity Shift",
                        ret => Me.InCombat && Me.Target != null && Me.Target.StrongOrGreater() &&
                               (!Me.Target.HasMyDebuff("Affliction") || Me.HasBuff("Force Flash"))),
                    Spell.Cast("Recklessness",
                        ret => Me.HasBuff("Force Flash") || !AbilityManager.HasAbility("Lightning Flash")),
                    Spell.Buff("Force Speed",
                        ret => Me.HasBuff("Force Flash") || !AbilityManager.HasAbility("Lightning Flash")),
                    Spell.Buff("Unlimited Power", ret => CombatHotkeys.EnableRaidBuffs),

                    //Companion
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15)
                    );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    //Movement
                    CombatMovement.CloseDistance(Distance.Ranged),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Interrupt
                    Spell.Cast("Jolt", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Affliction must be up: it makes Thundering Blast an automatic crit
                    Spell.DoT("Affliction", "Affliction"),

                    //Rotation
                    Spell.Cast("Thundering Blast"),
                    Spell.Cast("Lightning Flash"),   // grants Force Flash + Stormwatch
                    Spell.Cast("Crushing Darkness", ret => Me.HasBuff("Force Flash") || Me.Level < 50),
                    Spell.Cast("Shock", ret => Me.Target.HasMyDebuff("Crushed (Crushing Darkness)") || Me.Level < 26),
                    Spell.Cast("Chain Lightning", ret => Me.HasBuff("Lightning Storm")),
                    // lvl 23 choice (apc.sith_inquisitor.sorcerer.lightning_mods), replaces Chain Lightning.
                    // NB: the client's name string is "Halted Offensive " WITH a trailing space, so
                    // AbilityManager matches ability names whitespace-insensitively.
                    Spell.Cast("Halted Offensive", ret => Me.HasBuff("Lightning Storm")),
                    //Volt Rush is a movement fallback unless a tactical-specific AoE policy owns it.
                    Spell.Cast("Volt Rush", ret => Me.IsMoving),

                    //Fillers
                    Spell.Cast("Lightning Bolt"),
                    Spell.Cast("Lightning Strike"),   // pre-Lightning-Bolt filler on low level characters
                    Spell.Cast("Saber Strike", ret => Me.ForcePercent <= 30)
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new Decorator(ret => Targeting.ShouldAoe,
                    new PrioritySelector(
                        Spell.Cast("Chain Lightning", ret => Me.HasBuff("Lightning Storm")),
                        Spell.Cast("Halted Offensive", ret => Me.HasBuff("Lightning Storm")),
                        Spell.DoT("Affliction", "Affliction"),
                        Spell.Cast("Chain Lightning"),
                        Spell.Cast("Halted Offensive"),
                        Spell.CastOnGround("Force Storm")
                        ));
            }
        }
    }
}
