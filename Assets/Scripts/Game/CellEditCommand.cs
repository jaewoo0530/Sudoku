using Sudoku.Core;

namespace Sudoku.Game
{
    public class CellEditCommand
    {
        public int Row { get; }
        public int Col { get; }
        public Cell Before { get; }
        public Cell After { get; }

        public CellEditCommand(int row, int col, Cell before, Cell after)
        {
            Row = row;
            Col = col;
            Before = before;
            After = after;
        }

        public void Do(SudokuBoard board)
        {
            board.SetCell(Row, Col, After);
        }

        public void Undo(SudokuBoard board)
        {
            board.SetCell(Row, Col, Before);
        }
    }
}
