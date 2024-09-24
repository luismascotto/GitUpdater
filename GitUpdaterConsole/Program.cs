using System.Collections.Concurrent;
using CliWrap;
using CliWrap.Buffered;
using GitUpdaterConsole;
using Microsoft.Extensions.Configuration;

var switchMappings = new Dictionary<string, string>()
           {
               { "--path", "Path" },
               { "--parallellism", "MaxDegreeOfParallelism" },
               { "--rust", "UpdateRust" },
               { "--rustOnly", "UpdateRustOnly" },
               { "--order", "PrioritySort" }
           };

IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args, switchMappings)
            .Build();

string _Path = config["Path"] ?? @"K:\DesenvolvimentoGit";
int _MaxDegreeOfParallelism = int.Parse(config["MaxDegreeOfParallelism"] ?? "2");
bool _UpdateRust = bool.Parse(config["UpdateRust"] ?? "false");
bool _UpdateRustOnly = bool.Parse(config["UpdateRustOnly"] ?? "false");
string[] _PrioritySort = config.GetAppSetting("PrioritySort", "mega,ms_mega,mits,ms_").Split(',');


Console.WriteLine($"Path: {_Path}");
Console.WriteLine($"MaxDegreeOfParallelism: {_MaxDegreeOfParallelism}");
Console.WriteLine($"UpdateRust: {_UpdateRust}");
Console.WriteLine($"UpdateRustOnly: {_UpdateRustOnly}");
Console.WriteLine($"PrioritySort: {_PrioritySort.JoinString()}");
Thread.Sleep(1000);

List<ConsoleColor>? _consoleColorList = null;
ConsoleColor _lastColor = ConsoleColor.Black;
bool _anyErrors = false;
ConcurrentBag<string> _errors = [];
if (_UpdateRustOnly && !_UpdateRust)
{
    _UpdateRust = true;
}

if (!_UpdateRustOnly && Directory.Exists(_Path))
{
    var gitDirs = new List<string>();
    foreach (var subdir in Directory.GetDirectories(_Path))
    {
        if (Directory.Exists(Path.Combine(subdir, ".git")))
        {
            gitDirs.Add(subdir);
        }
    }
    if (!_PrioritySort.SafeEmpty())
    {
        gitDirs.Sort((left, right) =>
        {
            return comparer_priority(left, right, _PrioritySort);
        });
    }

    int iCountRemaining = gitDirs.Count;
    if (iCountRemaining == 0)
    {
        Console.WriteLine("No git repositories found");
    }
    else
    {
        Console.WriteLine($"Found {gitDirs.Count} git repositories");
    }
    //var strPath = $"{Path.GetDirectoryName(Environment.ProcessPath)}{Path.DirectorySeparatorChar}firing_order_{DateTime.Now:yyyy-MM-dd}_{DateTime.Now.Ticks:X16}.txt";
    object locker = new();
    int gitDir = 0;
    Parallel.For(0, gitDirs.Count,
        new ParallelOptions { MaxDegreeOfParallelism = _MaxDegreeOfParallelism },
        (_, loopState) =>
        {
            int thisIndex;
            lock (locker)
            {
                thisIndex = gitDir++;
                //File.AppendAllText(strPath, $"{gitDirs[thisIndex]}{Environment.NewLine}");
            }
            CliUpdateGit(gitDirs[thisIndex]).Wait();
            Interlocked.Decrement(ref iCountRemaining);
            Console.WriteLine($" <-------  {iCountRemaining}     {gitDirs[thisIndex].GetLastDirectory()}{Environment.NewLine}");
        });
}

if (_UpdateRust)
{
    CliUpdateRust().Wait();
    Task.WaitAny([Task.Delay(_anyErrors ? 1 : 3000), Task.Run(Console.ReadKey)]);
}

if (_anyErrors)
{
    Console.ForegroundColor = ConsoleColor.Red;
    //Console.WriteLine("There were errors during the update process.");
    if (!_errors.IsEmpty)
    {
        Console.WriteLine("Errors in the following repositories:");
        foreach (var error in _errors)
        {
            Console.WriteLine($"\t{error}");
        }
    }
    Console.ResetColor();
    //show Press enter to exit
    Console.WriteLine("\tPress to exit");
    Console.ReadLine();
}

//////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
///
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
                    //.Add("--no-rebase")
                    .Add("origin", true)
                )
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();
        if (result.ExitCode != 0)
        {
            _errors.Add(gitDir);
            _anyErrors |= true;
        }
        PrintCommandResult($"{gitDir} - ", result);
    }
    catch (Exception ex)
    {
        _anyErrors = true;
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

        _anyErrors = _anyErrors || result.ExitCode != 0;
        PrintCommandResult($" <-------  RUST UPDATE STABLE{Environment.NewLine}", result);
    }
    catch (Exception ex)
    {
        _anyErrors = true;
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
    var newColor = _lastColor;
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

static int comparer_priority(string dirLeft, string dirRight, string[] sort_priority)
{
    if (string.IsNullOrEmpty(dirLeft))
    {
        throw new ArgumentException($"{nameof(dirLeft)} is null or empty.", nameof(dirLeft));
    }

    if (string.IsNullOrEmpty(dirRight))
    {
        throw new ArgumentException($"{nameof(dirRight)} is null or empty.", nameof(dirRight));
    }

    if ((sort_priority == null) || (sort_priority.Length == 0))
    {
        throw new ArgumentException($"{nameof(sort_priority)} is null or empty.", nameof(sort_priority));
    }

    try
    {
        string left = dirLeft.GetLastDirectory().PadRight(32)[..32];
        string right = dirRight.GetLastDirectory().PadRight(32)[..32];
        if (left != right)
        {
            int leftPriority = -1;
            int rightPriority = -1;
            for (int p = 0; p < sort_priority.Length && (leftPriority == -1 || rightPriority == -1); p++)
            {
                if (leftPriority == -1 && left.StartsWith(sort_priority[p]))
                {
                    leftPriority = p;
                }
                if (rightPriority == -1 && right.StartsWith(sort_priority[p]))
                {
                    rightPriority = p;
                }
                //Se achou uma ou outra, já testar e sair. Mesmo que ache a prioridade do outro, será mais baixa
                if (leftPriority != rightPriority)
                {
                    if (leftPriority != -1)
                    {
                        return -1;
                    }
                    return 1;
                }
            }
        }
    }
    catch (Exception)
    {
    }
    return dirLeft.CompareTo(dirRight);
}