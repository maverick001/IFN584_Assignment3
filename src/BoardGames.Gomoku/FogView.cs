using BoardGames.Core;
using System.Text;

namespace  BoardGames.Gomoku;

public sealed class FogView : IBoardView
{
    private readonly TextWriter? _output;
    private Game? _game;

    public FogView(TextWriter? output = null)
    {
        _output = output;
    }

    public bool AutoRedraw{get; set;} = true;

    public void Attach(Game game)
    {
        _game = game;

        game.TurnChanged += (_, _) =>
        {
            if (AutoRedraw)
            {
                Render();
            }
        };
    }

    private bool IsVisible(Board board, Position position, Player player)
    {
        for(int r = position.Row - 1; r <= position.Row + 1; r++)
        {
            for(int c = position.Col - 1; c <= position.Col + 1; c++)
            {
                Position nearby = new Position(r, c);

                if (!board.InBounds(nearby))
                {
                    continue;
                }

                Piece? piece = board[nearby];

                if(piece != null && piece.Owner.Index == player.Index)
                {
                    return true;
                }
            }
        }

        return false;
    }
    public void Render()
    {
        if (_game == null)
        {
            return;
        }

        Board board = _game.State.Board;
        Player player = _game.State.Current;

        if(_game.Setup.Mode == GameMode.HvC)
        {
            player = _game.State.Players[0];
        }
        else
        {
            player = _game.State.Current;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.Append("   ");
        for(int c = 1; c <= board.Cols; c++)
        {
            sb.Append($"{c,3}");
        }
        sb.AppendLine();
        for(int r = 1; r <= board.Rows; r++)
        {
            sb.Append($"{r,3}");
            for(int c = 1; c <= board.Cols; c++)
            {
                Position position = new Position(r, c);
                char symbol = '?';

                if(_game.Result.IsOver || IsVisible(board, position, player))
                {
                    sb.Append($"{board[new Position(r, c)]?.Symbol ?? '.',3}");
                }
                else
                {
                    sb.Append($"{symbol,3}");
                }

            }
          sb.AppendLine();
          }
        (_output ?? Console.Out).Write(sb.ToString()); 
    }
}