using System.Runtime.CompilerServices;

namespace Greenthumb.Utils;

public static class Log
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LogInternal(string level, string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;

        Console.WriteLine($"[{level} ({DateTime.Now:HH:mm:ss})]: {message}");

        Console.ResetColor();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(string message)
        => LogInternal("ERROR", message, ConsoleColor.Red);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(string message)
        => LogInternal("INFO", message, ConsoleColor.Green);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warning(string message)
        => LogInternal("WARNING", message, ConsoleColor.Yellow);
}