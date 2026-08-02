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
//using DefaultCombat.Extensions; ((Hold off for now))

namespace DefaultCombat.Routines
{
    // 7.x Combat Medic Commando (healer, Republic mirror of Bodyguard Mercenary).
    // Cell convention: Me.EnergyPercent is the REMAINING resource (100 = full Energy Cells,
    // 0 = empty), so "starved" == low EnergyPercent. Med Shot is the free filler heal that lets
    // cells regenerate and builds Supercharge.
    // Healing lives in AreaOfEffect (the composite the framework runs before SingleTarget);
    // SingleTarget is the "nobody needs healing" dps filler.
    /// <summary>
    ///     Commando Combat Medic (healer) rotation: Trauma Probe upkeep and a Bacta Infusion /
    ///     Advanced Medical Probe triage priority, with a dps filler for solo play.
    /// </summary>
    public class CombatMedic : RotationBase
    {
        public override CharacterDiscipline Discipline => CharacterDiscipline.CombatMedic;

        public override string Name => "Commando Combat Medic";

        public override Composite Buffs => new PrioritySelector(
            Spell.Buff("Fortification")
        );

        public override Composite Cooldowns
        {
            get
            {
                return new PrioritySelector(
                    Spell.Buff("Tenacity", ret => Me.IsStunned),

                    //Spend 10 stacks of Supercharge when the heal target actually needs it. Combat Medic's
                    //spender is "Supercharged Kolto Cell" (abl.trooper.skill.combat_medic.supercharged_cell_support);
                    //plain "Supercharged Cell" is the Assault Specialist one and never matches here.
                    Spell.Buff("Supercharged Kolto Cell",
                        ret => Me.InCombat && Me.BuffCount("Supercharge") >= 10
                               && HealTarget != null && HealTarget.HealthPercent <= 85),

                    //Cells
                    Spell.Buff("Recharge Cells", ret => Me.InCombat && Me.EnergyPercent <= 40),

                    //Defensives -- these are what keep a leveling character alive
                    Spell.Buff("Reactive Shield", ret => Me.HealthPercent <= 60),
                    Spell.Buff("Diversion", ret => Me.HealthPercent <= 50),            //ability-tree choice
                    Spell.Buff("Adrenaline Rush", ret => Me.HealthPercent <= 35),
                    Spell.Buff("Echoing Deterrence", ret => Me.HealthPercent <= 25),   //ability-tree choice
                    Spell.Buff("Unity", ret => Me.Companion != null && Me.HealthPercent <= 15)
                    );
            }
        }

        /// <summary>
        ///     Dps filler for solo/leveling; only reached when nothing needs healing.
        /// </summary>
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

                    //Filler damage for solo/leveling -- only reached when nothing needs healing
                    Spell.Cast("High Impact Bolt", ret => Me.EnergyPercent >= 55),
                    Spell.Cast("Charged Bolts", ret => Me.EnergyPercent >= 70),

                    //Never stall: free basic attack
                    Spell.Cast("Hammer Shot")
                    );
            }
        }

        /// <summary>
        ///     The healing priority. Runs from the AoE slot, which the framework evaluates before
        ///     <see cref="SingleTarget"/>, so healing always pre-empts the dps filler.
        /// </summary>
        public override Composite AreaOfEffect
        {
            get
            {
                return new PrioritySelector(

                        //Cleanse
                        //Spell.Cast("Field Aid", ret => HealTarget.ShouldDispel()), ((New Code Hold off for now))
                        Spell.Cleanse("Field Aid"),

                        //Keep Trauma Probe rolling on whoever we are healing
                        Spell.Heal("Trauma Probe", on => HealTarget, 100, ret => !HealTarget.HasBuff("Trauma Probe")),

                        //Bacta Infusion is instant and free, so use it on cooldown. It procs
                        //Emergency Response, which makes the next Advanced Medical Probe instant.
                        Spell.Heal("Bacta Infusion", 90, ret => Me.InCombat),

                        //Procs worth reacting to
                        Spell.Heal("Advanced Medical Probe", 95, ret => Me.HasBuff("Emergency Response") && Me.InCombat),
                        //Combat Medic's spender is the ability "Supercharged Kolto Cell"; the aura it grants
                        //may be named either way, so accept both rather than silently miss the window.
                        Spell.Heal("Advanced Medical Probe", 95,
                            ret => (Me.HasBuff("Supercharged Kolto Cell") || Me.HasBuff("Supercharged Cell")) && Me.InCombat),

                        //AoE heal / Kolto Residue upkeep.
                        //"Kolto Residue" is only the PASSIVE'S name (abl.trooper.skill.combat_medic.kolto_residue);
                        //the aura it actually lands on the ally is "Invigorated" (Buff, 45s) -- verified against the ability data.
                        new Decorator(ctx => HealTarget != null && Targeting.ShouldAoeHeal,
                            Spell.CastOnGround("Kolto Bomb", on => HealTarget.Location, ret => Me.InCombat)),
                        new Decorator(ctx => HealTarget != null,
                            Spell.CastOnGround("Kolto Bomb", on => HealTarget.Location,
                                ret => Me.InCombat && HealTarget.HealthPercent < 90 && !HealTarget.HasMyBuff("Invigorated"))),

                        //Single target priority. Field Triage stacks (from Medical Probe, max 3)
                        //discount Advanced Medical Probe -- spend them when they are capped.
                        Spell.Heal("Advanced Medical Probe", 90, ret => Me.BuffCount("Field Triage") >= 3 || Me.Level < 40),
                        Spell.Heal("Successive Treatment", 90, ret => !Me.IsMoving),    //channelled, so stand still
                        Spell.Heal("Medical Probe", 88),                                //builds Field Triage stacks
                        Spell.Heal("Advanced Medical Probe", 65),                       //expensive panic heal

                        //Filler -- free, instant, regenerates cells and builds Supercharge
                        Spell.Heal("Med Shot", 95)
                        );
            }
        }
    }
}
