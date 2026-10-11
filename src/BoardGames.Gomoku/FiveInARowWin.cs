using BoardGames.Core;

namespace BoardGames.Gomoku;

public sealed class FiveInARowWin : IWinStrategy
{
    private int CheckCell(Board board, int r, int c, int playerIndex, int count) //helper function to keep WinCheck cleaner
    {
        Piece? piece = board[new Position(r, c)];

        if (piece != null && piece.Owner.Index == playerIndex)
        {
            count++;
        }
        else
        {
            count = 0;
        }
        return count;
    }

    private bool WinCheck(Board board, int r, int c, int playerIndex)
    {
        int count = 0;

        for(int i = 1; i <= board.Rows; i++) //verticle win check
        {
            count = CheckCell(board, i, c, playerIndex, count);

            if (count >= 5)
            {
                return true;
            }
        }

        count = 0;

        for(int i = 1; i <= board.Cols; i++) //horizontal win check
        {
            count = CheckCell(board, r, i, playerIndex, count);

            if (count >= 5)
            {
                return true;
            }
        }

        count = 0;

        int tempR = r;
        int tempC = c;

        while(tempR > 1 && tempC > 1)
        {
            tempR--;
            tempC--;
        }

        while (tempR <= board.Rows && tempC <= board.Cols) //diagonal wincheck 1
        {
            count = CheckCell(board, tempR, tempC, playerIndex, count);

            if(count >= 5)
            {
                return true;
            }

            tempR++;
            tempC++;
        }

        count = 0;

        tempR = r;
        tempC = c;

        while(tempR < board.Rows && tempC > 1)
        {
            tempR++;
            tempC--;
        }

        while(tempR >= 1 && tempC <= board.Cols) //diagonal wincheck 2
        {
            count = CheckCell(board, tempR, tempC, playerIndex, count);

            if(count >= 5)
            {
                return true;
            }

            tempR--;
            tempC++;
        }

        return false;
    }

    public GameResult Evaluate(GameState state, bool isTerminal)
    {
        Board board = state.Board;

        foreach(Position position in board.AllPositions())
        {
            Piece? piece = board[position];

            if (piece != null && WinCheck(board, position.Row, position.Col, piece.Owner.Index))
            {
                return GameResult.Win(piece.Owner, "five stones in a row");
            }
        }

        if (isTerminal)
        {
            return GameResult.Draw("no legal moves remain");
        }

        return GameResult.InProgress;
    }
}