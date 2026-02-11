using Rocket.Core.Logging;
using System;

namespace Emqo.NoNameTag.Utilities
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public enum LogCategory
    {
        Plugin,
        Permission,
        NameTag,
        DeathMessage,
        Command,
        Configuration
    }

    public static class PluginLogger
    {
        private static bool _debugEnabled = false;
        private const string Prefix = Constants.LogPrefix;
        private static readonly System.Collections.Generic.Dictionary<LogCategory, string> CategoryPrefixes = new System.Collections.Generic.Dictionary<LogCategory, string>
        {
            { LogCategory.Plugin, "[PLUGIN] " },
            { LogCategory.Permission, "[PERM] " },
            { LogCategory.NameTag, "[TAG] " },
            { LogCategory.DeathMessage, "[DEATH] " },
            { LogCategory.Command, "[CMD] " },
            { LogCategory.Configuration, "[CONFIG] " }
        };

        public static bool DebugEnabled
        {
            get => _debugEnabled;
            set => _debugEnabled = value;
        }

        public static void Log(string message, LogLevel level = LogLevel.Info, LogCategory category = LogCategory.Plugin)
        {
            var categoryPrefix = GetCategoryPrefix(category);
            switch (level)
            {
                case LogLevel.Debug:
                    if (_debugEnabled)
                        Rocket.Core.Logging.Logger.Log($"{Prefix}{categoryPrefix}[DEBUG] {message}");
                    break;
                case LogLevel.Info:
                    Rocket.Core.Logging.Logger.Log($"{Prefix}{categoryPrefix}{message}");
                    break;
                case LogLevel.Warning:
                    Rocket.Core.Logging.Logger.LogWarning($"{Prefix}{categoryPrefix}{message}");
                    break;
                case LogLevel.Error:
                    Rocket.Core.Logging.Logger.LogError($"{Prefix}{categoryPrefix}{message}");
                    break;
            }
        }

        public static void Debug(string message, LogCategory category = LogCategory.Plugin) => Log(message, LogLevel.Debug, category);
        public static void Info(string message, LogCategory category = LogCategory.Plugin) => Log(message, LogLevel.Info, category);
        public static void Warning(string message, LogCategory category = LogCategory.Plugin) => Log(message, LogLevel.Warning, category);
        public static void Error(string message, LogCategory category = LogCategory.Plugin) => Log(message, LogLevel.Error, category);

        public static void Exception(Exception ex, string context = null, LogCategory category = LogCategory.Plugin)
        {
            var message = string.IsNullOrEmpty(context)
                ? $"Exception: {ex.Message}"
                : $"{context}: {ex.Message}";

            Error(message, category);

            if (_debugEnabled && ex.StackTrace != null)
            {
                Rocket.Core.Logging.Logger.Log($"{Prefix}[STACKTRACE] {ex.StackTrace}");
            }
        }

        private static string GetCategoryPrefix(LogCategory category)
        {
            return CategoryPrefixes.TryGetValue(category, out var prefix) ? prefix : "";
        }
    }
}

