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
    ///     7.x Sorcerer Madness (ranged DoT DPS) rotation: keeps Affliction and Creeping Terror
    ///     up, spreads them with Death Field and spends 4-stack Wrath on Demolish.
    /// </summary>
    public class Madness : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Madness;

        public override string Name => "Sorcerer Madness";


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

                    //Align throughput cooldowns with an established DoT window on durable targets.
                    Spell.Cast("Polarity Shift",
                        ret => Me.Target != null && Me.Target.StrongOrGreater() &&
                               (Me.Target.HasMyDebuff("Affliction") || !AbilityManager.HasAbility("Affliction"))),
                    Spell.Cast("Recklessness",
                        ret => Me.Target != null && Me.Target.StrongOrGreater() &&
                               (Me.Target.HasMyDebuff("Affliction") || !AbilityManager.HasAbility("Affliction"))),
                    Spell.Buff("Force Speed", ret => Me.IsMoving),
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

                    //Interrupt (Electrocute is the backup interrupt, bosses are stun immune)
                    Spell.Cast("Jolt", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),
                    Spell.Cast("Electrocute",
                        ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts && !Me.Target.BossOrGreater()),

                    //Consume Wrath carried from a previous channel before refreshing setup effects.
                    Spell.Cast("Demolish", ret => Me.BuffCount("Wrath") >= 4),

                    //DoTs first, everything else in Madness scales off them.
                    Spell.DoT("Affliction", "Affliction"),
                    Spell.DoT("Creeping Terror", "Creeping Terror"),

                    //Rotation
                    Spell.CastOnGround("Death Field"),   // applies Deathmark, spreads DoTs
                    Spell.Cast("Demolish", ret => Me.BuffCount("Wrath") >= 4 || Me.Level < 50),
                    Spell.Cast("Force Leech"),
                    Spell.Cast("Lightning Strike", ret => Me.BuffCount("Wrath") >= 4),
                    Spell.Cast("Shock", ret => Me.Level < 27),   // pre-Plague Master filler only

                    //Filler / Wrath builder
                    Spell.Cast("Force Lightning"),
                    Spell.Cast("Saber Strike", ret => Me.ForcePercent <= 25)
                    );
            }
        }


        public override Composite AreaOfEffect
        {
            get
            {
                return new Decorator(ret => Targeting.ShouldAoe,
                    new PrioritySelector(
                        Spell.DoT("Affliction", "Affliction"),
                        Spell.DoT("Creeping Terror", "Creeping Terror"),
                        Spell.CastOnGround("Death Field"),
                        Spell.Cast("Demolish", ret => Me.BuffCount("Wrath") >= 4 || Me.Level < 50),
                        Spell.CastOnGround("Force Storm"),
                        Spell.Cast("Force Lightning")
                        ));
            }
        }
    }
}
