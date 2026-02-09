using System;
using Avalonia.Browser.Interop;
using Avalonia.Controls;
using Avalonia.Diagnostics;

namespace Avalonia.Browser
{
    internal class ConsoleLogImpl : IConsoleLog
    {
        public void WriteLine(string value)
        {
            ConsoleLogHelper.WriteLine(value);
        }

        public void WriteError(string value)
        {
            ConsoleLogHelper.WriteError(value);
        }

        public void WriteException(Exception ex)
        {
            ConsoleLogHelper.GroupCollapsed("[ERROR] " + ex.Message);
            ex.StackTrace?
                .Split([Environment.NewLine], StringSplitOptions.None)
                .Do(line => ConsoleLogHelper.WriteError(line.Trim()));
            ConsoleLogHelper.GroupEnd();
        }
    }
}
