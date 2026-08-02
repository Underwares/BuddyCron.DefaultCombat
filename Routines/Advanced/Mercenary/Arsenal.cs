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
using Targeting = DefaultCombat.Core.Targeting;

namespace DefaultCombat.Routines
{
    // 7.x Arsenal Mercenary.
    // Heat convention: Me.EnergyPercent is the REMAINING resource (100 = stone cold, 0 = fully
    // overheated), so "hot" == low EnergyPercent. Rapid Shots is the free heat dump.
    /// <summary>
    ///     Mercenary Arsenal (ranged burst dps) rotation: Priming Shot / Tracer Missile feed
    ///     Rail Shot, Heatseeker Missiles and Blazing Bolts.
    /// </summary>
    public class Arsenal : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.Arsenal;

        public override string Name => "Mercenary Arsenal";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Hunter's Boon")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Determination", ret => Me.IsStunned),

                    //Defensives -- these are what keep a leveling character alive
                    Spell.Buff("Energy Shield", ret => Me.HealthPercent <= 70),
                    Spell.Buff("Chaff Flare", ret => Me.HealthPercent <= 50),           //ability-tree choice (~43)
                    Spell.Buff("Kolto Overload", ret => Me.HealthPercent <= 35),
                    Spell.Buff("Responsive Safeguards", ret => Me.HealthPercent <= 30), //ability-tree choice (~68)
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15),

                    //Heat / offensive cooldowns
                    Spell.Buff("Vent Heat", ret => Me.InCombat && Me.EnergyPercent <= 40),
                    //Arsenal's Supercharge spender is named "High Velocity Supercharged Gas"
                    //(abl.bounty_hunter.skill.arsenal.supercharged_gas_high_velocity). Plain
                    //"Supercharged Gas" is the Innovative Ordnance one and never matches here.
                    Spell.Buff("High Velocity Supercharged Gas", ret => Me.InCombat && Me.BuffCount("Supercharge") >= 10),
                    Spell.Buff("Power Surge", ret => Me.InCombat)
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
                    Spell.Cast("Disabling Shot", ret => Me.Target.IsCasting && CombatHotkeys.EnableInterrupts),

                    //Emergency heat dump -- Rapid Shots is free and lets heat bleed off
                    Spell.Cast("Rapid Shots", ret => Me.EnergyPercent <= 25),

                    //Rotation
                    Spell.Cast("Priming Shot"),
                    Spell.Cast("Tracer Missile", ret => Me.HasBuff("Tracer Beacon")),   //Buff 12s, from Priming Shot
                    Spell.Cast("Rail Shot", ret => Me.BuffCount("Tracer Lock") >= 5 || Me.Level < 40),   //Buff, caps at 5
                    Spell.Cast("Heatseeker Missiles", ret => Me.Target.HasDebuff("Heat Signature") || Me.Level < 40),   //Debuff 45s, from Tracer Missile
                    Spell.Cast("Electro Net", ret => Me.Target.StrongOrGreater()),
                    Spell.Cast("Blazing Bolts", ret => Me.EnergyPercent >= 40),         //reset by the Barrage proc
                    Spell.Cast("Tracer Missile", ret => Me.EnergyPercent >= 50),

                    //Never stall: free basic attack
                    Spell.Cast("Rapid Shots")
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new Decorator(ret => Targeting.ShouldAoe,
                    new PrioritySelector(
                        Spell.CastOnGround("Death from Above"),
                        Spell.Cast("Fusion Missile", ret => Me.EnergyPercent >= 50),
                        Spell.CastOnGround("Sweeping Blasters", ret => Me.EnergyPercent >= 40)
                    ));
            }
        }
    }
}
