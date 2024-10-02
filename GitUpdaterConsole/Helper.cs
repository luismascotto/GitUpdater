using CliWrap.Buffered;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GitUpdaterConsole;
public class Helper
{
    private static List<ConsoleColor>? _consoleColorList = null;
    private static ConsoleColor _lastColor = ConsoleColor.Black;

    public static ConsoleColor Colorize()
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

    public static void PrintCommandResultError(string title, BufferedCommandResult? result)
    {
        if (result == null || result.ExitCode == 0 || result.StandardError.IsNullOrWhiteSpace())
        {
            return;
        }

        if (!title.IsNullOrEmpty())
        {
            AnsiConsole.Write(new Rule($"[yellow bold underline]{title}[/]").RuleStyle("grey").LeftJustified());
            AnsiConsole.WriteLine();
        }
        AnsiConsole.WriteLine($"ExitCode: {result.ExitCode}");
        WriteErrorMessage($"{result.StandardError}");
        AnsiConsole.ResetColors();
    }

    public static void PrintCommandResult(string title, BufferedCommandResult? result)
    {
        if (result == null)
        {
            return;
            //throw new ArgumentNullException(nameof(result), $"{nameof(result)} is null.");
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

    public static int PriorityComparer(string dirLeft, string dirRight, string[] sort_priority)
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

    public static void WriteLogMessage(string message)
    {
        AnsiConsole.MarkupLine(
            "[grey]LOG:[/] " +
            message +
            "[grey]...[/]");
    }
    
    public static void WriteErrorMessage(string message)
    {
        AnsiConsole.MarkupLine(
            "[darkred]ERR: [/][red bold]" +
            message +
            "[/][grey]...[/]");
    }
}
