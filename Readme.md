# Board Game Framework

A .NET 10 console framework for two-player board games. Both game families are now integrated and playable:

- **Gomoku** (10 x 10): Standard, GomokuPlus (Heavy and Eraser stones) and GomokuFog (hidden opponent stones).
- **Reversi** (8 x 8): Standard, Anti-Reversi (misère) and Corner Reversi.

Each game can be played Human vs Human or Human vs Computer (Dumb or Smarter AI), saved to and loaded from a JSON file, and driven by a script in CLI test mode.

Still open: **Task 4** adds automated CLI tests for the three Gomoku spec example scripts and runs the manual system test (see [Task 4 hand-over](#task-4-hand-over-whats-left-and-how-to-test)).

## Install and build

Requirements:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Check with `dotnet --version` (it must print `10.x`).
- Git. On Windows use Windows git (Git Bash or PowerShell), not WSL git.
- Optional: a recent Visual Studio or Rider to open `BoardGames.slnx` (the .NET 10 solution format). The command line is enough.

```
git clone https://github.com/maverick001/IFN584_Assignment3.git
cd IFN584_Assignment3
dotnet build
```

`dotnet build` must finish with `0 Warning(s)` and `0 Error(s)`. No other packages need installing: `dotnet build` restores NuGet packages (xUnit) by itself.

## Run the app

Run the app from the project folder, `src/BoardGames.App`. `dotnet run` needs a folder with exactly one project file; the repository root only has the solution file, so a bare `dotnet run` there fails with "Couldn't find a project to run".

```
cd src/BoardGames.App
dotnet run
```

### Interactive menu

Running with no arguments shows the menu:

1. **New game**: pick the family (Gomoku or Reversi), the variant, then the mode (Human vs Human, or Human vs Computer with Dumb or Smarter level). In Human vs Computer the human is Player 1 (X) and moves first.
2. **Load game**: type the file name of a saved game.
3. **Quit**.

Commands during a game (case does not matter):

| Command | Meaning |
| --- | --- |
| `O5:3` | Gomoku: place an Ordinary stone at row 5, column 3 (rows and columns start at 1) |
| `H5:3` | GomokuPlus: place a Heavy stone (each player has 2) |
| `E5:3` | GomokuPlus: Eraser removes an opponent Ordinary stone at 5:3 (each player has 2) |
| `P3:4` | Reversi: place a disk at row 3, column 4 |
| `PASS` | Reversi: only allowed when you have no legal move. A human must type it; it is never played for you |
| `undo` / `redo` | Undo takes back one full turn (two commands, your move and the opponent's reply). Refused with "Nothing to undo" if fewer than two commands exist |
| `save <file>` | Save the game, for example `save game1.json` |
| `load <file>` | Load a saved game, for example `load game1.json`. A failed load keeps the current game |
| `help` | Show the rules and commands for this variant |
| `quit` | Leave the game |

Game rules: Gomoku five or more in a row wins (Heavy stones count, six or more also wins). X (Player 1) always moves first. In GomokuFog you only see opponent stones next to your own stones (all 8 neighbours); placing on a hidden occupied cell is rejected and you try again. Reversi variants differ only in the win rule: Standard (most disks wins), Anti (fewest disks wins) and Corner (first to hold three corners wins, otherwise most disks at the end).

### CLI test mode

Passing arguments plays a comma-separated script without prompts (moves and PASS only, Human vs Human). It prints the final board, then the active player, the stone or disk counts and the result (winner, draw or in progress). It stops at the first invalid command (control commands such as `undo` or `save` in a script count as invalid) and exits with 0 (whole script ran), 1 (invalid command) or 2 (bad arguments).

Run these from `src/BoardGames.App`:

```
dotnet run -- --game reversi --variant corner "P3:4,P3:3,P3:2,P2:2,P1:2,P1:1"
dotnet run -- --game gomoku --variant standard "O5:5,O1:1,O5:6,O1:2,O5:7,O1:3,O5:8,O1:4,O5:9"
```

`--game` is `gomoku` or `reversi`. `--variant` is one of:

| Game | Variant keys |
| --- | --- |
| `gomoku` | `standard`, `plus`, `fog` |
| `reversi` | `standard`, `anti`, `corner` |

GomokuFog in CLI test mode prints the board from the active player's view, even when the game has finished.

## Test

```
dotnet test                                    run all tests (250 at the time of writing)
dotnet test --filter "FullyQualifiedName~Reversi"      only tests whose name contains Reversi
dotnet test --filter "FullyQualifiedName~Cli"          only the CLI tests
```

Keep `dotnet build` free of warnings and `dotnet test` green before every push.

Where the tests live (`tests/BoardGames.Tests`, xUnit):

| Folder or file | Covers |
| --- | --- |
| `Core/` | Board and its events, input parser, command history (undo, redo, PASS counting) |
| `Gomoku/`, `Reversi/` | Rules, win strategies and Smarter AI per family |
| `Persistence/` | `JsonGameRepository`: save, load, replay and snapshot verification |
| `Integration/` | Cross-game integration: Gomoku, GomokuPlus, GomokuFog, Reversi, save and load, CLI test mode |
| `AppTests.cs` | Main menu, CLI test mode, and a smoke test that all six variants start and play |
| `Support/TestHelpers.cs` | `TestIO` (scripted interactive input that records the prompts) and `CommandAssert.ExecuteThenUndoRestoresState` |

Core also has `ScriptedGameIO.FromCsv("O1:1,O2:2")` for running a script; its `Output` list holds everything the game printed.

## Task 4 hand-over: what's left and how to test

### Still to code (test code only)

No production code is expected. The only coding is in `tests/`:

- **Add tests for the three Gomoku spec example scripts** (spec section 4.5), in `Integration/CliIntegrationTests.cs`. Use `RunCli` as the existing tests do, with these scripts:
  - standard: `O5:3,O6:3,O5:4,O6:4,O5:5,O6:5,O5:6,O6:6,O5:7` (Player 1 wins with five in a row)
  - plus: `O3:2,O3:3,O3:1,O4:3,O3:4,O5:3,E3:3,H3:3` (an Eraser removes an Ordinary stone, then a Heavy stone goes on that cell)
  - fog: `O5:5,O2:2,O5:6,O2:3,O6:5,O8:8` (the printed board is fogged from the active player's view)
- **Tighten the three existing Reversi script tests** (`The_Reversi_example_scripts_run_to_the_end_on_every_variant`). They only check exit code 0 and eight board rows. Also assert the active player, the disk counts and the result line.
- All six scripts already run correctly from the command line, so `CliRunner.cs` needs no change unless the system test finds a defect.
- Do not edit `src/BoardGames.Core`. If you need a change there, ask the Task 1 owner and say why.

Already done, so nothing to write: the `help` text for all six variants (Gomoku in `GomokuFactories.cs`, Reversi in `ReversiFactories.cs`, with the variant's win rule), the Dumb AI (`DumbGomokuStrategy` and `DumbReversiStrategy`, a random pick from the legal moves; the loop issues the Reversi PASS when there is none) and the Smarter AI. Just read each variant's `help` output during the system test (checklist item 10) and report wrong or unclear wording to the owner of that family.

### Manual system test checklist

Run `dotnet run` from `src/BoardGames.App` and check each item for all six variants unless stated.

1. **Start and navigate**: the menu offers New game, Load game and Quit; invalid menu entries show a message and re-prompt; "Back" options return to the previous menu.
2. **Modes**: Human vs Human; Human vs Computer with Dumb level; Human vs Computer with Smarter level. The computer's moves are printed and always legal.
3. **Move validation**: off-board cell, occupied cell, malformed text (`abc`, `O5`, `O5:`), wrong letter for the family (`P3:4` in Gomoku, `O5:3` in Reversi) are rejected with a clear message and the same player moves again.
4. **GomokuPlus**: each player gets exactly 2 Heavy and 2 Eraser stones; a 3rd one is refused. A Heavy stone counts toward five. An Eraser removes only an opponent Ordinary stone (not an own stone, a Heavy stone or an empty cell).
5. **GomokuFog**: opponent stones are hidden unless next to one of your stones (8 neighbours, diagonals included); placing on a hidden occupied cell is rejected and you retry; the computer plays from the true board.
6. **Reversi**: only flanking moves are accepted; flipped disks are correct in all directions; `PASS` is refused while a legal move exists and accepted when none exists; the game ends when neither player can move. Check Standard (most disks wins), Anti (fewest disks wins) and Corner (three corners wins at once, otherwise most disks at the end).
7. **Win and draw**: Gomoku five and six in a row, horizontal, vertical and both diagonals; full-board draw. The winner and the reason are displayed.
8. **Undo and redo**: `undo` takes back a full turn (the player's move and the opponent's reply, in Human vs Computer too); a Reversi PASS counts as one of the two; `undo` at the start prints "Nothing to undo"; `redo` re-applies; a new move clears redo.
9. **Save and load**: `save game1.json`, quit, start the app, choose Load game (or type `load game1.json` mid-game). The board, mode, level and whose turn it is are restored, undo works straight after the load, and redo is empty. Loading a missing file, a corrupted file or a file for another game shows an error and keeps the current game. Also load a finished game: it shows the result without starting another turn.
10. **Help and quit**: `help` shows the right rules for the variant; `quit` leaves the game cleanly.
11. **CLI test mode**: run the six spec example scripts (one per variant), plus an invalid command mid-script (the run stops there, exit code 1), an unknown game or variant, and a missing script (exit code 2). Check the printed board, active player, counts and result.

Record any defect with the exact commands typed and the output, and report it to the owner of that area (Task 1 Core, Task 2 Gomoku, Task 3 Reversi and persistence).

## Where things are

```
src/BoardGames.Core        Task 1. Frozen: do not edit.
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
tests/BoardGames.Tests     xUnit tests (see the Test section).
```

The App project references Core, Gomoku, Reversi and Persistence; the game projects never reference the App.

## Add a game family or a game variant

1. Derive from `Game` and override `Validate`, `CreateCommand(PlaceInput)`, `GetLegalMoves` (and `IsTerminal` if the default is wrong). The default `IsTerminal` is "neither player has a legal move".
2. Write one `IMoveCommand` per kind of move. `Execute` and `Undo` must be exact inverses. Describe the change as `CellChange`s (`PlacePieceCommand` shows how); the board refuses a change that does not match what is on it.
3. Write an `IWinStrategy` and an `IHelpProvider` (reuse `BasicHelpProvider`: you only write the rules text).
4. Derive a factory from `GameFactoryBase`: `Descriptor`, `CreateBoard`, `CreateWinStrategy`, `CreateHelp`, `Assemble`. Optionally `CreateInventory`, `CreateView`, `CreateComputerStrategy`.
5. Add one `catalog.Register(...)` line in `src/BoardGames.App/CatalogSetup.cs`. No other file changes: the menu, CLI and loop pick it up.

## Rules the Core library already handles

- **PASS**: a game only decides in `Validate` when a PASS is allowed (Reversi: only when `GetLegalMoves` is empty). The loop builds the `PassCommand`, and the human must type PASS. `GetLegalMoves` returns placements only, never PASS.
- **Undo and redo**: one undo = two commands, refused with "Nothing to undo" if fewer than two exist. A PASS counts as one of the two. A new move clears redo. Redo is not saved, so it is empty after a load, but undo works at once.
- **Drawing**: views redraw on `Game.TurnChanged`, once per move, undo, redo and load.
- **Scripts** hold moves and PASS only; anything else stops the run at the first invalid command.
- **Load**: `game.Replay(moves)` runs the same steps as playing, so state is never rebuilt by hand.
- **Family codes**: Gomoku accepts only O, H, E and never PASS. Reversi accepts only P and PASS.
- **Computer strategies** must return a move from the `legal` list. If one returns an illegal move, the loop throws `InvalidOperationException` (a bug, not something the player sees).

## GitHub rules

- Raise a PR to merge your change into `main`; do not push to `main` directly.
- Keep `dotnet build` free of warnings and `dotnet test` green before every push.
- Do not commit `bin/`, `obj/`, `.vs/`, `.idea/` or `.DS_Store`.
- Do not edit `src/BoardGames.Core`. If you need a change, ask the Task 1 owner and say why.
