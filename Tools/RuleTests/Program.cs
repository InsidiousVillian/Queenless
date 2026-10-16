using System;
using System.IO;
using Queenless.Rules;

internal static class Program
{
    static int Main()
    {
        string root = FindRoot();
        string levels = Path.Combine(root, "Assets", "Levels");
        string report;
        int code = RuleTests.Run(levels, out report);
        Console.WriteLine(report);
        return code;
    }

    static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Assets", "Scripts", "Rules", "HiveRules.cs")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }
}
