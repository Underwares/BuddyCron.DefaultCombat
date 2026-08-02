// Copyright (C) 2011-2018 Bossland GmbH
// See the file LICENSE for the source code's detailed license

using Reborn.Utilities;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace DefaultCombat
{
    /// <summary>Writes routine log messages with a "[DefaultCombat]" prefix.</summary>
    public static class Logger
    {
        /// <summary>Logs <paramref name="message"/> in green.</summary>
        public static void Write(string message)
        {
            Write(Colors.Green, message);
        }

        /// <summary>Logs a formatted message in green.</summary>
        public static void Write(string message, params object[] args)
        {
            Write(Colors.Green, message, args);
        }

        /// <summary>Logs a formatted message in the given color.</summary>
        public static void Write(Color clr, string message, params object[] args)
        {
            Logging.Write(clr, "[DefaultCombat] " + message, args);
        }
    }
}
