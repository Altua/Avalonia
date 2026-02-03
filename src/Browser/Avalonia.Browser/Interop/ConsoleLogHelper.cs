using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Browser.Interop;

internal static partial class ConsoleLogHelper
{
    [JSImport("ConsoleLogHelper.writeLine", AvaloniaModule.MainModuleName)]
    public static partial void WriteLine(string value);

    [JSImport("ConsoleLogHelper.writeError", AvaloniaModule.MainModuleName)]
    public static partial void WriteError(string value);

    [JSImport("ConsoleLogHelper.groupCollapsed", AvaloniaModule.MainModuleName)]
    public static partial void GroupCollapsed(string label);

    [JSImport("ConsoleLogHelper.groupEnd", AvaloniaModule.MainModuleName)]
    public static partial void GroupEnd();
}
