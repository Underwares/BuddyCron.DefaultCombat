// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using System;
using System.Collections.Generic;
using System.Reflection;
using BuddyCron;
using DefaultCombat.Routines;

namespace DefaultCombat.Core
{
    /// <summary>
    /// Builds the rotation for a character. Rotations are keyed by their declared
    /// <see cref="RotationBase.Discipline"/> id every discipline has its own stable id in
    /// <see cref="CharacterDiscipline"/>, so no type-name string lookup or mirror-class alias
    /// remapping (Rage→Fury, Focus→Concentration, …) is needed.
    /// </summary>
    public class RotationFactory
    {
        private static readonly Dictionary<CharacterDiscipline, Type> s_byDiscipline = new();

        static RotationFactory()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsAbstract || !typeof(RotationBase).IsAssignableFrom(type))
                    continue;

                var probe = (RotationBase)Activator.CreateInstance(type);
                if (probe.Discipline != CharacterDiscipline.None)
                    s_byDiscipline[probe.Discipline] = type;
            }
        }

        /// <summary>Creates the rotation registered for <paramref name="discipline"/>.
        /// Throws when the discipline is indeterminate or unsupported.</summary>
        public RotationBase Build(CharacterDiscipline discipline)
        {
            if (discipline != CharacterDiscipline.None
                && discipline != CharacterDiscipline.CanNotBeDetermined
                && s_byDiscipline.TryGetValue(discipline, out var byDiscipline))
            {
                return (RotationBase)Activator.CreateInstance(byDiscipline);
            }

            throw new InvalidOperationException($"No combat-style rotation is registered for {discipline}.");
        }

    }
}
