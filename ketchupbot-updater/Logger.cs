using System.Drawing;
using Console = Colorful.Console;

namespace ketchupbot_updater;

/// <summary>
/// Log levels for logging messages
/// </summary>
public enum LogLevel
{
    Info,
    Warn,
    Error
}

/// <summary>
/// Log styles for logging messages
/// </summary>
public enum LogStyle
{
    Normal,
    Checkmark,
    Progress,
    Hang,
    Warning
}

/// <summary>
/// Logger for logging messages
/// </summary>
public static class Logger
{
    /// <summary>
    /// Log a message to the console using the specified log level and style
    /// </summary>
    /// <remarks>You should surround the call with a #if DEBUG statement to check if it is meant for debugging.</remarks>
    /// <param name="message">The message to print to the console</param>
    /// <param name="level">The log level to use</param>
    /// <param name="style">The log style to use</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void Log(string message, LogLevel level = LogLevel.Info, LogStyle? style = null)
    {
        // Set default style based on log level
        style ??= level switch
        {
            LogLevel.Error or LogLevel.Warn => LogStyle.Warning,
            _ => LogStyle.Normal
        };

        // Set style string based on style
        string styleString = style switch
        {
            LogStyle.Checkmark => @"[✔] ",
            LogStyle.Progress => "[~] ",
            LogStyle.Hang => "[...] ",
            LogStyle.Warning => "[!] ",
            _ => "[-] "
        };

        string finalMessage = styleString + message;

        switch (level)
        {
            case LogLevel.Info:
                Console.WriteLine(finalMessage);
                break;

            case LogLevel.Warn:
                Console.WriteLine(finalMessage, Color.Yellow);
                break;

            case LogLevel.Error:
                Console.Error.WriteLine(finalMessage);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    public static void LogToDiscord(string message)
    {
        throw new NotImplementedException();
    }

}