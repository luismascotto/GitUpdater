using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using CliWrap;
using CliWrap.Buffered;
using GitUpdaterConsole;
using Microsoft.Extensions.Configuration;
using Spectre.Console.Rendering;
using Spectre.Console;
using static System.Runtime.InteropServices.JavaScript.JSType;

var switchMappings = new Dictionary<string, string>()
           {
               { "--path", "Path" },
               { "--parallellism", "MaxDegreeOfParallelism" },
               { "--order", "PrioritySort" },
               { "--waitAfter", "WaitAfter" }
           };

IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args, switchMappings)
            .Build();

string _Path = config["Path"] ?? @"K:\DesenvolvimentoGit";
int _MaxDegreeOfParallelism = int.Parse(config["MaxDegreeOfParallelism"] ?? "2");
string[] _PrioritySort = config.GetAppSetting("PrioritySort", "").Split(',');
bool _WaitAfter = bool.Parse(config["WaitAfter"] ?? "true");

const string ESC = "\u001b";

AnsiConsole.MarkupLine("[yellow]Inicializando processo de update[/]...");

Helper.WriteLogMessage($"Path: {_Path}");
Helper.WriteLogMessage($"MaxDegreeOfParallelism: {_MaxDegreeOfParallelism}");
Helper.WriteLogMessage($"PrioritySort: {_PrioritySort.JoinString()}");
Helper.WriteLogMessage($"WaitAfter: {_WaitAfter}");

if (_WaitAfter)
{
    Thread.Sleep(1000);
}

bool _anyErrors = false;
ConcurrentBag<string> _errors = [];

if (!Directory.Exists(_Path))
{
    AnsiConsole.Prompt(
        new TextPrompt<string>("[red]Path não encontrado![/] - [green]Enter to [/][red]exit[/]...")
        .AllowEmpty());
    return;
}
try
{
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
                Helper.WriteLogMessage("Nenhum repositório encontrado.");
            }
            else
            {
                Helper.WriteLogMessage($"Quantidade de repositórios encontrados: {gitDirs.Count}");
            }

            var taskList = new List<ProgressTask>();
            gitDirs.ForEach(dir =>
            {
                taskList.Add(ctx.AddTask(dir.GetLastDirectory(), autoStart: false, maxValue: 1));
            });
            var gitTask = ctx.AddTask($"GIT UPDATER {_Path}", autoStart: true, maxValue: iCountRemaining);

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
                    taskList[thisIndex].StartTask();
                    CliUpdateGit(gitDirs[thisIndex]).Wait();
                    if (!ctx.IsFinished)
                    {
                        gitTask.Increment(1);
                        taskList[thisIndex].Increment(1);
                        taskList[thisIndex].StopTask();
                    }
                    //Interlocked.Decrement(ref iCountRemaining);
                    //AnsiConsole.WriteLine($" <-------  {iCountRemaining}     {gitDirs[thisIndex].GetLastDirectory()}{Environment.NewLine}");
                });
            while (ctx.IsFinished == false)
            {
                Thread.Sleep(100);
                if (!gitTask.IsFinished)
                {
                    gitTask.StopTask();
                }
                taskList.ForEach(task =>
                {
                    if (!task.IsFinished)
                    {
                        task.StopTask();
                    }
                });
            }
        }); //CTX
}
catch (Exception ex)
{
    _anyErrors = true;
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenTypes | ExceptionFormats.ShowLinks);
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
Task.WaitAny([Task.Delay(_anyErrors || !_WaitAfter ? 1 : 3000), Task.Run(Console.ReadKey)]);

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
                .ExecuteBufferedAsync()
                .ConfigureAwait(false);
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

#pragma warning disable CS8321 // Local function is declared but never used
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
                .ExecuteBufferedAsync()
                .ConfigureAwait(false);

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
#pragma warning restore CS8321 // Local function is declared but never used

static IRenderable RenderHook(IReadOnlyList<ProgressTask> tasks, IRenderable renderable)
{
    var header = new Panel("Atualizando codebase...").Expand().RoundedBorder();
    int qtdTasks = tasks.Count.SafeDecrement();
    int qtdRestantes = tasks.Count(i => i.IsFinished == false).SafeDecrement();
    var footer = new Rows(
        new Rule(),
        new Markup(
            $"[blue]{qtdTasks}[/] tasks. [green]{qtdTasks - qtdRestantes}[/] completadas.")
    );

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
    middleContent.AddRow(renderable, new FigletText(qtdRestantes.ToString()));

    return new Rows(header, middleContent, footer, new ControlCode(escapeSequence));
}
