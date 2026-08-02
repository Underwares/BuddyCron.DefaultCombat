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
    // 7.x Assault Specialist Commando (Republic mirror of Innovative Ordnance Mercenary).
    // Cell convention: Me.EnergyPercent is the REMAINING resource (100 = full Energy Cells,
    // 0 = empty), so "starved" == low EnergyPercent. Hammer Shot is the free filler that lets
    // cells regenerate.
    /// <summary>
    ///     Commando Assault Specialist (DoT ranged dps) rotation: keeps the Incendiary Round /
    ///     Serrated Bolt DoTs at full uptime, with Mag Bolt and Full Auto between refreshes.
    /// </summary>
    public class AssaultSpecialist : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.AssaultSpecialist;

        public override string Name => "Commando Assault Specialist";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Fortification")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Tenacity", ret => Me.IsStunned),

                    //Defensives -- these are what keep a leveling character alive
                    Spell.Buff("Reactive Shield", ret => Me.HealthPercent <= 70),
                    Spell.Buff("Diversion", ret => Me.HealthPercent <= 50),            //ability-tree choice
                    Spell.Buff("Adrenaline Rush", ret => Me.HealthPercent <= 35),
                    Spell.Buff("Echoing Deterrence", ret => Me.HealthPercent <= 30),   //ability-tree choice
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15),

                    //Cells / offensive cooldowns
                    Spell.Buff("Recharge Cells", ret => Me.InCombat && Me.EnergyPercent <= 40),
                    Spell.Buff("Supercharged Cell", ret => Me.InCombat && Me.BuffCount("Supercharge") >= 10),
                    Spell.Buff("Tech Override", ret => Me.InCombat)
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

                    //Emergency cell dump -- Hammer Shot is free and lets cells regenerate
                    Spell.Cast("Hammer Shot", ret => Me.EnergyPercent <= 25),

                    //DoT upkeep (the whole point of the spec). Debuff names verified against the ability data:
                    //abl.trooper.skill.plasmatech.incendiary_round        -> "Burning (Incendiary Round)" (Debuff, 15s)
                    //abl.trooper.skill.assault_specialist.serrated_bolt   -> "Bleeding" (Debuff, 15s)
                    Spell.DoT("Incendiary Round", "Burning (Incendiary Round)", 12000),
                    Spell.DoT("Serrated Bolt", "Bleeding", 12000),

                    //Rotation
                    Spell.Cast("Assault Plastique"),                                    //ability-tree choice
                    Spell.Cast("Mag Bolt"),                                             //free/reset by the Ionic Accelerator proc
                    Spell.Cast("Electro Net", ret => Me.Target.StrongOrGreater()),
                    Spell.Cast("Full Auto"),
                    Spell.Cast("Explosive Round", ret => Me.HasBuff("Hyper Assault Rounds")),   //free instant
                    Spell.Cast("Charged Bolts", ret => Me.EnergyPercent >= 55),

                    //Never stall: free basic attack
                    Spell.Cast("Hammer Shot")
                    );
            }
        }

        public override Composite AreaOfEffect
        {
            get
            {
                return new Decorator(ret => Targeting.ShouldAoe,
                    new PrioritySelector(
                        Spell.Cast("Plasma Grenade", ret => Me.EnergyPercent >= 50),
                        Spell.CastOnGround("Mortar Volley"),
                        Spell.DoT("Incendiary Round", "Burning (Incendiary Round)", 12000),
                        Spell.DoT("Serrated Bolt", "Bleeding", 12000),
                        Spell.Cast("Explosive Round", ret => Me.HasBuff("Hyper Assault Rounds")),
                        Spell.CastOnGround("Hail of Bolts", ret => Me.EnergyPercent >= 40)
                        ));
            }
        }
    }
}
