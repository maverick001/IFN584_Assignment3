# Task 3 implementation handover

Branch: `task3-reversi-ai-persistence`.

## Implemented

| Area | Concrete implementation | Verification |
| --- | --- | --- |
| Standard, Anti and Corner Reversi | `ReversiGame`, `ReversiRules`, three existing factories | Opening, eight-direction captures, pass, full games, objective tests |
| Reversi undoable move | `ReversiPlaceCommand : IMoveCommand` | Exact inverse, batched events, multiple full turns, HvC undo/redo, branching |
| Reversi Smarter AI | `SmarterReversiStrategy : IComputerStrategy` | Maximum/minimum flips, corner preference, third-corner win and defence, nine full HvC games |
| Gomoku Smarter AI | `SmarterGomokuStrategy : IComputerStrategy` | Four directions, heavy stones, gap blocking, win priority, legal Eraser simulation |
| Save/load | `BoardGames.Persistence.JsonGameRepository : IGameRepository` | Twenty-four setup round trips, menu integration, PASS replay, corruption handling, immediate undo |
| Design/report | `Task3_Report.md`, two SVG diagrams and their Mermaid sources | Object snapshot matches the CLI board; diagrams reference implemented classes |

`src/BoardGames.Core` is unchanged. App integration adds a Persistence project reference, injects the repository in `Program.cs`, and displays an already completed save's board and result in `MainMenu` without starting another turn. `GomokuFactories.cs` has a small AI selection override only; Task 2's rules and views remain untouched. `AppTests.cs` now uses legal Reversi opening moves rather than the placeholder's arbitrary placements.

## Commands

Run these in the repository root:

```powershell
dotnet build BoardGames.slnx -warnaserror
dotnet test BoardGames.slnx --no-build
dotnet run --project src/BoardGames.App
dotnet run --project src/BoardGames.App -- --game reversi --variant standard "P3:4,P3:3,P4:3,P5:3,P4:2"
dotnet run --project src/BoardGames.App -- --game reversi --variant anti "P4:3,P3:3,P3:4,P5:3,P6:3"
dotnet run --project src/BoardGames.App -- --game reversi --variant corner "P3:4,P3:3,P3:2,P2:2,P1:2,P1:1"
```

For a save/load demonstration, select Standard Reversi / HvH, enter `P3:4` then `P3:3`, use `save reversi-demo.json`, quit and restart. Choose Load game, enter the path, then use `undo`, `redo` and `quit`. A loaded game's redo stack begins empty; an undo after loading creates redo normally. Ignore the demo save when packaging the final source archive.

## Remaining team integration

### Task 1 owner

- Add `BoardGames.Persistence` to the final solution/class diagram and show `JsonGameRepository` realising `IGameRepository`.
- Include the new command, rules helper, three win strategies and the two Smarter strategy classes in the final class diagram.
- Retain the completed-save check in `MainMenu.PlayLoop`. `Game.Replay` restores `Game.Result`; the menu displays finished games without entering Core's turn loop. This fixes completed-save loading while keeping Core frozen.

### Task 2 owner

- Keep the Smarter AI override when replacing the Gomoku factories. Legal move lists must include only available `O`, `H` and `E` moves.
- Heavy and ordinary stones both count toward five. The strategy reads ownership, not rendered symbols.
- The strategy accepts the true board in Fog, as stated in the Task 1 handover.
- Implement every move's `ToRecord`; ensure Eraser execute/undo update the inventory and erased cell symmetrically. Replay then reconstructs inventory automatically.
- Once the real rules land, run integration tests for Heavy/Eraser save/load, immediate undo after load and both players' Fog views. Current AI tests simulate a legal Eraser input; they do not claim that Task 2's gameplay is implemented.
- If the Fog view contains memory beyond a deterministic projection of the board or replayed history, discuss extending the save contract with Task 1. The current contract reconstructs views by replay and does not store an additional perspective DTO.

### Task 4 owner

- Dumb currently retains Task 1's `FirstLegalMoveStrategy` placeholder. Integrate the random strategy through the two base factories without replacing their `AiLevel.Smarter` branches.
- Reversi factories now provide working rules text through `BasicHelpProvider`; refine it as needed for the final help content.
- The three Reversi assignment scripts have been run manually; add or retain final CLI regression tests as part of Task 4.
- Preserve the mandatory team identification and continuous asciinema recording. No final recording or group submission archive is produced by Task 3.

## VIA study notes

Be able to explain these using the code and diagrams:

1. Why `MajorityWin`, `MisereWin` and `CornerDominanceWin` are interchangeable strategies, and why terminal detection remains separate.
2. How eight-direction flanking finds only uninterrupted opponent disks bounded by a friendly disk.
3. Why a flip replaces an immutable `Piece`, and how a `CellChange` retains both colours for undo.
4. Why `CommandHistory` owns move counts and pairs of commands; how PASS and HvC fit the same mechanism.
5. Why only applied move records are saved, and how `Replay` reconstructs real undoable commands.
6. Why board and inventory snapshots are checked after replay; how a failed load preserves the current game.
7. How the Smarter AI evaluates candidates without mutating the live game, and why its heuristic is not an optimal solver.
8. How new win objectives and registered factories extend the framework without changing Core.
