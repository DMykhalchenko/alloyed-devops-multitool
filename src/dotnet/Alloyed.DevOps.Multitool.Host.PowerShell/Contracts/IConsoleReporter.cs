namespace Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;

public interface IConsoleReporter
{
    void WriteHeader(string title);

    void WriteMessage(ConsoleMessageLevel level, string message);

    void WriteKeyValue(string key, string value);
}

public enum ConsoleMessageLevel
{
    Info = 0,
    Warning = 1,
    Error = 2,
}

