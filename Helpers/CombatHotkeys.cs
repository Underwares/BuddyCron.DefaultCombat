// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using System.Windows.Forms;
using System.Windows.Input;
using DefaultCombat.Core;
using Reborn.Utilities;

namespace DefaultCombat.Helpers
{
    /// <summary>Runtime rotation toggles (AoE, interrupts, charge, raid buffs, pause) bound to
    /// F4–F8/F12 hotkeys.</summary>
    public static class CombatHotkeys
    {
        /// <summary>Allow AoE abilities in the rotation.</summary>
        public static bool EnableAoe;
        /// <summary>Suspend the rotation entirely.</summary>
        public static bool PauseRotation;
        /// <summary>Allow interrupt abilities.</summary>
        public static bool EnableInterrupts;
        /// <summary>Allow gap-closer/charge abilities.</summary>
        public static bool EnableCharge;
        /// <summary>Keep raid-wide buffs applied.</summary>
        public static bool EnableRaidBuffs;

        /// <summary>Resets all toggles to their defaults and registers the hotkeys.</summary>
        public static void Initialize()
        {
            EnableAoe = true;
            PauseRotation = false;
            EnableInterrupts = true;
            EnableCharge = true;
            EnableRaidBuffs = false;

            //F9 and F10 are reservered for internal commands

            HotkeyManager.Register("Toggle RaidBuffs (F4)", Keys.F4, ModifierKeys.None, hk => ChangeRaidBuffs());
            Logger.Write("[Hot Key][F4] Toggle Raid Buffs");

            HotkeyManager.Register("Toggle Interrupts (F5)", Keys.F5, ModifierKeys.None, hk => ChangeInterrupts());
            Logger.Write("[Hot Key][F5] Toggle Interrupts");

            HotkeyManager.Register("Toggle Charge (F6)", Keys.F6, ModifierKeys.None, hk => ChangeCharge());
            Logger.Write("[Hot Key][F6] Toggle Charge");

            HotkeyManager.Register("Toggle AOE (F7)", Keys.F7, ModifierKeys.None, hk => ChangeAoe());
            Logger.Write("[Hot Key][F7] Toggle AOE");

            HotkeyManager.Register("Pause Rotation (F8)", Keys.F8, ModifierKeys.None, hk => ChangePause());
            Logger.Write("[Hot Key][F8] Pause Rotation");

            Logger.Write("[Hot Key][F9] Pause/Resume Bot");

            Logger.Write("[Hot Key][F10] Start/Stop Bot");

            HotkeyManager.Register("Set Tank (F12)", Keys.F12, ModifierKeys.None, hk => Targeting.SetTank());
            Logger.Write("[Hot Key][F12] Set Tank");
        }

        private static void ChangeAoe()
        {
            if (EnableAoe)
            {
                Logger.Write("AOE Disabled");
                EnableAoe = false;
            }
            else
            {
                Logger.Write("AOE Enabled");
                EnableAoe = true;
            }
        }

        private static void ChangePause()
        {
            if (PauseRotation)
            {
                Logger.Write("Rotation Resumed");
                PauseRotation = false;
            }
            else
            {
                Logger.Write("Rotation Paused");
                PauseRotation = true;
            }
        }

        private static void ChangeCharge()
        {
            if (EnableCharge)
            {
                Logger.Write("Charge Disabled");
                EnableCharge = false;
            }
            else
            {
                Logger.Write("Charge Enabled");
                EnableCharge = true;
            }
        }

        private static void ChangeInterrupts()
        {
            if (EnableInterrupts)
            {
                Logger.Write("Interrupts Disabled");
                EnableInterrupts = false;
            }
            else
            {
                Logger.Write("Interrupts Enabled");
                EnableInterrupts = true;
            }
        }

        private static void ChangeRaidBuffs()
        {
            if (EnableRaidBuffs)
            {
                Logger.Write("Raid Buffs Disabled");
                EnableRaidBuffs = false;
            }
            else
            {
                Logger.Write("Raid Buffs Enabled");
                EnableRaidBuffs = true;
            }
        }
    }
}
