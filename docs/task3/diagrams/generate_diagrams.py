"""Generate editable SVG figures using the implemented Task 3 class names.

No external packages are needed. Run from any directory with Python 3.
The Mermaid files are alternative editable diagram sources.
"""
from html import escape
from pathlib import Path

OUT = Path(__file__).resolve().parent


class Figure:
    def __init__(self, width, height, title):
        self.parts = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">',
                      f'<title>{escape(title)}</title>',
                      '<defs><marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M 0 0 L 10 5 L 0 10 z" fill="#334155"/></marker></defs>',
                      '<style>text{font-family:Arial,sans-serif;fill:#152238}.mono{font-family:Consolas,monospace}.object{font-weight:bold;text-decoration:underline}.edge{fill:none;stroke:#334155;stroke-width:1.6;marker-end:url(#arrow)}.dash{stroke-dasharray:6 5}</style>',
                      f'<rect width="{width}" height="{height}" fill="white"/>']

    def text(self, x, y, value, size=17, css='', anchor='start'):
        self.parts.append(f'<text x="{x}" y="{y}" font-size="{size}" class="{css}" text-anchor="{anchor}">{escape(value)}</text>')

    def rect(self, x, y, w, h, fill='#f8fafc', dashed=False):
        dash = 'stroke-dasharray="6 5"' if dashed else ''
        self.parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{fill}" stroke="#64748b" stroke-width="1.3" {dash}/>')

    def box(self, x, y, w, h, name, lines):
        self.rect(x, y, w, h)
        self.text(x + 14, y + 28, name, 17, 'object')
        self.parts.append(f'<path d="M{x} {y+41} H{x+w}" stroke="#94a3b8"/>')
        for i, line in enumerate(lines):
            self.text(x + 14, y + 66 + 24 * i, line, 16)

    def edge(self, points, label='', at=None, dashed=False):
        self.parts.append(f'<polyline points="{" ".join(f"{x},{y}" for x,y in points)}" class="edge {"dash" if dashed else ""}"/>')
        if at:
            self.text(*at, label, 15)

    def write(self, name):
        (OUT / name).write_text('\n'.join(self.parts) + '\n</svg>\n', encoding='utf-8')


def object_diagram():
    f = Figure(1480, 1090, 'Reversi runtime object diagram after four legal moves')
    f.text(50, 35, 'Reversi runtime object diagram', 26)
    f.text(50, 62, 'Standard Reversi after P3:4, P3:3, P4:3, P5:3 — actual CLI state', 17)
    f.box(50, 115, 360, 145, 'game : ReversiGame', ['Descriptor = reversi/standard', 'Setup = HvH, Dumb', 'Result.Kind = InProgress'])
    f.box(560, 115, 360, 145, 'state : GameState', ['MoveCount = 4', '/Current = p1', '/TurnNumber = 3'])
    f.box(1030, 115, 390, 145, 'history : CommandHistory', ['_undo = [c1, c2, c3, c4]', '_redo = []', '/CanUndo = true'])
    f.edge([(410, 205), (560, 205)], 'State', (465, 197))
    f.edge([(350, 115), (350, 90), (1150, 90), (1150, 115)], 'History', (960, 84))
    f.box(50, 315, 200, 75, 'win : MajorityWin', [])
    f.box(290, 315, 260, 100, 'view : ConsoleBoardView', ['_game = game', 'AutoRedraw = false (CLI)'])
    f.edge([(150, 260), (150, 315)], 'Win', (160, 293))
    f.edge([(390, 260), (390, 315)], 'View', (402, 293))
    f.edge([(290, 352), (275, 352), (275, 260)], 'TurnChanged', (282, 280), True)
    f.box(590, 315, 240, 125, 'p1 : HumanPlayer', ['Index = 0; Symbol = X', 'Inventory = {}'])
    f.box(880, 315, 240, 125, 'p2 : HumanPlayer', ['Index = 1; Symbol = O', 'Inventory = {}'])
    f.edge([(690, 260), (710, 315)], 'Players[0]', (595, 293))
    f.edge([(810, 260), (1000, 315)], 'Players[1]', (889, 289))
    f.edge([(560, 236), (565, 236), (565, 470), (285, 470), (285, 505)], 'Board', (292, 491))
    f.box(50, 505, 450, 470, 'board : Board', ['Rows = 8; Cols = 8', '_cells rendered as symbols below'])
    rows = ['........', '........', '..OX....', '..OXX...', '..OOO...', '........', '........', '........']
    f.text(100, 626, '    1  2  3  4  5  6  7  8', 20, 'mono')
    for i, row in enumerate(rows):
        f.text(100, 662 + 30 * i, f'{i+1}   ' + '  '.join(row), 20, 'mono')
    f.text(80, 945, 'Derived disk counts: X = 3, O = 5', 18)
    f.rect(575, 495, 845, 335, 'white', dashed=True)
    f.text(590, 519, 'Applied commands in chronological order; c4 is most recent', 16)
    f.edge([(1350, 260), (1445, 260), (1445, 540), (1420, 540)], '_undo[0..3]', (1278, 473))
    f.box(595, 540, 375, 115, 'c1 : ReversiPlaceCommand', ['Actor = p1; At = (3,4)', '2 retained CellChanges'])
    f.box(1010, 540, 390, 115, 'c2 : ReversiPlaceCommand', ['Actor = p2; At = (3,3)', '2 retained CellChanges'])
    f.box(595, 690, 375, 115, 'c3 : ReversiPlaceCommand', ['Actor = p1; At = (4,3)', '2 retained CellChanges'])
    f.box(1010, 690, 390, 115, 'c4 : ReversiPlaceCommand', ['Actor = p2; At = (5,3)', '_changes = [d1, d2, d3]'])
    f.box(585, 885, 260, 130, 'd1 : CellChange', ['At = (5,3)', 'Before = null', 'After = Piece(p2, Disk)'])
    f.box(865, 885, 260, 130, 'd2 : CellChange', ['At = (4,3)', 'Before = Piece(p1, Disk)', 'After = Piece(p2, Disk)'])
    f.box(1145, 885, 260, 130, 'd3 : CellChange', ['At = (5,4)', 'Before = Piece(p1, Disk)', 'After = Piece(p2, Disk)'])
    f.edge([(1205, 805), (1205, 851), (715, 851), (715, 885)])
    f.edge([(1205, 851), (995, 851), (995, 885)])
    f.edge([(1205, 851), (1275, 851), (1275, 885)])
    f.text(590, 843, 'Retained before/after values reverse both captured disks on undo', 16)
    f.text(50, 1045, 'Underlined names are runtime instances. / marks a derived property. Empty lists are actual empty history/inventory collections.', 16)
    f.text(50, 1073, 'Piece(p1/p2, Disk) denotes an immutable Piece whose Owner references that player; the board does not store X/O characters.', 16)
    f.write('reversi-object.svg')


def sequence_diagram():
    f = Figure(1640, 1860, 'Save and load sequence diagram using the Task 3 repository')
    f.text(35, 35, 'Save and load sequence diagram', 26)
    f.text(35, 61, 'Concrete workflow: JSON validation, factory reconstruction, command replay and state verification', 17)
    xs = [110, 305, 520, 730, 930, 1120, 1320, 1525]
    labels = [('menu :', 'MainMenu'), ('game :', 'Game'), ('repository :', 'JsonGameRepository'), ('File system', ''),
              ('catalog :', 'GameCatalog'), ('factory :', 'IGameFactory'), ('history :', 'CommandHistory'), ('command :', 'IMoveCommand')]
    for x, (a, b) in zip(xs, labels):
        f.rect(x - 88, 87, 176, 63)
        f.text(x, 113, a, 16, anchor='middle')
        if b: f.text(x, 137, b, 16, anchor='middle')
        f.parts.append(f'<path d="M{x} 150 V1758" stroke="#94a3b8" stroke-dasharray="6 6"/>')

    def message(a, b, y, label, reply=False):
        f.edge([(xs[a], y), (xs[b], y)], dashed=reply)
        f.text((xs[a] + xs[b]) / 2, y - 9, label, 16, anchor='middle')

    def note(x, y, width, lines):
        f.rect(x, y, width, 20 + len(lines) * 22, 'white')
        for i, line in enumerate(lines): f.text(x + 10, y + 23 + i * 22, line, 15)

    f.rect(20, 170, 1600, 430, 'none')
    f.text(35, 197, 'SAVE  |  initiated by Game.HandleControl after reading save path', 18)
    message(1, 2, 233, 'Save(game, path)')
    message(2, 1, 274, 'ToSaveData()')
    message(1, 6, 315, 'Records()')
    message(6, 1, 356, 'applied MoveRecords, oldest first', True)
    note(95, 377, 395, ['Game also calls Board.ToSnapshot()', 'and copies both player inventories.'])
    message(1, 2, 465, 'GameSaveData', True)
    message(2, 3, 505, 'WriteAllText(temp, JSON)')
    message(2, 3, 545, 'Move(temp, path, overwrite)')
    message(2, 1, 586, 'saved', True)

    f.rect(20, 630, 1600, 1135, 'none')
    f.text(35, 658, 'LOAD  |  new menu load or GameOutcome.LoadRequested', 18)
    message(0, 2, 703, 'Load(path, catalog)')
    message(2, 3, 745, 'ReadAllText(path)')
    message(3, 2, 785, 'JSON', True)
    note(390, 803, 385, ['RequireFields; Deserialize; ValidateData', 'Check schema, version, enums and records.'])
    message(2, 4, 895, 'Resolve(familyKey, variantKey)')
    message(4, 2, 935, 'registered factory', True)
    message(2, 5, 975, 'CreateGame(GameSetup)')
    note(890, 993, 390, ['Create parts; Assemble fresh game.', 'Reversi initialises the four centre disks.'])
    message(5, 2, 1082, 'fresh game', True)
    message(2, 1, 1122, 'AutoRedraw = false')
    f.rect(245, 1142, 1358, 353, 'none', dashed=True)
    f.text(257, 1167, 'loop  [each applied MoveRecord; reject if previous Result.IsOver]', 16)
    message(2, 1, 1208, 'Replay([record])')
    note(252, 1225, 335, ['Verify actor; Validate; CreateCommand.'])
    message(1, 6, 1284, 'Do(command, State)')
    message(6, 7, 1324, 'Execute(State)')
    note(1090, 1342, 485, ['Reversi command: record CellChanges; Board.Apply.', 'History: push undo, clear redo, MoveCount++.'])
    note(252, 1420, 480, ['Game: OnMoveApplied; evaluate win; raise TurnChanged.'])
    message(1, 2, 1482, 'replayed', True)
    message(2, 1, 1532, 'read board + inventories')
    message(1, 2, 1572, 'reconstructed values', True)
    note(410, 1590, 535, ['Compare board and inventories with saved snapshots.', 'Empty history: Replay(empty) evaluates the initial result.'])
    message(2, 1, 1678, 'restore redraw; Repository = this')
    message(2, 0, 1720, 'verified game; undo stack ready', True)
    message(0, 1, 1756, 'Play(io) if in progress')
    f.text(35, 1794, 'Failure: MainMenu.TryLoad catches the error; the current game is retained. Internal calls are described in notes.', 17)
    f.text(35, 1822, 'Completed save: the menu renders its board and result, then returns without starting another turn.', 17)
    f.write('save-load-sequence.svg')


if __name__ == '__main__':
    object_diagram()
    sequence_diagram()
    print('Generated reversi-object.svg and save-load-sequence.svg')
