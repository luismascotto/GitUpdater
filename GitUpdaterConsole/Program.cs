using System.Collections.Concurrent;
using CliWrap;
using CliWrap.Buffered;
using GitUpdaterConsole;
using Microsoft.Extensions.Configuration;
using Spectre.Console.Rendering;
using Spectre.Console;

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

AnsiConsole.MarkupLine("[yellow]Inicializando processo de update[/]...");

Helper.WriteLogMessage($"Path: {_Path}");
Helper.WriteLogMessage($"MaxDegreeOfParallelism: {_MaxDegreeOfParallelism}");
Helper.WriteLogMessage($"UpdateRust: {_UpdateRust}");
Helper.WriteLogMessage($"UpdateRustOnly: {_UpdateRustOnly}");
Helper.WriteLogMessage($"PrioritySort: {_PrioritySort.JoinString()}");

Thread.Sleep(1000);

bool _anyErrors = false;
ConcurrentBag<string> _errors = [];

_UpdateRust = _UpdateRust || _UpdateRustOnly;
if (!_UpdateRustOnly && !Directory.Exists(_Path))
{
    AnsiConsole.Prompt(
        new TextPrompt<string>("[red]Path não encontrado![/] - [green]Enter to [/][red]exit[/]...")
        .AllowEmpty());
    return;
}

// Show progress
AnsiConsole.Progress()
    .AutoClear(false)
    .Columns(
    [
        new TaskDescriptionColumn(),    // Task description
        new ProgressBarColumn(),        // Progress bar
        new PercentageColumn(),         // Percentage
        new ElapsedTimeColumn(),        // Elapsed time
        new SpinnerColumn(),            // Spinner
    ])
    .UseRenderHook((renderable, tasks) => RenderHook(tasks, renderable))
    .Start(ctx =>
    {
        var gitTask = ctx.AddTask("Git", autoStart: false).IsIndeterminate();
        var rustTask = ctx.AddTask("Rust", autoStart: true, maxValue: 1).IsIndeterminate();

        if (_UpdateRustOnly)
        {
            gitTask.StopTask();
        }
        else if (!_UpdateRust)
        {
            rustTask.StopTask();
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
                gitDirs.Sort(
                    (left, right) =>
                    {
                        return Helper.PriorityComparer(left, right, _PrioritySort);
                    });
            }

            int iCountRemaining = gitDirs.Count;
            if (iCountRemaining == 0)
            {
                Helper.WriteLogMessage("Nenhum repositório Git encontrado.");
            }
            else
            {
                Helper.WriteLogMessage($"Found {gitDirs.Count} git repositories");
            }

            gitTask.MaxValue = iCountRemaining;
            gitTask.StartTask();
            gitTask.IsIndeterminate(false);

            //var strPath = $"{Path.GetDirectoryName(Environment.ProcessPath)}{Path.DirectorySeparatorChar}firing_order_{DateTime.Now:yyyy-MM-dd}_{DateTime.Now.Ticks:X16}.txt";
            object locker = new();
            int gitDir = 0;
            Parallel.For(
                0,
                gitDirs.Count,
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
                    if (!ctx.IsFinished)
                    {
                        gitTask.Increment(1);
                    }
                    Interlocked.Decrement(ref iCountRemaining);
                    //AnsiConsole.WriteLine($" <-------  {iCountRemaining}     {gitDirs[thisIndex].GetLastDirectory()}{Environment.NewLine}");
                });
        }
        if (_UpdateRust)
        {
            rustTask.MaxValue = 1;
            rustTask.StartTask();
            rustTask.IsIndeterminate(false);
            if (!ctx.IsFinished)
            {
                CliUpdateRust().Wait();
                rustTask.Increment(1);
            }
            if (!rustTask.IsFinished)
            {
                rustTask.StopTask();
            }
        }
        while (ctx.IsFinished == false)
        {
            Thread.Sleep(100);
            if (!gitTask.IsFinished)
            {
                gitTask.StopTask();
            }
            if (!rustTask.IsFinished)
            {
                rustTask.StopTask();
            }
        }
    }); //CTX
if (_UpdateRust)
{
    Task.WaitAny([Task.Delay(_anyErrors ? 1 : 3000), Task.Run(Console.ReadKey)]);
}

if (_anyErrors)
{
    //Console.WriteLine("There were errors during the update process.");
    if (!_errors.IsEmpty)
    {
        AnsiConsole.WriteLine("Erros nos seguintes repositórios:");
        foreach (var error in _errors)
        {
            Helper.WriteErrorMessage(error);
        }
    }
    AnsiConsole.Prompt(
        new TextPrompt<string>("[green]Enter to [/][red]exit[/]...")
        .AllowEmpty());
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
        Helper.PrintCommandResultError($"{gitDir}", result);
    }
    catch (Exception ex)
    {
        _anyErrors = true;
        AnsiConsole.WriteLine($"CliUpdateGit ({gitDir}) EXCEPTION:");
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenTypes | ExceptionFormats.ShowLinks);
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
        Helper.PrintCommandResult($" <-------  RUST UPDATE STABLE{Environment.NewLine}", result);
    }
    catch (Exception ex)
    {
        _anyErrors = true;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"CliUpdateRust EXCEPTION: {ex}");
        Console.ResetColor();
    }
}

static IRenderable RenderHook(IReadOnlyList<ProgressTask> tasks, IRenderable renderable)
{
    var header = new Panel("Atualizando codebase...").Expand().RoundedBorder();
    var footer = new Rows(
        new Rule(),
        new Markup(
            $"[blue]{tasks.Count}[/] tasks. [green]{tasks.Count(i => i.IsFinished)}[/] completadas.")
    );

    const string ESC = "\u001b";
    string escapeSequence;
    if (tasks.All(i => i.IsFinished))
    {
        escapeSequence = $"{ESC}]]9;4;0;100{ESC}\\";
    }
    else
    {
        var total = tasks.Sum(i => i.MaxValue);
        var done = tasks.Sum(i => i.Value);
        var percent = (int)(done / total * 100);
        escapeSequence = $"{ESC}]]9;4;1;{percent}{ESC}\\";
    }

    var middleContent = new Grid().AddColumns(new GridColumn(), new GridColumn().Width(20));
    middleContent.AddRow(renderable, new FigletText(tasks.Count(i => i.IsFinished == false).ToString()));

    return new Rows(header, middleContent, footer, new ControlCode(escapeSequence));
}


/////