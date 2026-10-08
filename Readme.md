# Board Game Framework (Task 1)

The shared core for the Gomoku and Reversi games. It comes with placeholder games that Tasks 2 and 3 replace, and with a working menu and CLI starter that Task 4 extends.

## Read in this order

1. `Task1_Briefing.docx`: the design (patterns, interfaces, how the game loop and undo behave).
2. `src/BoardGames.Gomoku/GomokuGame.cs`: the smallest complete game. Read it as the pattern for yours.
3. `tests/BoardGames.Tests/AppTests.cs`: how to test through the menu, the CLI and scripted input.

Open `BoardGames.slnx` (the .NET 10 solution format: needs a recent Visual Studio or Rider, or just use the command line).

```
dotnet build                                   build everything
dotnet test                                    run all tests (23 test methods, 50 cases)
dotnet run --project src/BoardGames.App        the interactive menu
```

## Where things are

```
src/BoardGames.Core      Task 1. Frozen.
  Board.cs        Position, Piece, Player, Board (+ BoardChanged event), GameState
  Commands.cs     IMoveCommand, MoveRecord, CommandHistory, PassCommand, PlacePieceCommand (the example)
  Game.cs         Game (the template method), GameSetup, GameOutcome and the other small game types
  GameIO.cs       IGameIO, ConsoleGameIO, ScriptedGameIO
  Input.cs        PlaceInput / PassInput / ControlInput, IInputParser, StandardInputParser
  Strategies.cs   IWinStrategy, IComputerStrategy, GameResult, FirstLegalMoveStrategy (placeholder)
  Factories.cs    IGameFactory, GameFactoryBase, GameCatalog (Composite tree)
  Views.cs        IBoardView, ConsoleBoardView, IHelpProvider, BasicHelpProvider
  Persistence.cs  GameSaveData, IGameRepository

src/BoardGames.Gomoku    Task 2. Placeholder GomokuGame and 3 factories: replace the bodies.
src/BoardGames.Reversi   Task 3. Placeholder ReversiGame and 3 factories: replace the bodies.
src/BoardGames.App       Program, MainMenu, CatalogSetup (the list of games), CliRunner (Task 4 extends)
tests/BoardGames.Tests   xUnit. Core/ = Task 1 tests (board, parser, undo and redo), AppTests.cs = menu, CLI and
                         the six placeholders, Support/TestHelpers.cs = TestIO and CommandAssert.
```

## Add a game family or a game variant (Task 2 and 3)

1. Derive from `Game` and override `Validate`, `CreateCommand(PlaceInput)`, `GetLegalMoves` (and `IsTerminal` if the default is wrong). The default `IsTerminal` is "neither player has a legal move".
2. Write one `IMoveCommand` per kind of move. `Execute` and `Undo` must be exact inverses. Describe the change as `CellChange`s (`PlacePieceCommand` shows how); the board refuses a change that does not match what is on it.
3. Write an `IWinStrategy`, and an `IHelpProvider` (reuse `BasicHelpProvider`: you only write the rules text).
4. Derive a factory from `GameFactoryBase`: `Descriptor`, `CreateBoard`, `CreateWinStrategy`, `CreateHelp`, `Assemble`. Optionally `CreateInventory`, `CreateView`, `CreateComputerStrategy`.
5. Add one `catalog.Register(...)` line in `App/CatalogSetup.cs`. No other file changes: the menu, CLI and loop pick it up.

## Game rules the Core library already handles (no need to write again)

- **PASS**: you only decide in `Validate` when a PASS is allowed (Reversi: only when `GetLegalMoves` is empty). The loop builds the `PassCommand`, and the human must type PASS (the teacher confirmed this). `GetLegalMoves` returns placements only, never PASS.
- **Undo / redo**: one undo = two commands, refused with "Nothing to undo" if fewer than two exist. A PASS counts as one of the two. A new move clears redo. Redo is not saved, so it is empty after a load, but undo works at once.
- **Drawing**: views redraw on `Game.TurnChanged`, once per move, undo, redo and load. Your view can use `game.State.Current`.
- **Scripts** hold moves and PASS only; anything else stops the run at the first invalid command.
- **Load**: `game.Replay(moves)` runs the same steps as playing, so you never rebuild state by hand.

## Game rules in Task 2 and Task 3

- **Reject the other family's codes.** Gomoku accepts only O, H, E and never PASS. Reversi accepts only P and PASS. The placeholders show the pattern.
- **Reject off-board and occupied cells** with a clear message. Never change state inside `Validate`.
- **Your computer strategy must return a move from the `legal` list.** If it returns an illegal move, the loop throws `InvalidOperationException` (it is a bug, not something the player sees).

## What must be coded in Task 2, 3 and 4

| Who    | Writes                                                                                                                                                                                                                      | Notes                                                                                                                                                                                                                                                                                       |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Task 2 | `GomokuGame`, `PlaceStoneCommand` or reuse `PlacePieceCommand`, `EraserCommand`, `FiveInARowWin`, `FogView : IBoardView`                                                                                                    | Heavy stones count toward five. Eraser only removes an opponent Ordinary stone. The Fog AI plays from the true board: its strategy gets the real `GameState`.                                                                                                                               |
| Task 3 | `ReversiGame` (flanking in `Validate`/`GetLegalMoves`), `ReversiPlaceCommand` (remember the flipped cells for `Undo`), `MajorityWin`, `MisereWin`, `CornerDominanceWin`, Smarter AI, `JsonGameRepository : IGameRepository` | Load = `catalog.Resolve(data.FamilyKey, data.VariantKey).CreateGame(new GameSetup(data.Mode, data.Level))` then `game.Replay(data.Moves)`. Compare `game.ToSaveData().BoardSnapshot` with the file's. Then pass the repository to `MainMenu` in `Program.cs`.                               |
| Task 4 | `RandomLegalMoveStrategy` (Dumb AI), help texts, `CliRunner` polish, more CLI tests, the system test                                                                                                                        | Task 1 already wrote a working starter `CliRunner` and its three tests (the CLI section of `AppTests.cs`). Add the spec's six example scripts as tests there once the real games land. `ScriptedGameIO.FromCsv(script)` runs a script; `ScriptedGameIO.Output` holds what the game printed. |

## Required test cases for each Task

| Tests                                                        | Owner  | Where                                                |
| ------------------------------------------------------------ | ------ | ---------------------------------------------------- |
| Core, menu and the integration check on the six placeholders | Task 1 | `tests/BoardGames.Tests/Core/` and `AppTests.cs`     |
| Three starter CLI test mode tests                            | Task 1 | CLI section of `AppTests.cs`                         |
| Gomoku unit and integration tests                            | Task 2 | `tests/BoardGames.Tests/Gomoku/`                     |
| Reversi unit and integration tests                           | Task 3 | `tests/BoardGames.Tests/Reversi/`                    |
| More CLI tests (the six spec scripts), the system test       | Task 4 | `AppTests.cs` CLI section, then a manual system test |

**The tests that Task 1 already covers:** `Core/` has the board and its events, the input parser, and undo, redo and PASS counting. `AppTests.cs` has the menu, the CLI (final board only, stop at the first invalid command, bad arguments) and a smoke test that the six placeholder variants start and play.

**`AppTests` will need a small edit when your real rules land.** Its smoke test plays `O1:1,O1:2` (Gomoku) and `P1:1,P1:2` (Reversi) on every variant, which only the placeholders accept. When your variant starts refusing them (for example Reversi needs a flanking move first), change the script in that test to a legal opening for your variant. That test is yours to edit.

**What the variant tests (Task 2, 3 and 4) must cover.**

Add tests under `tests/BoardGames.Tests/Gomoku/` and `.../Reversi/`, one file per area:

**Helpers:** `Support/TestHelpers.cs` has `TestIO` (interactive input: undo, redo, help; it records the prompts too) and `CommandAssert.ExecuteThenUndoRestoresState`. Core has `ScriptedGameIO.FromCsv("O1:1,O2:2")` for scripts, and its `Output` list holds everything the game printed.

## GitHub rules

- Raise a PR to merge your change into the main branch.
- Keep `dotnet build` free of warnings and `dotnet test` green before every push.
- Do not edit `src/BoardGames.Core`. If you need a change, ask the Task 1 owner and say why.
