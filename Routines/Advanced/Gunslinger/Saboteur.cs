// Copyright (C) 2011-2018 Bossland GmbH// See the file LICENSE for the source code's detailed license


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
                    Spell.Buff("Escape", ret => Me.IsStunned),
                    Spell.Buff("Defense Screen", ret => Me.HealthPercent <= 70),
                    Spell.Buff("Dodge", ret => Me.HealthPercent <= 40),
                    Spell.Buff("Hunker Down", ret => Me.IsInCover() && Me.HealthPercent <= 60),
                    //Ability tree choice (lvl 68) -- silently skipped when not chosen
                    Spell.Buff("Scrambling Field", ret => Me.HealthPercent <= 50),
                    //Resets Defense Screen/Dodge/Hunker Down/Hightail It when things go bad
                    Spell.Buff("Bag of Tricks", ret => Me.HealthPercent <= 25),

                    //Energy -- Saboteur is hungry, Cool Head at 45-50% keeps us in the top regen band
                    Spell.Cast("Cool Head", ret => Me.EnergyPercent <= 50),

                    //Offensive
                    Spell.Cast("Smuggler's Luck", ret => Me.Target.StrongOrGreater()),
                    //Ability tree choice (lvl 43) -- silently skipped when not chosen
                    Spell.Cast("Illegal Mods", ret => Me.Target.StrongOrGreater()),

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

                    //Cover -- Charged Burst/Speed Shot/Sweeping Gunfire need it and it is our energy regen
                    Spell.Buff("Crouch", ret => !Me.IsInCover() && !Me.IsMoving),

                    //Interrupt
                    Spell.Cast("Distraction", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Low Energy -- Seize the Moment makes Thermal Grenade free, so it is still worth firing
                    new Decorator(ret => Me.EnergyPercent < 45,
                        new PrioritySelector(
                            Spell.Cast("Thermal Grenade", ret => Me.HasBuff("Seize the Moment")),
                            Spell.Cast("Flurry of Bolts")
                            )),

                    //Rotation
                    Spell.DoT("Shock Charge", "Shock Charge"),
                    //Speed Shot is a huge chunk of our damage and shares a cooldown length with
                    //Incendiary Grenade's duration -- it goes on cooldown, every cooldown
                    Spell.Cast("Speed Shot"),
                    Spell.DoTGround("Incendiary Grenade", 9000),
                    Spell.Cast("Explosive Charge"),
                    //Sabotage detonates our charges; do not let the Shock Charge gate lock it out
                    //on a low level character that has not trained Shock Charge yet
                    Spell.Cast("Sabotage", ret => Me.Target.HasMyDebuff("Shock Charge") || Me.Level < 30),
                    Spell.Cast("Thermal Grenade", ret => Me.HasBuff("Seize the Moment")),
                    Spell.DoT("Vital Shot", "Vital Shot"),
                    Spell.CastOnGround("Bombing Run", ret => Me.EnergyPercent > 70),
                    //Ability tree choice (lvl 27) for Saboteur -- skipped when not chosen
                    Spell.Cast("Quickdraw", ret => Me.Target.HealthPercent <= 30),

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
                        Spell.Buff("Crouch", ret => !Me.IsInCover() && !Me.IsMoving),
                        Spell.DoTGround("Incendiary Grenade", 9000),
                        //Bombing Run (7.0 rename of XS Freighter Flyby)
                        Spell.CastOnGround("Bombing Run", ret => Me.EnergyPercent > 50),
                        //Sow Chaos makes Sabotage cleave off anything burning with Blazing Speed
                        Spell.Cast("Sabotage", ret => Me.Target.HasMyDebuff("Shock Charge") || Me.Level < 30),
                        Spell.Cast("Thermal Grenade", ret => Me.EnergyPercent > 50),
                        Spell.CastOnGround("Sweeping Gunfire", ret => Me.IsInCover() && Me.EnergyPercent > 30)
                        ));
            }
        }
    }
}
