// See https://aka.ms/new-console-template for more information
using CliWrap;
using CliWrap.Buffered;
using GitUpdaterConsole;
using Microsoft.Extensions.Configuration;

var switchMappings = new Dictionary<string, string>()
           {
               { "--path", "Path" },
               { "--parallellism", "MaxDegreeOfParallelism" },
               { "--aggressive", "Aggressive" },
           };

IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args, switchMappings)
            .Build();

if (string.IsNullOrEmpty(config["Path"]))
{
    config["Path"] = @"K:\DesenvolvimentoGit";
}

if (string.IsNullOrEmpty(config["MaxDegreeOfParallelism"]))
{
    config["MaxDegreeOfParallelism"] = "2";
}

if (string.IsNullOrEmpty(config["Aggressive"]))
{
    config["Aggressive"] = "false";
}
if (Directory.Exists(config["Path"]))
{
    var gitDirs = new List<string>();
    foreach (var subdir in Directory.GetDirectories(config["path"]!))
    {
        if (Directory.Exists(Path.Combine(subdir, ".git")))
        {
            gitDirs.Add(subdir);
            //if (gitDirs.Count > 3)
            //{
            //    break;
            //}
        }
    }
    if (gitDirs.Count == 0)
    {
        Console.WriteLine("No git repositories found");
        return;
    }
    int iCountRemaining = gitDirs.Count;
    Console.WriteLine($"Found {gitDirs.Count} git repositories");

    Parallel.ForEach(
        gitDirs,
        new ParallelOptions { MaxDegreeOfParallelism = int.Parse(config["MaxDegreeOfParallelism"] ?? "1") },
        gitDir =>
        {
            CliUpdateGit(gitDir).Wait();
            Interlocked.Decrement(ref iCountRemaining);
            Console.WriteLine($" <-------  {iCountRemaining}     {gitDir.GetLastDirectory()}{Environment.NewLine}");
        });
    //foreach (var gitDir in gitDirs)
    //{
    //    await CliUpdateGit(gitDir);
    //}
    //Console.ReadKey(true);
}

async Task CliUpdateGit(string gitDir, int remaining = 0)
{
    try
    {
        Console.ForegroundColor = colorize();
        if (bool.Parse(config["Aggressive"] ?? "false"))
        {
            var gitMaintenance = await Cli.Wrap("git")
                .WithWorkingDirectory(gitDir)
                .WithArguments(args => args
                    .Add("maintenance")
                    .Add("run")
                    .Add("--task")
                    .Add("gc")
                )
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();
            Console.WriteLine($"{gitDir} - ExitCode:{gitMaintenance.ExitCode} Output:{gitMaintenance.StandardOutput}{Environment.NewLine}{gitMaintenance.StandardError}");
        }
        var result = await Cli.Wrap("git")
                .WithWorkingDirectory(gitDir)
                .WithArguments(args => args
                    .Add("pull")
                    .Add("--progress")
                    .Add("-v")
                    .Add("--prune")
                    .Add("--no-rebase")
                    .Add("origin", true)
                )
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();
        Console.WriteLine($"{gitDir} - ExitCode:{result.ExitCode} Output:{result.StandardOutput}{Environment.NewLine}{result.StandardError}");

        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{gitDir} - Error: {ex}");
        Console.ResetColor();
    }

}

static ConsoleColor colorize()
{
    List<ConsoleColor>? values = null;
    //More chance for light colors
    values ??=
        [
            ConsoleColor.Blue,
            ConsoleColor.Cyan,
            ConsoleColor.Gray,
            ConsoleColor.Green,
            ConsoleColor.Magenta,
            ConsoleColor.Red,
            ConsoleColor.White,
            ConsoleColor.Yellow,
            ConsoleColor.DarkBlue,
            ConsoleColor.DarkCyan,
            ConsoleColor.DarkGray,
            ConsoleColor.DarkGreen,
            ConsoleColor.DarkMagenta,
            ConsoleColor.DarkRed,
            ConsoleColor.DarkYellow,
            ConsoleColor.Blue,
            ConsoleColor.Cyan,
            ConsoleColor.Gray,
            ConsoleColor.Green,
            ConsoleColor.Magenta,
            ConsoleColor.Red,
            ConsoleColor.White,
            ConsoleColor.Yellow,
        ];
    return values[Random.Shared.Next(values.Count)];
}