using CliWrap;
using CliWrap.Buffered;
using GitUpdaterConsole;
using Microsoft.Extensions.Configuration;

var switchMappings = new Dictionary<string, string>()
           {
               { "--path", "Path" },
               { "--parallellism", "MaxDegreeOfParallelism" },
               //{ "--aggressive", "Aggressive" },
               { "--rust", "UpdateRust" },
           };

IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args, switchMappings)
            .Build();

string _Path = config["Path"] ?? @"K:\DesenvolvimentoGit";
int _MaxDegreeOfParallelism = int.Parse(config["MaxDegreeOfParallelism"] ?? "2");
bool _Aggressive = bool.Parse(config["Aggressive"] ?? "false");
bool _UpdateRust = bool.Parse(config["UpdateRust"] ?? "false");



if (Directory.Exists(_Path))
{
    var gitDirs = new List<string>();
    foreach (var subdir in Directory.GetDirectories(_Path))
    {
        if (Directory.Exists(Path.Combine(subdir, ".git")))
        {
            gitDirs.Add(subdir);
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
        new ParallelOptions { MaxDegreeOfParallelism = _MaxDegreeOfParallelism },
        gitDir =>
        {
            CliUpdateGit(gitDir).Wait();
            Interlocked.Decrement(ref iCountRemaining);
            Console.WriteLine($" <-------  {iCountRemaining}     {gitDir.GetLastDirectory()}{Environment.NewLine}");
        });

    if (_UpdateRust)
    {
        CliUpdateRust().Wait();
    }
}

async Task CliUpdateGit(string gitDir, int remaining = 0)
{
    try
    {
        Console.ForegroundColor = colorize();
        //if (bool.Parse(config["Aggressive"] ?? "false"))
        //{
        //    var gitMaintenance = await Cli.Wrap("git")
        //        .WithWorkingDirectory(gitDir)
        //        .WithArguments(args => args
        //            .Add("maintenance")
        //            .Add("run")
        //            .Add("--task")
        //            .Add("gc")
        //        )
        //        .WithValidation(CommandResultValidation.None)
        //        .ExecuteBufferedAsync();
        //    Console.WriteLine($"{gitDir} - ExitCode:{gitMaintenance.ExitCode} Output:{gitMaintenance.StandardOutput}{Environment.NewLine}{gitMaintenance.StandardError}");
        //}
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

async Task CliUpdateRust()
{
    try
    {
        Console.ForegroundColor = colorize();
        var result = await Cli.Wrap("rustup")
                .WithArguments(args => args
                    .Add("update")
                    .Add("stable")
                )
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();
        Console.WriteLine($"ExitCode:{result.ExitCode} Output:{result.StandardOutput}{Environment.NewLine}{result.StandardError}");

        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {ex}");
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
            //ConsoleColor.Red,
            ConsoleColor.White,
            ConsoleColor.Yellow,
            ConsoleColor.DarkBlue,
            ConsoleColor.Blue,
            ConsoleColor.DarkCyan,
            ConsoleColor.DarkGray,
            ConsoleColor.DarkGreen,
            //ConsoleColor.DarkMagenta,
            ConsoleColor.DarkRed,
            ConsoleColor.DarkYellow,
            ConsoleColor.Blue,
            ConsoleColor.Cyan,
            ConsoleColor.Gray,
            ConsoleColor.Green,
            //ConsoleColor.Magenta,
            //ConsoleColor.Red,
            ConsoleColor.White,
            ConsoleColor.Yellow,
        ];
    return values[Random.Shared.Next(values.Count)];
}