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
    ///     Assassin Hatred (DoT melee dps) rotation: Death Field on cooldown, keeps the
    ///     Creeping Terror / Discharge DoTs rolling and spends Raze procs on free Eradicates.
    /// </summary>
    public class Hatred : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Hatred;

        public override string Name => "Assassin Hatred";

        public override Composite Buffs
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Mark of Power"),
                    //Re-stealth out of combat: the opener is Phantom Stride (free Raze) into the DoTs
					Spell.Buff("Stealth", ret => !Rest.KeepResting() && !DefaultCombat.MovementDisabled && !Me.IsMounted)
                    );
            }
        }

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Unbreakable Will", ret => Me.IsStunned),
                    Spell.Buff("Overcharge Saber", ret => Me.HealthPercent <= 75),
                    Spell.Buff("Deflection", ret => Me.HealthPercent <= 60),
                    Spell.Buff("Force Shroud", ret => Me.HealthPercent <= 50),
                    Spell.Buff("Recklessness", ret => Me.InCombat),
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15)
                    );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    //Phantom Stride is the stealth opener and also grants Raze
                    Spell.Cast("Phantom Stride", ret => CombatHotkeys.EnableCharge && Me.Target.Distance >= 1f),
                    Spell.Cast("Force Speed", ret => CombatHotkeys.EnableCharge && Me.IsMoving && Me.Target.Distance > 1f),

                    //Movement
                    CombatMovement.CloseDistance(Distance.Melee),

                    //Interrupts
                    Spell.Cast("Jolt", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),
                    Spell.Cast("Electrocute", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Rotation
                    //Raze finishes Eradicate's cooldown and makes it free - always spend it
                    Spell.Cast("Eradicate", ret => Me.HasBuff("Raze")),
                    //Death Field on cooldown: it applies Overwhelmed (Mental) and boosts our DoT damage
                    Spell.CastOnGround("Death Field"),
                    //Keep both 18s DoTs rolling
                    Spell.Cast("Creeping Terror",
                        ret => !Me.Target.HasMyDebuff("Creeping Terror") || Me.Target.DebuffTimeLeft("Creeping Terror") <= 3),
                    Spell.Cast("Discharge",
                        ret => !Me.Target.HasMyDebuff("Discharge") || Me.Target.DebuffTimeLeft("Discharge") <= 3),
                    Spell.Cast("Assassinate", ret => Me.Target.HealthPercent <= 30 || Me.HasBuff("Reaper's Rush")),
                    Spell.Cast("Leeching Strike", ret => Me.ForcePercent >= 30),
                    //Eradicate off cooldown even without a Raze proc (low levels never proc Raze)
                    Spell.Cast("Eradicate", ret => Me.ForcePercent >= 50),
                    Spell.Cast("Thrash", ret => Me.ForcePercent >= 45),
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
                            Spell.CastOnGround("Death Field"),
                            Spell.Cast("Discharge", ret => !Me.Target.HasMyDebuff("Discharge")),
                            Spell.Cast("Creeping Terror", ret => !Me.Target.HasMyDebuff("Creeping Terror")),
							Spell.Cast("Severing Slash"))),
                    new Decorator(ret => Targeting.ShouldPbaoe,
                        //Lacerate is the AoE filler and spreads the DoTs
                        Spell.Cast("Lacerate", ret => Me.ForcePercent >= 40))
                    );
            }
        }
    }
}
