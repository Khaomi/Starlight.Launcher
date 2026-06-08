using System.Runtime.InteropServices;

public static class ConsoleHelper
{
    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();

    public static void CreateConsole() => AllocConsole();
}
