# GitUpdater

Tool to update all git repositories in a directory by running `git pull --progress -v --prune origin` in parallel.

## Prerequisites

- .NET 9.0 SDK
- Windows (targets win-x64 runtime)
- Git installed and available in PATH

## Build

```bash
dotnet restore
dotnet build --configuration Release
```

## Run

Via command line arguments:
```bash
GitUpdaterConsole.exe --path C:\myRepos --parallelism 3
```

Or configure via `appsettings.json`:
```json
{
  "Path": "C:\\myRepos",
  "MaxDegreeOfParallelism": 5,
  "PrioritySort": "prefix1,prefix2",
  "WaitAfter": false
}
```

## Configuration

All options can be set via command line or `appsettings.json` (command line overrides file settings):

- `--path` / `Path`: Directory to scan for git repositories (default: `K:\DesenvolvimentoGit`)
- `--parallelism` / `MaxDegreeOfParallelism`: Number of repositories to update concurrently (default: `2`)
- `--order` / `PrioritySort`: Comma-separated list of directory name prefixes for prioritized update order (default: none)
- `--waitAfter` / `WaitAfter`: Wait before exiting (default: `false`)
