// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license


using BuddyCron;
using BuddyCron.Behaviors;
using BuddyCron.Helpers;
using BuddyCron.Managers;
using BuddyCron.Navigation;
using BuddyCron.Objects;
using DefaultCombat.Behaviors;
using Reborn.Utilities;
using Reborn.Behaviors.Treesharp;
using DefaultCombat.Helpers;

namespace DefaultCombat.Routines
{
    /// <summary>
    ///     Gunslinger Saboteur (ranged dps) rotation: Shock Charge / Explosive Charge /
    ///     Incendiary Grenade upkeep detonated by Sabotage, with Speed Shot on cooldown.
    /// </summary>
    public class Saboteur : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Saboteur;

        public override string Name => "Gunslinger Saboteur";


        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Lucky Shots")
        );


        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    //Survival
                    Spell.Buff("Escape", ret => Core.Player.IsStunned),
                    Spell.Buff("Defense Screen", ret => Core.Player.HealthPercent <= 70),
                    Spell.Buff("Dodge", ret => Core.Player.HealthPercent <= 40),
                    Spell.Buff("Hunker Down", ret => Core.Player.IsInCover() && Core.Player.HealthPercent <= 60),
                    //Ability tree choice (lvl 68) -- silently skipped when not chosen
                    Spell.Buff("Scrambling Field", ret => Core.Player.HealthPercent <= 50),
                    //Resets Defense Screen/Dodge/Hunker Down/Hightail It when things go bad
                    Spell.Buff("Bag of Tricks", ret => Core.Player.HealthPercent <= 25),

                    //Energy -- Saboteur is hungry, Cool Head at 45-50% keeps us in the top regen band
                    Spell.Cast("Cool Head", ret => Core.Player.EnergyPercent <= 50),

                    //Offensive
                    Spell.Cast("Smuggler's Luck", ret => Core.Player.Target.StrongOrGreater()),
                    //Ability tree choice (lvl 43) -- silently skipped when not chosen
                    Spell.Cast("Illegal Mods", ret => Core.Player.Target.StrongOrGreater()),

                    Spell.Buff("Unity", ret => Core.Player.Companion != null && Core.Player.HealthPercent <= 15)
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
                    RotationRuntime.HeroicMoment,

                    //Cover -- Charged Burst/Speed Shot/Sweeping Gunfire need it and it is our energy regen
                    Spell.Buff("Crouch", ret => !Core.Player.IsInCover() && !Core.Player.IsMoving),

                    //Interrupt
                    Spell.Cast("Distraction", ret => Core.Player.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Low Energy -- Seize the Moment makes Thermal Grenade free, so it is still worth firing
                    new Decorator(ret => Core.Player.EnergyPercent < 45,
                        new PrioritySelector(
                            Spell.Cast("Thermal Grenade", ret => Core.Player.HasBuff("Seize the Moment")),
                            Spell.Cast("Flurry of Bolts")
                            )),

                    //Establish Shock Charge before the charge/channel cycle. Speed Shot refreshes it
                    //with the recommended tactical; DoT reapplies it when that refresh is absent.
                    Spell.DoT("Shock Charge", "Shock Charge"),
                    Spell.Cast("Explosive Charge"),
                    Spell.Cast("Speed Shot"),
                    Spell.DoTGround("Incendiary Grenade", 9000),
                    //Sabotage detonates our charges; do not let the Shock Charge gate lock it out
                    //on a low level character that has not trained Shock Charge yet.
                    Spell.Cast("Sabotage", ret => Core.Player.Target.HasMyDebuff("Shock Charge") || Core.Player.Level < 30),
                    Spell.Cast("Thermal Grenade", ret => Core.Player.HasBuff("Seize the Moment")),
                    Spell.DoT("Vital Shot", "Vital Shot"),
                    Spell.CastOnGround("Bombing Run", ret => Core.Player.EnergyPercent > 70),
                    //Ability tree choice (lvl 27) for Saboteur -- skipped when not chosen
                    Spell.Cast("Quickdraw", ret => Core.Player.Target.HealthPercent <= 30),

                    //Filler -- keeps low level characters (most of the list above untrained) moving
                    Spell.Cast("Charged Burst"),

                    //Never stall
                    Spell.Cast("Flurry of Bolts")
                    );
            }
        }


        public override Composite AreaOfEffect
        {
            get
            {
                return new Decorator(ret => Targeting.ShouldAoe,
                    new PrioritySelector(
                        Spell.Buff("Crouch", ret => !Core.Player.IsInCover() && !Core.Player.IsMoving),
                        Spell.DoTGround("Incendiary Grenade", 9000),
                        //Bombing Run (7.0 rename of XS Freighter Flyby)
                        Spell.CastOnGround("Bombing Run", ret => Core.Player.EnergyPercent > 50),
                        //Sow Chaos makes Sabotage cleave off anything burning with Blazing Speed
                        Spell.Cast("Sabotage", ret => Core.Player.Target.HasMyDebuff("Shock Charge") || Core.Player.Level < 30),
                        Spell.Cast("Thermal Grenade", ret => Core.Player.EnergyPercent > 50),
                        Spell.CastOnGround("Sweeping Gunfire", ret => Core.Player.IsInCover() && Core.Player.EnergyPercent > 30)
                        ));
            }
        }
    }
}
