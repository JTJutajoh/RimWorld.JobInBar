using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Verse;

namespace DarkLog
{
    /// <summary>
    /// A utility class for my mods that helps automate some debug logging stuff.
    /// Mostly just a wrapper for Verse.Log.<br />
    /// Adds a prefix in format "[packageid]" to the start of each log.<br />
    /// Expects modInst to be set at mod initialization. If not set, defaults to the assembly name.<br />
    /// Also allows setting a PrefixColor.
    /// </summary>
    [StaticConstructorOnStartup]
    static class LogPrefixed
    {
        public static Verse.Mod modInst;
        static string PackageId => modInst?.Content.PackageIdPlayerFacing ?? Assembly.GetEntryAssembly().GetName().Name;
        static string PrefixColor = "cyan";

        static string PrefixedMessage(string message) => $"<color={PrefixColor}>[{PackageId}]</color> {message}";

        static LogPrefixed()
        {
#if DEBUG
            Error("DEBUG LOGSPAM ENABLED!");
#endif
        }

        public static void Error(string text)
        {
            Log.Error(PrefixedMessage(text));
        }

        public static void ErrorOnce(string text, int key)
        {
            Log.ErrorOnce(PrefixedMessage(text), key);
        }

        public static void Warning(string text)
        {
            Log.Warning(PrefixedMessage(text));
        }

#if v1_4 || v1_5
        public static void WarningOnce(string text, int key)
        {
            Log.WarningOnce(PrefixedMessage(text), key);
        }
#endif

        public static void Message(string text)
        {
            Log.Message(PrefixedMessage(text));
        }

        /// <summary>
        /// Sends a debug message, but only if project was built with the DEBUG constant defined.
        /// Use this for logspam.
        /// </summary>
        /// <param name="warning">If true, sends as a yellow warning message for visibility.</param>
        public static void Debug(string text, bool warning=false)
        {
#if DEBUG
            if (warning)
                Warning(PrefixedMessage(text));
            else
                Message(PrefixedMessage(text));
#endif
            return;
        }
    }
}
