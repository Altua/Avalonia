using System;

namespace Avalonia.Diagnostics;

public interface IConsoleLog
{
    void WriteLine(string value);

    void WriteError(string value);

    void WriteException(Exception ex);
}


public static class ConsoleLog
{
    private static IConsoleLog? GetConsoleLog() => AvaloniaLocator.Current?.GetService<IConsoleLog>();

     public static void WriteLine(string value)
    {
        var consoleLog = GetConsoleLog();
        if (consoleLog != null)
        {
            consoleLog.WriteLine(value);
        }
        else
        {
            Console.WriteLine(value);
        }
    }

    public static void WriteError(string error)
    {
        var consoleLog = GetConsoleLog();
        if (consoleLog != null)
        {
            consoleLog.WriteError(error);
        }
        else
        {
            Console.WriteLine(error);
        }
    }

    public static void WriteException(Exception ex)
    {
        var consoleLog = GetConsoleLog();
        if (consoleLog != null)
        {
            consoleLog.WriteException(ex);
        }
        else
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }
}
