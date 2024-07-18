using CliWrap;
using CliWrap.Buffered;
using GitUpdaterConsole;
using Microsoft.Extensions.Configuration;

var switchMappings = new Dictionary<string, string>()
           {
               { "--path", "Path" },
               { "--parallellism", "MaxDegreeOfParallelism" },
               { "--rust", "UpdateRust" },
           };

IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args, switchMappings)
            .Build();

string _Path = config["Path"] ?? @"K:\DesenvolvimentoGit";
int _MaxDegreeOfParallelism = int.Parse(config["MaxDegreeOfParallelism"] ?? "2");
bool _UpdateRust = bool.Parse(config["UpdateRust"] ?? "false");

List<ConsoleColor>? _consoleColorList = null;
ConsoleColor _lastColor = ConsoleColor.Black;
bool _AnyErrors = false;

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
}

if (_UpdateRust)
{
    CliUpdateRust().Wait();
}

if(_AnyErrors)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("There were errors during the update process.");
    Console.ResetColor();
    //show Press enter to exit
    Console.WriteLine("\tPress to exit");
    Console.ReadLine();

}

async Task CliUpdateGit(string gitDir, int remaining = 0)
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
        _AnyErrors = _AnyErrors || result.ExitCode != 0;
        PrintCommandResult($"{gitDir} - ", result);
    }
    catch (Exception ex)
    {
        _AnyErrors = true;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"CliUpdateGit ({gitDir}) EXCEPTION: {ex}");
        Console.ResetColor();
    }

}

async Task CliUpdateRust()
{
    try
    {
        var result = await Cli.Wrap("rustup")
                .WithArguments(args => args
                    .Add("update")
                    .Add("stable")
                )
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

        _AnyErrors = _AnyErrors || result.ExitCode != 0;
        PrintCommandResult($" <-------  RUST UPDATE STABLE{Environment.NewLine}", result);
    }
    catch (Exception ex)
    {
        _AnyErrors = true;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"CliUpdateRust EXCEPTION: {ex}");
        Console.ResetColor();
    }
}

ConsoleColor Colorize()
{
    //More chance for light colors
    _consoleColorList ??=
        [
            ConsoleColor.Blue,
            ConsoleColor.Cyan,
            ConsoleColor.Gray,
            ConsoleColor.Green,
            ConsoleColor.White,
            ConsoleColor.Yellow,

            ConsoleColor.DarkBlue,
            ConsoleColor.DarkCyan,
            ConsoleColor.DarkGray,
            ConsoleColor.DarkGreen,
            ConsoleColor.DarkYellow,

            ConsoleColor.Blue,
            ConsoleColor.Cyan,
            ConsoleColor.Gray,
            ConsoleColor.Green,
            ConsoleColor.White,
            ConsoleColor.Yellow,
        ];
    var newColor = _consoleColorList[Random.Shared.Next(_consoleColorList.Count)];
    while (newColor == _lastColor)
    {
        newColor = _consoleColorList[Random.Shared.Next(_consoleColorList.Count)];
    }
    _lastColor = newColor;
    return newColor;
}

void PrintCommandResult(string title, BufferedCommandResult? result)
{
    if (result == null)
    {
        throw new ArgumentNullException(nameof(result), $"{nameof(result)} is null.");
    }

    Console.ForegroundColor = Colorize();
    if (!title.IsNullOrEmpty())
    {
        Console.Write($"{title}");
    }
    Console.WriteLine($"ExitCode: {result.ExitCode}");
    if (!result.StandardOutput.IsNullOrWhiteSpace())
    {
        Console.WriteLine($"{result.StandardOutput}");
    }
    if (!result.StandardError.IsNullOrWhiteSpace())
    {
        if (result.ExitCode != 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }
        Console.WriteLine($"{result.StandardError}");
    }
    Console.ResetColor();
}