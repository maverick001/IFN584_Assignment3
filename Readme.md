# Board Game Framework

A .NET 10 console framework for two-player board games. Both families are integrated and playable:

- **Gomoku** (10 x 10): Standard, GomokuPlus (Heavy and Eraser stones) and GomokuFog (hidden opponent stones).
- **Reversi** (8 x 8): Standard, Anti-Reversi (misère) and Corner Reversi.

Each game can be played Human vs Human or Human vs Computer (Dumb or Smarter AI), saved to and loaded from a JSON file, and driven by a script in CLI test mode.

## Task 4: what is left

Everything is built. Task 4 has two jobs, and neither changes the game code.

### 1. Write tests (in `tests/BoardGames.Tests/Integration/CliIntegrationTests.cs`)

- **Add one test for each Gomoku spec script** (spec section 4.5). Copy the style of the existing Reversi script test and use `RunCli`:

  | Variant | Script | What to check |
  | --- | --- | --- |
  | standard | `O5:3,O6:3,O5:4,O6:4,O5:5,O6:5,O5:6,O6:6,O5:7` | Player 1 wins with five in a row |
  | plus | `O3:2,O3:3,O3:1,O4:3,O3:4,O5:3,E3:3,H3:3` | An Eraser removes an Ordinary stone, then a Heavy stone goes on that cell |
  | fog | `O5:5,O2:2,O5:6,O2:3,O6:5,O8:8` | The printed board is fogged from the active player's view |

- **Tighten the 3 Reversi script tests** (`The_Reversi_example_scripts_run_to_the_end_on_every_variant`). They only check exit code 0 and 8 board rows. Also check the active player, the disk counts and the result line.
- `CliRunner.cs` needs no change unless the manual test below finds a bug.

### 2. Play-test the app by hand

Run `dotnet run` from `src/BoardGames.App`. Check each item for all six variants unless stated.

1. **Menu**: New game, Load game, Quit. A wrong entry shows a message and asks again; "Back" returns to the previous menu.
2. **Modes**: Human vs Human, and Human vs Computer at Dumb and Smarter. The computer's moves are printed and always legal.
3. **Bad input**: an off-board cell, an occupied cell, text like `abc`, `O5` or `O5:`, and the wrong letter for the game (`P3:4` in Gomoku, `O5:3` in Reversi) are refused with a clear message, and the same player moves again.
4. **GomokuPlus**: each player has exactly 2 Heavy and 2 Eraser stones, and a 3rd is refused. A Heavy stone counts toward five. An Eraser only removes an opponent's Ordinary stone (not your own, not a Heavy, not an empty cell).
5. **GomokuFog**: you see an opponent stone only next to one of your stones (8 neighbours, diagonals included). Placing on a hidden occupied cell is refused and you retry. The computer plays from the true board.
6. **Reversi**: only flanking moves are accepted and the flips are right in all directions. `PASS` is refused while you have a legal move and accepted when you have none. The game ends when neither player can move. Check the Standard (most disks), Anti (fewest disks) and Corner (3 corners wins at once, otherwise most disks) results.
7. **Win and draw**: Gomoku with five and six in a row (horizontal, vertical, both diagonals), and a full-board draw. The winner and the reason are shown.
8. **Undo and redo**: `undo` takes back a full turn (your move and the reply, also in Human vs Computer). A Reversi PASS counts as one of the two moves. `undo` at the start prints "Nothing to undo". `redo` re-applies. A new move clears redo.
9. **Save and load**: `save game1.json`, quit, restart, choose Load game (or type `load game1.json` mid-game). Board, mode, level and whose turn it is come back, undo works at once, and redo is empty. A missing file, a corrupted file or a file for another game shows an error and keeps the current game. A finished game loads and shows its result without starting another turn.
10. **Help and quit**: `help` shows the right rules for the variant. Read the wording of all six and report anything wrong or unclear. `quit` leaves cleanly.
11. **CLI test mode**: run the six spec scripts, plus an invalid command mid-script (the run stops there, exit code 1), an unknown game or variant, and a missing script (exit code 2). Check the board, active player, counts and result.

Report each bug to the owner of that area (Task 1 Core, Task 2 Gomoku, Task 3 Reversi and persistence). Send the exact commands you typed and the output.

### Rules for you

- Do not edit `src/BoardGames.Core`. If you need a change there, ask the Task 1 owner and say why.
- Work on a branch and open a PR into `main`.
- Before you push, `dotnet build` shows 0 warnings and `dotnet test` passes.

## Install and build

Requirements:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Check with `dotnet --version` (it must print `10.x`).
- Git. On Windows use Windows git (Git Bash or PowerShell), not WSL git.
- Optional: a recent Visual Studio or Rider to open `BoardGames.slnx`. The command line is enough.

```
git clone https://github.com/maverick001/IFN584_Assignment3.git
cd IFN584_Assignment3
dotnet build
```

`dotnet build` must finish with `0 Warning(s)` and `0 Error(s)`. It restores the NuGet packages (xUnit) by itself.

## Run the app

Run from `src/BoardGames.App`. A bare `dotnet run` in the repository root fails with "Couldn't find a project to run", because the root only has the solution file.

```
cd src/BoardGames.App
dotnet run
```

### Interactive menu

1. **New game**: pick the family and variant, then the mode (Human vs Human, or Human vs Computer with Dumb or Smarter level). In Human vs Computer the human is Player 1 (X) and moves first.
2. **Load game**: type the file name of a saved game.
3. **Quit**.

Commands during a game (case does not matter; rows and columns start at 1):

| Command | Meaning |
| --- | --- |
| `O5:3` | Gomoku: place an Ordinary stone at row 5, column 3 |
| `H5:3` | GomokuPlus: place a Heavy stone (2 per player) |
| `E5:3` | GomokuPlus: Eraser removes an opponent Ordinary stone at 5:3 (2 per player) |
| `P3:4` | Reversi: place a disk at row 3, column 4 |
| `PASS` | Reversi: only when you have no legal move. A human must type it; it is never played for you |
| `undo` / `redo` | Undo takes back one full turn (your move and the opponent's reply). Refused with "Nothing to undo" if fewer than two moves exist |
| `save <file>` | Save the game, for example `save game1.json` |
| `load <file>` | Load a saved game. A failed load keeps the current game |
| `help` | Show the rules and commands for this variant |
| `quit` | Leave the game |

Rules in short: X (Player 1) moves first. In Gomoku, five or more in a row wins (Heavy stones count). In GomokuFog you only see opponent stones next to your own stones (all 8 neighbours); placing on a hidden occupied cell is refused. The Reversi variants differ only in the win rule: Standard (most disks), Anti (fewest disks), Corner (first to hold three corners, otherwise most disks).

### CLI test mode

Arguments play a comma-separated script without prompts (moves and PASS only, Human vs Human). The program prints the final board, the active player, the stone or disk counts and the result. It stops at the first invalid command (`undo`, `save` and other control commands count as invalid). Exit code: 0 whole script ran, 1 invalid command, 2 bad arguments.

```
dotnet run -- --game reversi --variant corner "P3:4,P3:3,P3:2,P2:2,P1:2,P1:1"
dotnet run -- --game gomoku --variant standard "O5:5,O1:1,O5:6,O1:2,O5:7,O1:3,O5:8,O1:4,O5:9"
```

`--game` is `gomoku` or `reversi`. `--variant` is `standard`, `plus` or `fog` for Gomoku, and `standard`, `anti` or `corner` for Reversi. GomokuFog prints the active player's view, even when the game has finished.

## Test

```
dotnet test                                          run all tests (250 at the time of writing)
dotnet test --filter "FullyQualifiedName~Reversi"    only tests whose name contains Reversi
dotnet test --filter "FullyQualifiedName~Cli"        only the CLI tests
```

Tests are in `tests/BoardGames.Tests` (xUnit):

| Folder or file | Covers |
| --- | --- |
| `Core/` | Board, input parser, command history (undo, redo, PASS counting) |
| `Gomoku/`, `Reversi/` | Rules, win strategies, Dumb and Smarter AI |
| `Persistence/` | `JsonGameRepository`: save, load, replay, snapshot check |
| `Integration/` | Cross-game tests: Gomoku, GomokuPlus, GomokuFog, Reversi, save and load, CLI test mode |
| `AppTests.cs` | Main menu, CLI test mode, and a smoke test that all six variants start and play |
| `Support/TestHelpers.cs` | `TestIO` (scripted interactive input) and `CommandAssert.ExecuteThenUndoRestoresState` |

`ScriptedGameIO.FromCsv("O1:1,O2:2")` in Core runs a script; its `Output` list holds everything the game printed.

## Where things are

```
src/BoardGames.Core        Task 1. Do not edit.
  Board.cs        Position, Piece, Player, Board (+ BoardChanged event), GameState
  Commands.cs     IMoveCommand, MoveRecord, CommandHistory, PassCommand, PlacePieceCommand
  Game.cs         Game (the template method), GameSetup, GameOutcome and the other small game types
  GameIO.cs       IGameIO, ConsoleGameIO, ScriptedGameIO
  Input.cs        PlaceInput / PassInput / ControlInput, IInputParser, StandardInputParser
  Strategies.cs   IWinStrategy, IComputerStrategy, GameResult, FirstLegalMoveStrategy (Core's default, unused by the two games)
  Factories.cs    IGameFactory, GameFactoryBase, GameCatalog (Composite tree)
  Views.cs        IBoardView, ConsoleBoardView, IHelpProvider, BasicHelpProvider
  Persistence.cs  GameSaveData, IGameRepository

src/BoardGames.Gomoku      Task 2. GomokuGame, EraserCommand, FiveInARowWin, FogView, DumbGomokuStrategy, SmarterGomokuStrategy, 3 factories.
src/BoardGames.Reversi     Task 3. ReversiGame, ReversiPlaceCommand, ReversiRules, win strategies, DumbReversiStrategy, SmarterReversiStrategy, 3 factories.
src/BoardGames.Persistence Task 3. JsonGameRepository: replay-based loading with snapshot verification.
src/BoardGames.App         Program, MainMenu, CatalogSetup (the list of games), CliRunner (CLI test mode).
tests/BoardGames.Tests     xUnit tests.
```

The App project references Core, Gomoku, Reversi and Persistence; the game projects never reference the App.

## Add a game family or a game variant

1. Derive from `Game` and override `Validate`, `CreateCommand(PlaceInput)`, `GetLegalMoves` (and `IsTerminal` if the default is wrong). The default `IsTerminal` is "neither player has a legal move".
2. Write one `IMoveCommand` per kind of move. `Execute` and `Undo` must be exact inverses. Describe the change as `CellChange`s (`PlacePieceCommand` shows how); the board refuses a change that does not match what is on it.
3. Write an `IWinStrategy` and an `IHelpProvider` (reuse `BasicHelpProvider`: you only write the rules text).
4. Derive a factory from `GameFactoryBase`: `Descriptor`, `CreateBoard`, `CreateWinStrategy`, `CreateHelp`, `Assemble`. Optionally `CreateInventory`, `CreateView`, `CreateComputerStrategy`.
5. Add one `catalog.Register(...)` line in `src/BoardGames.App/CatalogSetup.cs`. No other file changes: the menu, CLI and loop pick it up.

## GitHub rules

- Raise a PR to merge into `main`; do not push to `main` directly.
- Keep `dotnet build` free of warnings and `dotnet test` green before every push.
- Do not commit `bin/`, `obj/`, `.vs/`, `.idea/` or `.DS_Store`.
- Do not edit `src/BoardGames.Core`. If you need a change, ask the Task 1 owner and say why.
