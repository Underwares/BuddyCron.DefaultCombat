// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using System.Diagnostics;
using BuddyCron;
using Reborn.Behaviors.Treesharp;
using Reborn.Utilities;
using Action = Reborn.Behaviors.Treesharp.Action;

namespace DefaultCombat.Helpers
{
    /// <summary>A consumable inventory item (matched by name substring) with a reuse cooldown,
    /// usable from behavior trees.</summary>
    public class InventoryManagerItem
    {
        /// <summary>Selects a value from the behavior-tree context.</summary>
        public delegate T Selection<out T>(object context);

        private readonly Stopwatch _timer;
        /// <summary>Reuse cooldown in seconds.</summary>
        public int CoolDown;
        /// <summary>Substring the inventory item's name must contain.</summary>
        public string ItemName;

        /// <summary>Tracks the item matched by <paramref name="name"/> with a
        /// <paramref name="cd"/>-second reuse cooldown.</summary>
        public InventoryManagerItem(string name, int cd)
        {
            ItemName = name;
            CoolDown = cd;
            _timer = new Stopwatch();
            Logging.WriteDiagnostic(name + "  Created!");
        }

        /// <summary>Composite that uses the first backpack item whose name contains
        /// <see cref="ItemName"/> when <paramref name="reqs"/> passes and the cooldown has elapsed;
        /// fails when unavailable or on cooldown.</summary>
        public Composite UseItem(Selection<bool> reqs = null)
        {
            return new Decorator(ret => reqs == null || reqs(ret),
                new Action(delegate
                {
                    if (_timer.IsRunning && _timer.Elapsed.TotalSeconds < CoolDown)
                        return RunStatus.Failure;

                    if (!_timer.IsRunning)
                        _timer.Start();

                    foreach (var o in Core.Player.Backpack)
                    {
                        if (o.Name.Contains(ItemName))
                        {
                            o.Use();
                            _timer.Stop();
                            _timer.Reset();
                            _timer.Start();
                            return RunStatus.Success;
                        }
                    }
                    return RunStatus.Failure;
                }));
        }
    }
}
