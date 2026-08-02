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
    ///     7.x Shadow Serenity (melee DoT DPS) rotation: keeps Sever Force and Force Breach
    ///     rolling and spends the Force Strike / Crush Spirit procs.
    /// </summary>
    public class Serenity : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Serenity;

        public override string Name => "Shadow Serenity";

        public override Composite Buffs
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Force Valor"),
                    //Re-stealth out of combat: the opener is Shadow Stride (free Squelch) into the DoTs
					Spell.Buff("Stealth", ret => !Rest.KeepResting() && !DefaultCombat.MovementDisabled && !Me.IsMounted)
                    );
            }
        }

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Force of Will", ret => Me.IsStunned),
                    Spell.Buff("Battle Readiness", ret => Me.HealthPercent <= 75),
                    Spell.Buff("Deflection", ret => Me.HealthPercent <= 60),
                    Spell.Buff("Resilience", ret => Me.HealthPercent <= 50),
                    Spell.Buff("Force Potency", ret => Me.InCombat),
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15)
                    );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    //Shadow Stride is the stealth opener and also grants Force Strike
                    Spell.Cast("Shadow Stride", ret => CombatHotkeys.EnableCharge && Me.Target.Distance >= 1f),
                    Spell.Cast("Force Speed", ret => CombatHotkeys.EnableCharge && Me.IsMoving && Me.Target.Distance > 1f),

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Interrupts
                    Spell.Cast("Mind Snap", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),
                    Spell.Cast("Force Stun", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Rotation
                    //Force Strike finishes Squelch's cooldown and makes it free - always spend it
                    Spell.Cast("Squelch", ret => Me.HasBuff("Force Strike")),
                    //Force in Balance on cooldown: it applies Overwhelmed (Mental) and boosts our DoT damage
                    Spell.CastOnGround("Force in Balance"),
                    //Keep both 18s DoTs rolling
                    Spell.Cast("Sever Force",
                        ret => !Me.Target.HasMyDebuff("Sever Force") || Me.Target.DebuffTimeLeft("Sever Force") <= 3),
                    Spell.Cast("Force Breach",
                        ret => !Me.Target.HasMyDebuff("Force Breach") || Me.Target.DebuffTimeLeft("Force Breach") <= 3),
                    //Crush Spirit finishes Spinning Strike's cooldown and lets it be used early
                    Spell.Cast("Spinning Strike",
                        ret => Me.Target.HealthPercent <= 30 || Me.HasBuff("Crush Spirit") || Me.HasBuff("Stalker's Swiftness")),
                    Spell.Cast("Serenity Strike", ret => Me.ForcePercent >= 30),
                    //Squelch off cooldown even without a Force Strike proc (low levels never proc it)
                    Spell.Cast("Squelch", ret => Me.ForcePercent >= 50),
                    Spell.Cast("Double Strike", ret => Me.ForcePercent >= 45),
                    Spell.Cast("Saber Strike")
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new PrioritySelector(
                    new Decorator(ret => Targeting.ShouldAoe,
                        new PrioritySelector(
                            Spell.CastOnGround("Force in Balance"),
                            Spell.Cast("Force Breach", ret => !Me.Target.HasMyDebuff("Force Breach")),
                            Spell.Cast("Sever Force", ret => !Me.Target.HasMyDebuff("Sever Force")),
                            //Cleaving Cut spreads both DoTs and heals for 50% of the damage dealt
							Spell.Cast("Cleaving Cut"))),
                    new Decorator(ret => Targeting.ShouldPbaoe,
                        //Whirling Blow is the AoE filler and also spreads the DoTs
                        Spell.Cast("Whirling Blow", ret => Me.ForcePercent >= 40))
                    );
            }
        }
    }
}
