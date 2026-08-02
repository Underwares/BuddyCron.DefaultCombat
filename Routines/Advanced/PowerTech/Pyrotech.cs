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
    // 7.x Pyrotech. Me.EnergyPercent is resource REMAINING (100 = no heat, 0 = overheated),
    // so "low EnergyPercent" == "high heat" == conserve.
    /// <summary>
    ///     PowerTech Pyrotech (DoT melee dps) rotation: keeps the burns up so Rail Shot stays
    ///     usable and spends Superheated Flamethrower stacks on Searing Wave.
    /// </summary>
    public class Pyrotech : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.FirebugPyrotech;

        public override string Name => "PowerTech Pyrotech";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Hunter's Boon")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Determination", ret => Me.IsStunned),

                    //Interrupt lives here so a heat-starved rotation can never swallow it
                    Spell.Cast("Quell", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Defensives
                    Spell.Buff("Energy Shield", ret => Me.HealthPercent <= 60),
                    Spell.Buff("Kolto Overload", ret => Me.HealthPercent <= 30),
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15),

                    //Heat: 7.0 folded Thermal Sensor Override into Vent Heat
                    Spell.Cast("Vent Heat", ret => Me.EnergyPercent <= 40),

                    //Offensive cooldowns
                    Spell.Cast("Explosive Fuel", ret => Me.InCombat && Me.Target.StrongOrGreater()),
                    Spell.Cast("Shoulder Cannon", ret => Me.InCombat && !Me.HasBuff("Shoulder Cannon")),

                    //DoT upkeep -- Rail Shot needs the target burning
                    Spell.DoT("Incendiary Missile", "Burning (Incendiary Missile)"),
                    Spell.DoT("Scorch", "Scorch")
                    );
            }
        }

        public override Composite SingleTarget
        {
            get
            {
                return new PrioritySelector(
                    Spell.Cast("Jet Charge", ret => CombatHotkeys.EnableCharge && Me.Target.Distance >= 1f),

                    CombatMovement.CloseDistance(Distance.Melee),

                    //Legacy Heroic Moment Abilities --will only be active when user initiates Heroic Moment--
                    HeroicComposite,

                    //Overheated -- free casts only until heat bleeds off
                    new Decorator(ret => Me.EnergyPercent <= 40,
                        new PrioritySelector(
                            Spell.Cast("Flame Burst", ret => Me.HasBuff("Flame Barrage")),
                            Spell.Cast("Rapid Shots")
                            )),

                    //Rotation: Searing Wave at 2 stacks -> Immolate (Consuming Flames) -> Rail Shot -> Flaming Fist
                    Spell.Cast("Searing Wave", ret => Me.BuffCount("Superheated Flamethrower") >= 2 || Me.Level < 50),
                    Spell.Cast("Immolate", ret => Me.HasBuff("Consuming Flames") || Me.Level < 50),
                    //Rail Shot is only usable on a target suffering periodic damage (or CC'd) unless the
                    //caster has Prototype Rail (Advanced Prototype only). Pyrotech's two DoTs are the only
                    //burns we bring -- there is no aura literally named "Burning".
                    Spell.Cast("Rail Shot", ret => Me.Target.HasMyDebuff("Burning (Incendiary Missile)") || Me.Target.HasMyDebuff("Scorch")),
                    Spell.Cast("Flaming Fist"),
                    Spell.Cast("Shoulder Cannon", ret => Me.HasBuff("Shoulder Cannon") && Me.Target.StrongOrGreater()),

                    //Fallback so a missing proc-passive can never park Immolate
                    Spell.Cast("Immolate"),

                    //Fillers
                    Spell.Cast("Flame Burst", ret => Me.HasBuff("Flame Barrage")),
                    Spell.Cast("Flame Burst", ret => Me.EnergyPercent >= 65),
                    Spell.Cast("Rapid Shots")
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
                            CombatMovement.CloseDistance(Distance.MeleeAoE),
                            Spell.DoT("Incendiary Missile", "Burning (Incendiary Missile)"),
                            Spell.DoT("Scorch", "Scorch"),
                            Spell.CastOnGround("Deadly Onslaught")
                            )),
                    new Decorator(ret => Targeting.ShouldPbaoe,
                        new PrioritySelector(
                            CombatMovement.CloseDistance(Distance.MeleeAoE),
                            Spell.Cast("Searing Wave"),
                            Spell.Cast("Flame Sweep", ret => Me.HasBuff("Flame Barrage")),
                            //(Shatter Slug is granted to Advanced Prototype and Shield Tech only -- not Pyrotech.)
                            Spell.Cast("Flame Sweep", ret => Me.EnergyPercent >= 50)
                            )));
            }
        }
    }
}
