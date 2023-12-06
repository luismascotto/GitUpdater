// See https://aka.ms/new-console-template for more information
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Configuration;

var switchMappings = new Dictionary<string, string>()
           {
               { "--path", "Path" },
               { "--parallellism", "MaxDegreeOfParallelism" },
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
    Console.WriteLine($"Found {gitDirs.Count} git repositories");

    Parallel.ForEach(
        gitDirs,
        new ParallelOptions { MaxDegreeOfParallelism = int.Parse(config["MaxDegreeOfParallelism"] ?? "1") },
        gitDir =>
        {
            CliUpdateGit(gitDir).Wait();
        });
    //foreach (var gitDir in gitDirs)
    //{
    //    await CliUpdateGit(gitDir);
    //}
    //Console.ReadKey(true);
}

static async Task CliUpdateGit(string gitDir)
{
    try
    {
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
        Random.Shared.NextBytes(new byte[1]);
        Console.ForegroundColor = colorize();
        Console.WriteLine($"{gitDir} - ExitCode:{result.ExitCode} Output:{result.StandardOutput}");
        Console.WriteLine($"{result.StandardError}");
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
    values ??=
        [
            ConsoleColor.White,
            ConsoleColor.Green,
            ConsoleColor.Blue,
            ConsoleColor.Yellow,
            ConsoleColor.Gray,
            ConsoleColor.Cyan,
        ];
    return values[Random.Shared.Next(values.Count)];
}